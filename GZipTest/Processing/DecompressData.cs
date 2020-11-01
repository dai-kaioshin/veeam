using GZipTest.Chunks;
using GZipTest.Processing.Process;
using GZipTest.Processing.Read;
using GZipTest.Processing.Write;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace GZipTest.Processing
{
    internal class DecompressData : AbstractReadProcessWrite
    {
        protected override void CheckPreconditions(ReadProcessWriteInput input)
        {
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
