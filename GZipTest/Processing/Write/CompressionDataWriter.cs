using GZipTest.Chunks;
using log4net;
using System;
using System.IO;
using System.Text;

namespace GZipTest.Processing.Write
{
    class CompressionDataWriter : AbstractDataWriter
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CompressionDataWriter));

        public static readonly int WHOLE_HEADER_SIZE = Constants.FILE_HEADER_LENGTH + sizeof(int) + sizeof(long);

        public static readonly int CHUNK_HEADER_SIZE = 2 * sizeof(int) + sizeof(long);

        internal CompressionDataWriter(string inputFileName, string outputFileName, int chunkSize)
            : base(outputFileName)
        {
            long size = new FileInfo(inputFileName).Length;
            byte[] ascii = Encoding.ASCII.GetBytes(Constants.FILE_HEADER);
            _outputFile.Write(ascii);
            _outputFile.Write(BitConverter.GetBytes(size), 0, sizeof(long));
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
