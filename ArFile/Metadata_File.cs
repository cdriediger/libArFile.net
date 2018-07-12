using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MsgPack.Serialization;

namespace ArFile
{
    public partial class Metadata
    {
        public int NewFile(string FilePath, string FileHash, bool Dedubulication=true, int FileId = 0)
        {
            if (FileId == 0) FileId = GetNewFileId();
            File NewFile = new File(FileId, FilePath, FileHash, Dedubulication);
            Files.Add(FileId, NewFile);
            return FileId;
        }

        public List<int> GetChunksOfFile(int FileId)
        {
            if (!(Files.ContainsKey(FileId))) throw new FileNotFoundException($"File {FileId} not found in Metadata");
            return Files[FileId].GetChunks();
        }

        private int GetNewFileId()
        {
            Logger.Debug($"M: GET NEW FILE ID");
            LastFileId++;
            return LastFileId;
        }
    }
}
