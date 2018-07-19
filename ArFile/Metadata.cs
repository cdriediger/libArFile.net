using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MsgPack.Serialization;

namespace ArFile
{
    public partial class Metadata
    {

        public Dictionary<int, File> Files { get; set; }
        public int LastChunkId { get; set; }
        public int MetadataChunkId { get; set; }
        public int LastFileId { get; set; }
        public int EndOfArchive { get; set; }
        public Dictionary<int, Chunk> WrittenChunks { get; set; }
        public Dictionary<int, Chunk> EmptyChunks { get; set; }
        public Dictionary<int, Chunk> EmptyChunksStart { get; set; }
        public Dictionary<int, Chunk> EmptyChunksLength { get; set; }
        public int defaultChunkSize { get; set; }
        public CompressionMethod defaultCompressionMethod { get; set; }
        public int defaultCompressionLevel { get; set; }

        public Metadata()
        {
            this.Files = new Dictionary<int, File>();
            this.LastChunkId = -1;
            this.MetadataChunkId = 0;
            this.LastFileId = 0;
            this.EndOfArchive = 0;
            this.WrittenChunks = new Dictionary<int, Chunk>();
            this.EmptyChunks = new Dictionary<int, Chunk>();
            this.EmptyChunksStart = new Dictionary<int, Chunk>();
            this.EmptyChunksLength = new Dictionary<int, Chunk>();
            this.defaultChunkSize = 10000;
            this.defaultCompressionMethod = CompressionMethod.LZMA;
            this.defaultCompressionLevel = 5;
            //Register Superblock Chunk
            Logger.Debug("Register Superblock Chunk...");
            WriteChunk(GetChunk(100), 0);
            Logger.Debug("Register Superblock Chunk complete!");
        }
    }
}
