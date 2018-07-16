using System;
using System.Collections.Generic;
using System.Text;

namespace ArFile
{
    public class Chunk
    {
        public int Id { get; }
        public int Start { get; }
        public int Length { get; }
        private int Written { get; set; }
        private int EndOfChunk { get; set; }
        private bool Deleted { get; set; }

        public Chunk(int Start, int Length, int Written, int Id)
        {
            Logger.Debug($"C:{Id} INIT CHUNK");
            this.Id = Id;
            this.Start = Start;
            this.Length = Length;
            this.Written = Written;
            this.Deleted = false;

            if (Id == 0)
            {
                Logger.Debug($"Initializing superblock {ToString()}");
            } else
            {
                Logger.Debug($"Initializing {ToString()}");
            }
            this.EndOfChunk = Start + Length;
            //Set EndOfArchive in Metadata if EndOfChung > EndOfArchive
        }

        public override string ToString()
        {
            return $"ChunkId: {Id} Start: {Start} Length: {Length} Written: {Written}";
        }

        public void Delete()
        {
            Logger.Debug($"C:DELETE CHUNK {Id}");
            if (IsWritten()) Logger.FatalError($"Cannot delete written Chunk {Id} because its written");
            this.Deleted = true;
        }

        public bool IsWritten()
        {
            if (Written > 1) return true;
            return false;
        }

        public void SetWritten()
        {
            Written++;
        }

        public void SetNotWritten()
        {
            Written--;
        }

    }
}
