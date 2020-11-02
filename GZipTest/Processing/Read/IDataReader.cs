using GZipTest.Chunks;
using System;

namespace GZipTest.Processing
{
    interface IDataReader : IDisposable
    {
        public bool ReadNext(out DataChunk chunk);
    }
}
