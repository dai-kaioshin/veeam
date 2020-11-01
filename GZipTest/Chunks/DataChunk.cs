namespace GZipTest.Chunks
{
    class DataChunk
    {

        public readonly static DataChunk END = new DataChunk(-1, -1, null);

        public int Part
        {
            get;
        }

        public long Position
        {
            get;
        }

        public int DecompressedSize
        {
            get;
        }

        public byte[] Data
        {
            get;
        }


        public DataChunk(int part, long position, byte[] data)
        {
            Part = part;
            Position = position;
            Data = data;
            DecompressedSize = data.Length;
        }

        public DataChunk(int part, long position, int decompressedSize, byte[] data)
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
