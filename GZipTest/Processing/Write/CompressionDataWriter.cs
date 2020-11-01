using GZipTest.Chunks;
using log4net;
using System;
using System.Text;

namespace GZipTest.Processing.Write
{
    class CompressionDataWriter : AbstractDataWriter
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CompressionDataWriter));
        internal CompressionDataWriter(string outputFileName, int chunkSize)
            : base(outputFileName)
        {
            byte[] ascii = Encoding.ASCII.GetBytes(Constants.FILE_HEADER);
            _outputFile.Write(ascii);
            _outputFile.Write(BitConverter.GetBytes(chunkSize), 0, sizeof(int));
        }

        public override void WriteChunk(DataChunk chunk)
        {
            _log.Debug($"Writing chunk : {chunk}");
            _outputFile.Write(BitConverter.GetBytes(chunk.Part), 0, sizeof(int));
            _outputFile.Write(BitConverter.GetBytes(chunk.Position), 0, sizeof(long));
            _outputFile.Write(BitConverter.GetBytes(chunk.Data.Length), 0, sizeof(int));
            _outputFile.Write(chunk.Data, 0, chunk.Data.Length);
        }
    }
}
