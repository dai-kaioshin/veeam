using GZipTest.Processing.Process;
using GZipTest.Processing.Read;
using GZipTest.Processing.Write;
using System;
using System.IO;
using System.Text;

namespace GZipTest.Processing
{
    internal class DecompressData : AbstractReadProcessWrite
    {
        protected override void CheckPreconditions(ReadProcessWriteInput input)
        {
            long decompressedSize;
            base.CheckPreconditions(input);
            using(FileStream inputFileStream = File.OpenRead(input.InputFileName))
            {
                byte[] buffer = new byte[Constants.FILE_HEADER_LENGTH];
                inputFileStream.Read(buffer, 0, buffer.Length);
                string headerName = Encoding.ASCII.GetString(buffer);
                if(headerName != Constants.FILE_HEADER)
                {
                    throw new ProcessingException($"File : {input.InputFileName} is not a valid veeam archive.");
                }
                inputFileStream.Read(buffer, 0, sizeof(long));
                decompressedSize = BitConverter.ToInt64(buffer);
            }
            DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(input.OutputFileName));
            if (driveInfo.AvailableFreeSpace < decompressedSize)
            {
                throw new ProcessingException($"Free space on destination drive {Math.Round(driveInfo.AvailableFreeSpace / 1024.0d * 1024.0d, 2)} MB is not be enough for the decompression to succeed. Decompressed file size is : {Math.Round(decompressedSize / 1024.0d * 1024.0d, 2)} MB");
            }
        }

        protected override IDataProcessor CreateDataProcessor()
        {
            return new DecompressionDataProcessor();
        }

        protected override IDataReader CreateDataReader(ReadProcessWriteInput input)
        {
            return new DecompressionDataReader(input.InputFileName);
        }

        protected override IDataWriter CreateDataWriter(ReadProcessWriteInput input)
        {
            return new DecompressionDataWriter(input.OutputFileName);
        }
    }
}
