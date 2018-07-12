using System;
using System.Collections.Generic;
using System.Text;

namespace ArFile
{
    public class ChunkNotFoundException : Exception
    {
        public ChunkNotFoundException()
        {
        }

        public ChunkNotFoundException(string message)
            : base(message)
        {
        }

        public ChunkNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
