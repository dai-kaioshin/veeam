using GZipTest.Chunks;
using GZipTest.Processing.Process;
using GZipTest.Processing.Read;
using GZipTest.Processing.Write;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace GZipTest.Processing
{
    internal class CompressData : AbstractReadProcessWrite
    {
        protected override IDataProcessor CreateDataProcessor()
        {
            return new CompressionDataProcessor();
        }

        protected override IDataReader CreateDataReader(ReadProcessWriteInput input)
        {
            return new CompressionDataReader(input.InputFileName, input.ChunkSize);
        }

        protected override IDataWriter CreateDataWriter(ReadProcessWriteInput input)
        {
            return new CompressionDataWriter(input.OutputFileName, input.ChunkSize);
        }
    }
}
