using System;
using System.Collections.Generic;
using System.Text;

namespace ArFile
{
    class ChunkToSmallException : Exception
    {
        public ChunkToSmallException()
        {
        }

        public ChunkToSmallException(string message)
            : base(message)
        {
        }

        public ChunkToSmallException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
