using GZipTest.Chunks;
using log4net;
using System.IO;

namespace GZipTest.Processing.Write
{
    class DecompressionDataWriter : AbstractDataWriter
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DecompressionDataWriter));
        internal DecompressionDataWriter(string outputFileName)
            : base(outputFileName)
        {
        }

        public override void WriteChunk(DataChunk chunk)
        {

            _log.Debug($"Writing chunk : {chunk}");

            if (_outputFile.Length < chunk.Position)
            {
                _outputFile.SetLength(chunk.Position + chunk.DecompressedSize);
            }
            _outputFile.Seek(chunk.Position, SeekOrigin.Begin);
            _outputFile.Write(chunk.Data, 0, chunk.DecompressedSize);
        }
    }
}
