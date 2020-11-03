using GZipTest.Chunks;
using System;

namespace GZipTest.Processing
{
    interface IDataReader : IDisposable
    {
        public long Chunks
        {
            get;
        }

        public bool ReadNext(out DataChunk chunk);
    }
}
