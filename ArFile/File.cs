using System;
using System.Collections.Generic;
using System.Text;

namespace ArFile
{
    public class File
    {
        public int FileId;
        public string FilePath;
        public string FileHash;
        public bool Dedubulication;
        public List<int> Chunks;
        
        public File(int FileId, string FilePath, string FileHash, bool Dedubulication = true)
        {
            this.FileId = FileId;
            this.FilePath = FilePath;
            this.FileHash = FileHash;
            this.Dedubulication = Dedubulication;
            this.Chunks = new List<int>();
        }

        public void AddChunk(int ChunkId)
        {
            Chunks.Add(ChunkId);
        }

        public List<int> GetChunks()
        {
            return Chunks;
        }

        public void DeleteChunk(int ChunkId)
        {
            if (!(Chunks.Contains(ChunkId))) throw new ChunkNotFoundException($"Chunk {ChunkId} not found in File {FileId}");
            Chunks.Remove(ChunkId);
        }
    }
}
