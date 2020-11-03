namespace GZipTest.Processing
{
    class ReadProcessWriteInput
    {
        public string InputFileName { get; }
        public string OutputFileName { get; }
        public int ChunkSize { get; }

        internal ReadProcessWriteInput(string inputFileName, string outputFileName, int chunkSize = 1024 * 1024)
        {
            InputFileName = inputFileName;
            OutputFileName = outputFileName;
            ChunkSize = chunkSize;
        }
    }

    delegate void ProcessingProgress(double percentDone); 

    interface IReadProcessWrite
    {
        event ProcessingProgress ProcessingProgress;

        void ReadProcessWrite(ReadProcessWriteInput input);
    }
}
