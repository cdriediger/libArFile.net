using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MsgPack.Serialization;

namespace ArFile
{
    public partial class Metadata
    {
        public int GetChunk(int DataLength)
        {
            Logger.Debug($"M: GET CHUNK");
            Chunk Chunk = null;
            try
            {
                Chunk = FindEmptyChunkWithLengthMin(DataLength);
                if (Chunk.Length > DataLength) Chunk = SplitChunk(Chunk, DataLength);
            }
            catch (ChunkNotFoundException)
            {
                Chunk = GetNewChunk(DataLength);
            }
            return Chunk.Id;
        }

        public Chunk GetSuperblockChunk()
        {
            return WrittenChunks[this.MetadataChunkId];
        }

        public void WriteChunk(int ChunkId, int FileId)
        {
            Logger.Debug($"M: MARK CHUNK {ChunkId} AS WRITTEN BY {FileId}");
            Chunk Chunk = GetChunkByIdInternal(ChunkId);
            Chunk.SetWritten();
            if (EmptyChunks.ContainsKey(Chunk.Id)) EmptyChunks.Remove(Chunk.Id);
            if (EmptyChunksStart.ContainsKey(Chunk.Start)) EmptyChunksStart.Remove(Chunk.Start);
            if (EmptyChunksLength.ContainsKey(Chunk.Length)) EmptyChunksLength.Remove(Chunk.Length);
            if (!(WrittenChunks.ContainsKey(Chunk.Id))) WrittenChunks.Add(Chunk.Id, Chunk);
            if (!(FileId == 0))
            {
                if (!(Files.ContainsKey(FileId))) throw new FileNotFoundException($"File {FileId} not found in Metadata");
                Files[FileId].AddChunk(ChunkId);
            }
        }

        public void MarkChunkAsEmpty(int ChunkId, int FileId)
        {
            Logger.Debug($"M: MARK CHUNK {ChunkId} AS NOT WRITTEN BY {FileId}");
            Chunk Chunk = GetChunkByIdInternal(ChunkId);
            Chunk.SetNotWritten();
            if (!(Files.ContainsKey(FileId))) throw new FileNotFoundException($"File {FileId} not found in Metadata");
            Files[FileId].DeleteChunk(ChunkId);
            if (!(Chunk.IsWritten()))
            {
                if (WrittenChunks.ContainsKey(Chunk.Id)) WrittenChunks.Remove(Chunk.Id);
                try
                {
                    Chunk PreChunk = GetChunkBeforIfEmpty(Chunk);
                    Chunk = CombineChunks(PreChunk, Chunk);
                }
                catch (ChunkNotFoundException)
                {

                }
                try
                {
                    Chunk AfterChunk = GetChunkAfterIfEmpty(Chunk);
                    Chunk = CombineChunks(Chunk, AfterChunk);
                }
                catch (ChunkNotFoundException)
                {

                }
                if (!(EmptyChunks.ContainsKey(Chunk.Id)))
                {
                    EmptyChunks.Add(Chunk.Id, Chunk);
                    EmptyChunksStart.Add(Chunk.Start, Chunk);
                    EmptyChunksLength.Add(Chunk.Length, Chunk);
                }
            }
        }

        public void DeleteEmptyChunks(int newEndOfArchive)
        {
            Logger.Debug($"M: Delete empty Chunks");
            EmptyChunks = new Dictionary<int, Chunk>();
            EmptyChunksStart = new Dictionary<int, Chunk>();
            EmptyChunksLength = new Dictionary<int, Chunk>();
            SetEndOfArchive(newEndOfArchive);
        }

        private Chunk GetChunkByIdInternal(int ChunkId)
        {
            Logger.Debug($"M: GET CHUNK BY ID");
            if (EmptyChunks.ContainsKey(ChunkId)) return EmptyChunks[ChunkId];
            if (WrittenChunks.ContainsKey(ChunkId)) return WrittenChunks[ChunkId];
            throw new ChunkNotFoundException($"Chunk {ChunkId} not found in Metadata");
        }

        public Chunk GetChunkById(int ChunkId)
        {
            Logger.Warn($"Direct Access to Chunk {ChunkId}");
            return GetChunkByIdInternal(ChunkId);
        }

        public int GetChunkStart(int ChunkId)
        {
            if (EmptyChunks.ContainsKey(ChunkId)) return EmptyChunks[ChunkId].Start;
            if (WrittenChunks.ContainsKey(ChunkId)) return WrittenChunks[ChunkId].Start;
            throw new ChunkNotFoundException($"Chunk {ChunkId} not found in Metadata");
        }

        public int GetChunkLength(int ChunkId)
        {
            if (EmptyChunks.ContainsKey(ChunkId)) return EmptyChunks[ChunkId].Length;
            if (WrittenChunks.ContainsKey(ChunkId)) return WrittenChunks[ChunkId].Length;
            throw new ChunkNotFoundException($"Chunk {ChunkId} not found in Metadata");
        }

        private Chunk FindEmptyChunkWithLengthMin(int DataLength)
        {
            Logger.Debug($"M: FIND EMPTY CHNUNK WITH LENGTH MIN {DataLength}");
            List<int> BigEnoughChunks = new List<int>();
            List<int> ChunksLengths = new List<int>(this.EmptyChunksLength.Keys);

            BigEnoughChunks = ChunksLengths.FindAll(x => x >= DataLength);
            BigEnoughChunks.Sort();

            //Logger.Debug("###############");
            //Logger.Debug($"M: EmptyChunks: {EmptyChunks.Count}");
            //foreach (KeyValuePair<int, Chunk> Chunk in EmptyChunks)
            //{
            //    Logger.Debug($"ChunkId: {Chunk.Key}, {Chunk.Value.ToString()}");
            //}
            //Logger.Debug("---------------");
            //Logger.Debug("###############");
            //Logger.Debug($"M: EmptyChunksLength: {EmptyChunksLength.Count}");
            //foreach (KeyValuePair<int, Chunk> Chunk in EmptyChunksLength)
            //{
            //    Logger.Debug($"ChunkId: {Chunk.Key}, {Chunk.Value.ToString()}");
            //}
            //Logger.Debug("---------------");
            //Logger.Debug("###############");
            //Logger.Debug($"M: Big Enough Chunks: {BigEnoughChunks.Count}");
            //foreach (int ChunkId in BigEnoughChunks)
            //{
            //    Logger.Debug($"ChunkId: {ChunkId}, "); //{GetChunkByIdInternal(ChunkId).ToString()}");
            //}
            //Logger.Debug("---------------");

            if (BigEnoughChunks.Count > 0) return EmptyChunksLength[BigEnoughChunks[0]];
            throw new ChunkNotFoundException($"Not Chunk >= {DataLength} found");
        }

        private Chunk GetNewChunk(int DataLength)
        {
            Logger.Debug($"M: GET NEW CHUNK");
            int ChunkId = GetNewChunkID();
            int ChunkStart = GetEndOfArchive();
            int ChunkLength = DataLength;
            SetEndOfArchive(ChunkStart + ChunkLength);
            Chunk NewChunk = new Chunk(ChunkStart, ChunkLength, 0, ChunkId);
            EmptyChunks.Add(ChunkId, NewChunk);
            return NewChunk;
        }

        private int GetNewChunkID()
        {
            Logger.Debug($"M: GET NEW CHUNK ID");
            LastChunkId++;
            return LastChunkId;
        }

        private int GetEndOfArchive()
        {
            Logger.Debug($"M: GET END OF ARCHIVE");
            return EndOfArchive;
        }

        private void SetEndOfArchive(int EndOfArchive)
        {
            Logger.Debug($"M: SET END OF ARCHIVE");
            this.EndOfArchive = EndOfArchive;
        }

        private Chunk GetChunkBeforIfEmpty(Chunk Chunk)
        {
            Logger.Debug($"M: GET CHUNK BEFORE IF EMPTY");
            foreach (KeyValuePair<int, Chunk> PreChunkKv in EmptyChunks)
            {
                Chunk PreChunk = PreChunkKv.Value;
                int PreChunkEnd = PreChunk.Start + PreChunk.Length;
                if (PreChunkEnd == Chunk.Start) return PreChunk;
            }
            throw new ChunkNotFoundException($"No Empty Chunk directly before {Chunk.Id} found");
        }

        private Chunk GetChunkAfterIfEmpty(Chunk Chunk)
        {
            Logger.Debug($"M: GET CHUNK AFTER IF EMPTY");

            foreach (KeyValuePair<int, Chunk> AfterChunkKv in EmptyChunks)
            {
                Chunk AfterChunk = AfterChunkKv.Value;
                int ChunkEnd = Chunk.Start + Chunk.Length;
                if (AfterChunk.Start == ChunkEnd) return AfterChunk;
            }
            throw new ChunkNotFoundException($"No Empty Chunk directly after {Chunk.Id} found");
        }

        private Chunk CombineChunks(Chunk Chunk1, Chunk Chunk2)
        {
            Logger.Debug($"M: COMBINE CHUNKS {Chunk1.Id} & {Chunk2.Id}");

            RemoveChunk(Chunk1);
            RemoveChunk(Chunk2);
            int NewChunkId = Chunk1.Id;
            int NewChunkStart = Chunk1.Start;
            int NewChunkLength = Chunk1.Length + Chunk2.Length;
            Chunk NewChunk = RegisterChunk(NewChunkStart, NewChunkLength, NewChunkId);
            return NewChunk;
        }

        private Chunk SplitChunk(Chunk OrigChunk, int NewLength)
        {
            RemoveChunk(OrigChunk);
            int NewChunk1Start = OrigChunk.Start;
            int NewChunk1Length = NewLength;
            int NewChunk2Start = NewChunk1Start + NewChunk1Length;
            int NewChunk2Length = OrigChunk.Length - NewLength;
            Chunk NewChunk1 = RegisterChunk(NewChunk1Start, NewChunk1Length, OrigChunk.Id);
            Chunk NewChunk2 = RegisterChunk(NewChunk2Start, NewChunk2Length);
            return NewChunk1;
        }

        private Chunk RegisterChunk(int Start, int Length, int Id = 0)
        {
            if (Id == 0) Id = GetNewChunkID();
            Chunk NewChunk = new Chunk(Start, Length, 0, Id);
            EmptyChunks.Add(NewChunk.Id, NewChunk);
            EmptyChunksStart.Add(NewChunk.Start, NewChunk);
            EmptyChunksLength.Add(NewChunk.Length, NewChunk);
            return NewChunk;
        }

        private void RemoveChunk(Chunk Chunk)
        {
            if (WrittenChunks.ContainsKey(Chunk.Id)) WrittenChunks.Remove(Chunk.Id);
            if (EmptyChunks.ContainsKey(Chunk.Id)) EmptyChunks.Remove(Chunk.Id);
            if (EmptyChunksStart.ContainsKey(Chunk.Start)) EmptyChunksStart.Remove(Chunk.Start);
            if (EmptyChunksLength.ContainsKey(Chunk.Length)) EmptyChunksLength.Remove(Chunk.Length);
        }
    }
}
