using GZipTest.Chunks;
using System;

namespace GZipTest.Processing.Read
{
    class CompressionDataReader : AbstractDataReader
    {
        private readonly int _chunkSize;

        private int _part = 0;

        internal CompressionDataReader(string inputFileName, int chunkSize) 
            : base(inputFileName)
        {
            _chunkSize = chunkSize;
            _buffer = new byte[_chunkSize];
        }

        public override long Chunks
        {
            get
            {
                return (long) Math.Ceiling(_fileStream.Length / (double)_chunkSize);
            }
        }

        public override bool ReadNext(out DataChunk chunk)
        {
            if(_fileStream.Position >= _fileStream.Length)
            {
                chunk = null;
                return false;
            }
            long position = _fileStream.Position;
            int read = _fileStream.Read(_buffer, 0, _chunkSize);
            byte[] result = new byte[read];
            Array.Copy(_buffer, result, read);
            chunk = new DataChunk(_part++, position, read, result);
            return true;
        }
    }
}
