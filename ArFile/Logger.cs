using System;
using System.Collections.Generic;
using System.Text;

namespace ArFile
{
    static class Logger
    {
        static public string LogFilePath;

        static public void Debug(string msg)
        {
            Console.WriteLine($"Debug: {msg}");
        }

        static public void Info(string msg)
        {
            Console.WriteLine($"Info: {msg}");
        }

        static public void Warn(string msg)
        {
            Console.WriteLine($"Warning: {msg}");
        }

        static public void Error(string msg)
        {
            Console.WriteLine($"Error: {msg}");
        }

        static public void FatalError(string msg)
        {
            Console.WriteLine($"FatalError: {msg}");
            System.Environment.Exit(1);
        }

    }
}
