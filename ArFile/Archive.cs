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

        public int WriteChunk(byte[] Data, int FileId)
        {
            byte[] compressedData;
            if (FileId == 0)
            {
                compressedData = Data;
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
            byte[] metadataBuffer = new byte[superBlock.MetadataLength];
            ArchivFile.Seek(superBlock.MetadataStart, SeekOrigin.Begin);
            ArchivFile.Read(metadataBuffer, 0, superBlock.MetadataLength);
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
