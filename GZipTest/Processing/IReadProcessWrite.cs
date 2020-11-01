using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Processing
{
    class ReadProcessWriteInput
    {
        public string InputFileName { get; }
        public string OutputFileName { get; }
        public int ChunkSize { get; }

        public ReadProcessWriteInput(string inputFileName, string outputFileName, int chunkSize = 1024 * 1024)
        {
            InputFileName = inputFileName;
            OutputFileName = outputFileName;
            ChunkSize = chunkSize;
        }
    }

    interface IReadProcessWrite
    {
        void ReadProcessWrite(ReadProcessWriteInput input);
    }
}
