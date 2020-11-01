using GZipTest.Chunks;
using System.IO;

namespace GZipTest.Processing.Write
{
    abstract class AbstractDataWriter : IDataWriter
    {
        protected FileStream _outputFile;

        protected bool _disposed = false;

        internal AbstractDataWriter(string outputFileName)
        {
            _outputFile = File.OpenWrite(outputFileName);
        }
        public void Dispose()
        {
            Dispose(true);
        }

        public abstract void WriteChunk(DataChunk chunk);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _outputFile.Dispose();
                _outputFile = null;
            }

            _disposed = true;
        }
    }
}
