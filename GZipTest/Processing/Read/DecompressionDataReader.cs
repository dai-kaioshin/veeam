using GZipTest.Chunks;
using log4net;
using System;

namespace GZipTest.Processing.Read
{
    class DecompressionDataReader : AbstractDataReader
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DecompressionDataReader));

        private int _decompressedChunkSize;

        private int _part = 0;
        internal DecompressionDataReader(string inputFileName)
            : base(inputFileName)
        {
            _fileStream.Position = Constants.FILE_HEADER_LENGTH + sizeof(long);
            byte[] buffer = new byte[sizeof(int)];
            _fileStream.Read(buffer, 0, sizeof(int));
            _decompressedChunkSize = BitConverter.ToInt32(buffer);

            // The size of GZIP compressed chunk can be much bigger then original one in some cases.
            _buffer = new byte[(int)Math.Round(_decompressedChunkSize * 1.2d)];
        }

        public override bool ReadNext(out DataChunk chunk)
        {
            chunk = null;
            if (_fileStream.Position >= _fileStream.Length)
            {
                return false;
            }

            _fileStream.Read(_buffer, 0, sizeof(long));
            long position = BitConverter.ToInt64(_buffer);

            _fileStream.Read(_buffer, 0, sizeof(int));
            int compressedChunkSize = BitConverter.ToInt32(_buffer);

            if(compressedChunkSize > _buffer.Length)
            {
                _log.Warn($"Chunk size {compressedChunkSize} bigger then buffer {_buffer.Length} - resizing.");
                _buffer = new byte[compressedChunkSize];
            }

            int read = _fileStream.Read(_buffer, 0, compressedChunkSize);
            byte[] result = new byte[read];
            Array.Copy(_buffer, result, read);
            chunk = new DataChunk(_part++, position, _decompressedChunkSize, result);
            return true;
        }
    }
}
