using GZipTest.Chunks;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace GZipTest.Processing.Process
{
    class CompressionDataProcessor : IDataProcessor
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CompressionDataProcessor));
        public DataChunk ProcessData(DataChunk chunk)
        {
            _log.Debug($"Compressing chunk : {chunk}");
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
                {
                    gzipStream.Write(chunk.Data, 0, chunk.Data.Length);
                    gzipStream.Flush();
                    return new DataChunk(chunk.Part, chunk.Position, chunk.DecompressedSize, memoryStream.ToArray());
                }
            }
        }
    }
}
