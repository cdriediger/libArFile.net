using System;
using System.Collections.Generic;
using System.Text;

namespace ArFile
{
    class FileNotFoundException : Exception
    {
        public FileNotFoundException()
        {
        }

        public FileNotFoundException(string message)
            : base(message)
        {
        }

        public FileNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }

    }
}
