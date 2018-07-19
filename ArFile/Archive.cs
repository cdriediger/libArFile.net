using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MsgPack.Serialization;
using Newtonsoft.Json;

namespace ArFile
{ 
    class Archive : IDisposable
    {
        private string PathToArchiveFile;
        private FileAccess Mode;
        private FileStream ArchivFile;
        public Metadata Metadata;

        public Archive(string PathToArchiveFile, FileAccess Mode = FileAccess.Read)
        {
            this.PathToArchiveFile = PathToArchiveFile;
            this.Mode = Mode;
            if (System.IO.File.Exists(PathToArchiveFile))
            {
                OpenArchive();
            }
            else
            {
                CreateNewArchive();
            }
        }

        public void Dispose()
        {
            string MetadataPacked = JsonConvert.SerializeObject(Metadata);    
            //Write Metadata to File
            int MetaDataChunkId = WriteChunk(MetadataPacked, 0);
            int MetaDataChunkStart = Metadata.GetChunkStart(MetaDataChunkId);
            int MetaDataChunkLength = Metadata.GetChunkLength(MetaDataChunkId);
            Logger.Debug($"MetadaChunk: Start: {MetaDataChunkStart}, Length: {MetaDataChunkLength}\n##########\n{MetadataPacked.ToString()}\n#############");
            string MetaDataChunkHash = "MD5DEMOHASH";
            //Write Superblock
            SuperBlock NewSuperBlock = new SuperBlock(MetaDataChunkStart, MetaDataChunkLength, MetaDataChunkHash);
            MessagePackSerializer SuperBlockSerializer = MessagePackSerializer.Get<SuperBlock>();
            Chunk SuperblockChunk = Metadata.GetSuperblockChunk();
            ArchivFile.Seek(SuperblockChunk.Start, SeekOrigin.Begin);
            SuperBlockSerializer.Pack(ArchivFile, NewSuperBlock);
            ArchivFile.Flush();
            ArchivFile.Close();
        }

        public int NewFile(string FilePath, bool Dedubulication = true)
        {
            if (!(System.IO.File.Exists(FilePath))) throw new FileNotFoundException($"File '{FilePath}' not found");
            string FileHash = "MD5DEMOHASH";
            int FileId = this.Metadata.NewFile(FilePath, FileHash, Dedubulication = true);
            return FileId;
        }

        public void DeleteFile(int fileId)
        {
            List<int> chunks = new List<int>(Metadata.GetChunksOfFile(fileId));
            foreach (int chunkId in chunks)
            {
                Metadata.MarkChunkAsEmpty(chunkId, fileId);
            }
            Metadata.Files.Remove(fileId);
        }

        public void ReclaimWhiteSpace()
        {
            Logger.Debug("A: start reclaiming whitespace...");
            string tmpPathToArchiveFile = PathToArchiveFile + ".tmp";
            Logger.Debug($"A: Temporary archive path: {tmpPathToArchiveFile}");
            string bakPathToArchiveFile = PathToArchiveFile + ".bak";
            Logger.Debug($"A: Backup archive path: {bakPathToArchiveFile}");
            FileStream tmpArchivFile = System.IO.File.Open(tmpPathToArchiveFile, FileMode.CreateNew, FileAccess.ReadWrite);
            int tmpArchivPosition = 100;
            tmpArchivFile.Seek(tmpArchivPosition, SeekOrigin.Begin);
            foreach (KeyValuePair<int, Chunk> chunkKv in Metadata.WrittenChunks)
            {
                int chunkId = chunkKv.Key;
                Chunk chunk = chunkKv.Value;
                if (chunkId == 0) continue;
                Logger.Debug($"A: copy chunk to tmp archive file: ChunkId: {chunk.Id}, Start: {chunk.Start} Length: {chunk.Length}");
                ArchivFile.Seek(chunk.Start, SeekOrigin.Begin);
                chunk.Start = tmpArchivPosition;
                tmpArchivPosition += chunk.Length;
                byte[] buffer = new byte[chunk.Length];
                ArchivFile.Read(buffer, 0, chunk.Length);
                tmpArchivFile.Write(buffer, 0, chunk.Length);
            }
            tmpArchivFile.Close();
            ArchivFile.Close();
            System.IO.File.Move(PathToArchiveFile, bakPathToArchiveFile);
            System.IO.File.Move(tmpPathToArchiveFile, PathToArchiveFile);
            ArchivFile = System.IO.File.Open(PathToArchiveFile, FileMode.Open, FileAccess.ReadWrite);
            Metadata.EmptyChunks = new Dictionary<int, Chunk>();
            Metadata.EmptyChunksStart = new Dictionary<int, Chunk>();
            Metadata.EmptyChunksLength = new Dictionary<int, Chunk>();
            Metadata.EndOfArchive = tmpArchivPosition;
        }

        public int WriteChunk(byte[] Data, int FileId)
        {
            byte[] compressedData;
            if (FileId == 0)
            {
                compressedData = Compressor.Compress(Data, CompressionMethod.LZMA, 9);
            }
            else
            {
                compressedData = Compressor.Compress(Data, Metadata.defaultCompressionMethod, Metadata.defaultCompressionLevel);
            }
            int chunkId = Metadata.GetChunk(compressedData.Length);
            int ChunkStart = Metadata.GetChunkStart(chunkId);
            int ChunkLength = Metadata.GetChunkLength(chunkId);
            if (compressedData.Length > ChunkLength) throw new ChunkToSmallException($"Chunk {chunkId} smaller then Data");
            Metadata.WriteChunk(chunkId, FileId);
            ArchivFile.Seek(ChunkStart, SeekOrigin.Begin);
            if (!(ArchivFile.Position == ChunkStart)) Logger.FatalError("ArchiveFile Position != ChunkStart");
            if (compressedData.Length > ChunkLength) Logger.FatalError("DataLength > Chunk");
            Logger.Debug($"Writing '{Data}' to Chunk: {chunkId} Start: {ChunkStart} Length: {ChunkLength}");
            ArchivFile.Write(compressedData, 0, ChunkLength);
            return chunkId;
        }

        public int WriteChunk(string Data, int FileId)
        {
            return WriteChunk(Encoding.ASCII.GetBytes(Data), FileId);
        }

        public string ReadString(int ChunkId)
        {
            return System.Text.Encoding.Default.GetString(ReadChunk(ChunkId));
        }

        public byte[] ReadChunk(int ChunkId)
        {
            int ChunkStart = Metadata.GetChunkStart(ChunkId);
            int ChunkLength = Metadata.GetChunkLength(ChunkId);
            byte[] CompressedDataBuffer = new byte[ChunkLength];
            ArchivFile.Seek(ChunkStart, SeekOrigin.Begin);
            ArchivFile.Read(CompressedDataBuffer, 0, ChunkLength);
            return Compressor.Decompress(CompressedDataBuffer, Metadata.defaultCompressionMethod);
        }

        private void CreateNewArchive()
        {
            ArchivFile = System.IO.File.Open(PathToArchiveFile, FileMode.CreateNew, FileAccess.ReadWrite);
            Metadata = new Metadata();
        }

        private void OpenArchive()
        {
            ArchivFile = System.IO.File.Open(PathToArchiveFile, FileMode.Open, FileAccess.ReadWrite);
            SuperBlock superBlock = ReadSuperBlock();
            Logger.Debug($"A: Found Superblock: Start: {superBlock.MetadataStart}, Length: {superBlock.MetadataLength}; Hash: {superBlock.MetadataHash}");
            byte[] compressedMetadataBuffer = new byte[superBlock.MetadataLength];
            ArchivFile.Seek(superBlock.MetadataStart, SeekOrigin.Begin);
            ArchivFile.Read(compressedMetadataBuffer, 0, superBlock.MetadataLength);
            byte[] metadataBuffer = Compressor.Decompress(compressedMetadataBuffer, CompressionMethod.LZMA);
            string MetadataString = System.Text.Encoding.Default.GetString(metadataBuffer);
            Logger.Debug($"A: Found Metadata: '{MetadataString}' Length: {MetadataString.Length}");
            Metadata = JsonConvert.DeserializeObject<Metadata>(MetadataString);
        }

        private SuperBlock ReadSuperBlock()
        {
            SuperBlock SuperBlock = null;
            byte[] SuperBlockBuffer = new byte[100];
            ArchivFile.Read(SuperBlockBuffer, 0, 100);
            MessagePackSerializer MessagePackSerializer = MessagePackSerializer.Get<SuperBlock>();
            SuperBlock = (SuperBlock)MessagePackSerializer.UnpackSingleObject(SuperBlockBuffer);
            return SuperBlock;
        }

    }
}
