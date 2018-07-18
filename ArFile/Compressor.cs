using System;
using System.Collections.Generic;
using System.Text;
using Noemax.Compression;

namespace ArFile
{
    public enum CompressionMethod
    {
        None,
        BZip2,
        Deflate,
        GZip,
        Lzf4,
        LZMA,
        Zlib
    }

    public class Compressor
    {
        static public byte[] Compress(byte[] data, CompressionMethod compressionMethod, int compressionLevel)
        {
            switch (compressionMethod)
            {
                case CompressionMethod.BZip2:
                    return CompressionFactory.BZip2.Compress(data, compressionLevel);
                case CompressionMethod.Deflate:
                    return CompressionFactory.Deflate.Compress(data, compressionLevel);
                case CompressionMethod.GZip:
                    return CompressionFactory.GZip.Compress(data, compressionLevel);
                case CompressionMethod.Lzf4:
                    return CompressionFactory.Lzf4.Compress(data, compressionLevel);
                case CompressionMethod.LZMA:
                    return CompressionFactory.Lzma.Compress(data, compressionLevel);
                case CompressionMethod.Zlib:
                    return CompressionFactory.Zlib.Compress(data, compressionLevel);
                case CompressionMethod.None:
                default:
                    return data;
            }
        }

        static public byte[] Decompress(byte[] data, CompressionMethod compressionMethod)
        {
            switch (compressionMethod)
            {
                case CompressionMethod.BZip2:
                    return CompressionFactory.BZip2.Decompress(data);
                case CompressionMethod.Deflate:
                    return CompressionFactory.Deflate.Decompress(data);
                case CompressionMethod.GZip:
                    return CompressionFactory.GZip.Decompress(data);
                case CompressionMethod.Lzf4:
                    return CompressionFactory.Lzf4.Decompress(data);
                case CompressionMethod.LZMA:
                    return CompressionFactory.Lzma.Decompress(data);
                case CompressionMethod.Zlib:
                    return CompressionFactory.Zlib.Decompress(data);
                case CompressionMethod.None:
                default:
                    return data;
            }
        }
    }
}
