using GZipTest.Processing.Process;
using GZipTest.Processing.Read;
using GZipTest.Processing.Write;
using System;
using System.IO;

namespace GZipTest.Processing
{
    class CompressData : AbstractReadProcessWrite
    {
        protected override void CheckPreconditions(ReadProcessWriteInput input)
        {
            base.CheckPreconditions(input);
            long size = new FileInfo(input.InputFileName).Length;
            long chunks = size / input.ChunkSize;
            long estimatedMaximumSize = size + CompressionDataWriter.WHOLE_HEADER_SIZE + (chunks * (CompressionDataWriter.CHUNK_HEADER_SIZE + Constants.GZIP_HEADER_AND_FOOTER_LENGTH));
            DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(input.OutputFileName));
            if(driveInfo.AvailableFreeSpace < estimatedMaximumSize)
            {
                double maxSpace = Math.Round(estimatedMaximumSize / 1024.0d * 1024.0d, 2);
                double driveSpace = Math.Round(driveInfo.AvailableFreeSpace / 1024.0d * 1024.0d, 2);
                Console.WriteLine($"Free space on destination drive ({driveSpace} MB) might not be enough for the compression to succeed (Pessimistic space needed = {maxSpace} MB) do you wish to continue  ? [Y/N]");
                string result = Console.ReadLine();
                if(result != "Y")
                {
                    throw new ProcessingException("Aborted by user.");
                }
            }
        }
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
            return new CompressionDataWriter(input.InputFileName, input.OutputFileName, input.ChunkSize);
        }
    }
}
