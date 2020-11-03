namespace GZipTest.Chunks
{
    class DataChunk
    {
        internal int Part
        {
            get;
        }

        internal long Position
        {
            get;
        }

        internal int DecompressedSize
        {
            get;
        }

        internal byte[] Data
        {
            get;
        }


        internal DataChunk(int part, long position, byte[] data)
        {
            Part = part;
            Position = position;
            Data = data;
            DecompressedSize = data.Length;
        }

        internal DataChunk(int part, long position, int decompressedSize, byte[] data)
        {
            Part = part;
            Position = position;
            Data = data;
            DecompressedSize = decompressedSize;
        }

        public override string ToString()
        {
            return $"DataChunk {{Part = {Part}, Position = {Position} DecompressedSize = {DecompressedSize}, Data = {Data.Length} bytes}}";
        }
    }
}
