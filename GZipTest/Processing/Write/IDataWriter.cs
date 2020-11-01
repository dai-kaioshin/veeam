using GZipTest.Chunks;
using System;

namespace GZipTest.Processing.Write
{
    interface IDataWriter : IDisposable
    {
        public void WriteChunk(DataChunk chunk);
    }
}
