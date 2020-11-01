using GZipTest.Chunks;
using System;

namespace GZipTest.Processing
{
    internal interface IDataReader : IDisposable
    {
        public bool ReadNext(out DataChunk chunk);
    }
}
