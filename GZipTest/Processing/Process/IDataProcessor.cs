using GZipTest.Chunks;

namespace GZipTest.Processing.Process
{
    interface IDataProcessor
    {
        public DataChunk ProcessData(DataChunk chunk);
    }
}
