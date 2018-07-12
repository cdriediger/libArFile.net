using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace ArFile
{
    class Test
    {
        static void Main()
        {
            AddFile();
            //ReadArchive();
            //WriteTest();
            Console.ReadLine();
        }

        static public void AddFile()
        {
            Archive archive = new Archive(@"C:\Users\c.driediger\source\repos\ArFile\ArFile\testarchive");
            int f1 = archive.NewFile(@"C:\tmp\test.txt");
            string data = "1234567890";
            int c1 = archive.Metadata.GetChunk(data.Length);
            archive.WriteChunk(data, c1, f1);
            data = "ABCDEFGHIJKLMNOPGRSTUVWXYZ";
            int c2 = archive.Metadata.GetChunk(data.Length);
            archive.WriteChunk(data, c2, f1);
            archive.Dispose();
        }

        static public void AddRandomData()
        {

        }

        static public void ReadArchive()
        {
            Archive archive = new Archive(@"C:\Users\c.driediger\source\repos\ArFile\ArFile\testarchive");
            string data = archive.ReadChunk(3);
            Logger.Warn($"Data: '{data}'");
        }

        static public void WriteTest()
        {
            FileStream ArchivFile = System.IO.File.Open(@"C:\Users\c.driediger\source\repos\ArFile\ArFile\testfile", FileMode.CreateNew, FileAccess.ReadWrite);
            ArchivFile.Seek(100, SeekOrigin.Begin);
            ArchivFile.Write(Encoding.ASCII.GetBytes("Data"), 0, 4);
            ArchivFile.Seek(0, SeekOrigin.Begin);
            ArchivFile.Write(Encoding.ASCII.GetBytes("Superblock"), 0, 10);
            ArchivFile.Flush();
            ArchivFile.Close();
        }
    }
}
