using GZipTest.Chunks;
using System.Data;
using System.IO;

namespace GZipTest.Processing.Read
{
    abstract class AbstractDataReader : IDataReader
    {
        protected FileStream _fileStream;

        private bool _disposed = false;

        protected byte[] _buffer;
        
        internal AbstractDataReader(string inputFileName)
        {
            _fileStream = File.OpenRead(inputFileName);
        }

        public abstract bool ReadNext(out DataChunk chunk);

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _fileStream.Dispose();
                _fileStream = null;
            }

            _disposed = true;
        }
    }
}
