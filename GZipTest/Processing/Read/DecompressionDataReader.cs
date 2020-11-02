using GZipTest.Chunks;
using System;

namespace GZipTest.Processing.Read
{
    class DecompressionDataReader : AbstractDataReader
    {
        private int _decompressedChunkSize;

        private int _part = 0;
        internal DecompressionDataReader(string inputFileName)
            : base(inputFileName)
        {
            _fileStream.Position = Constants.FILE_HEADER_LENGTH + sizeof(long);
            byte[] buffer = new byte[sizeof(int)];
            _fileStream.Read(buffer, 0, sizeof(int));
            _decompressedChunkSize = BitConverter.ToInt32(buffer);
            _buffer = new byte[_decompressedChunkSize + Constants.GZIP_HEADER_AND_FOOTER_LENGTH];

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

            int read = _fileStream.Read(_buffer, 0, compressedChunkSize);
            byte[] result = new byte[read];
            Array.Copy(_buffer, result, read);
            chunk = new DataChunk(_part++, position, _decompressedChunkSize, result);
            return true;
        }
    }
}
