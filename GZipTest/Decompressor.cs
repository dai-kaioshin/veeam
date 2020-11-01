using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    public class Decompressor
    {
        public void Decompress(string inputFileName, string outputFileName)
        {
            byte[] buffer = new byte[2048];
            byte[] decompressedBuffer = new byte[2048];

            using(var inputFileStream = File.OpenRead(inputFileName))
            {
                using (var fileStream = File.Create(outputFileName))
                {
                    while(inputFileStream.Position < inputFileStream.Length)
                    {
                        inputFileStream.Read(buffer, 0, sizeof(int));
                        int part = BitConverter.ToInt32(buffer);
                        inputFileStream.Read(buffer, 0, sizeof(long));
                        long position = BitConverter.ToInt64(buffer);
                        inputFileStream.Read(buffer, 0, sizeof(int));
                        int chunkSize = BitConverter.ToInt32(buffer);
                        inputFileStream.Read(buffer, 0, sizeof(int));
                        int compressedChunkSize = BitConverter.ToInt32(buffer);
                        inputFileStream.Read(buffer, 0, compressedChunkSize);
                        using (var memoryStream = new MemoryStream(buffer, 0, compressedChunkSize))
                        {
                            using(var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                            {
                                int read = 0;
                                while((read = gzipStream.Read(decompressedBuffer, 0, chunkSize)) > 0) {
                                    if(fileStream.Length <= position)
                                    {
                                        fileStream.SetLength(position + read);
                                    }
                                    fileStream.Seek(position, SeekOrigin.Begin);
                                    fileStream.Write(decompressedBuffer, 0, read);
                                }
                            }
                        }
                        
                    }
                }
            }
        }
    }
}
