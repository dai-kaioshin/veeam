namespace GZipTest.Processing
{
    public enum ReadProcessWriteMode
    {
        Compress,
        Decompress
    }

    class ReadProcessWriteFactory
    {
        public static IReadProcessWrite Create(ReadProcessWriteMode mode)
        {
            switch(mode)
            {
                case ReadProcessWriteMode.Compress:
                    return new CompressData();
                case ReadProcessWriteMode.Decompress:
                    return new DecompressData();
                default:
                    throw new ProcessingException("Unknown processing mode.");
            }
        }
    }
}
