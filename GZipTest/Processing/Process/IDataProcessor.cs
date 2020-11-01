using GZipTest.Chunks;
using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Processing.Process
{
    interface IDataProcessor
    {
        public DataChunk ProcessData(DataChunk chunk);
    }
}
