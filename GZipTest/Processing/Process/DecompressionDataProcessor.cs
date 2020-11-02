using GZipTest.Chunks;
using log4net;
using System.IO;
using System.IO.Compression;

namespace GZipTest.Processing.Process
{
    class DecompressionDataProcessor : IDataProcessor
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DecompressionDataProcessor));
        public DataChunk ProcessData(DataChunk chunk)
        {
            using (var memoryStream = new MemoryStream(chunk.Data))
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    _log.Debug($"Decompressing chunk : {chunk}");
                    byte[] buffer = new byte[chunk.DecompressedSize];
                    int read = gzipStream.Read(buffer, 0, chunk.DecompressedSize);

                    return new DataChunk(chunk.Part, chunk.Position, read, buffer);
                }
            }
        }
    }
}
