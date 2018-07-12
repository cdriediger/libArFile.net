using System;
using System.Collections.Generic;
using System.Text;

namespace ArFile
{
    public class SuperBlock
    {
        public int MetadataStart;
        public int MetadataLength;
        public string MetadataHash;

        public SuperBlock(int MetadataStart, int MetadataLength, string MetadataHash)
        {
            this.MetadataStart = MetadataStart;
            this.MetadataLength = MetadataLength;
            this.MetadataHash = MetadataHash;
        }
    }
}
