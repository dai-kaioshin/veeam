using GZipTest.Chunks;
using System;
using System.Security.Cryptography;

namespace GZipTest.Processing.Read
{
    class DecompressionDataReader : AbstractDataReader
    {
        private int _decompressedChunkSize;
        internal DecompressionDataReader(string inputFileName)
            : base(inputFileName)
        {
            _fileStream.Position = Constants.FILE_HEADER_LENGTH;
            byte[] buffer = new byte[sizeof(int)];
            _fileStream.Read(buffer, 0, sizeof(int));
            _decompressedChunkSize = BitConverter.ToInt32(buffer);
            _buffer = new byte[(int)Math.Floor(_decompressedChunkSize * 1.3)];
        }

        public override bool ReadNext(out DataChunk chunk)
        {
            chunk = null;
            if (_fileStream.Position >= _fileStream.Length)
            {
                return false;
            }

            _fileStream.Read(_buffer, 0, sizeof(int));
            int part = BitConverter.ToInt32(_buffer);

            _fileStream.Read(_buffer, 0, sizeof(long));
            long position = BitConverter.ToInt64(_buffer);

            _fileStream.Read(_buffer, 0, sizeof(int));
            int compressedChunkSize = BitConverter.ToInt32(_buffer);

            int read = _fileStream.Read(_buffer, 0, compressedChunkSize);
            byte[] result = new byte[read];
            Array.Copy(_buffer, result, read);
            chunk = new DataChunk(part, position, _decompressedChunkSize, result);
            return true;
        }
    }
}
