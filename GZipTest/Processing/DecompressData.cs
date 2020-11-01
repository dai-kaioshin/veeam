using GZipTest.Chunks;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace GZipTest.Processing
{
    internal class DecompressData : AbstractReadProcessWrite
    {
        protected override void CheckPreconditions(ReadProcessWriteInput input)
        {
            base.CheckPreconditions(input);
            using(FileStream inputFileStream = File.OpenRead(input.InputFileName))
            {
                byte[] buffer = new byte[FILE_HEADER_LENGTH];
                inputFileStream.Read(buffer, 0, buffer.Length);
                string headerName = Encoding.ASCII.GetString(buffer);
                if(headerName != FILE_HEADER)
                {
                    throw new ProcessingException($"File : {input.InputFileName} is not a valid veeam archive.");
                }
            }
        }
        protected override void DoProcessing(ProcessingSyncContext<Semaphore> processingSyncContext, ProcessingSyncContext<AutoResetEvent> writeSyncContext)
        {
            try
            {
                while (true)
                {
                    DataChunk chunk = null;
                    bool canFinish = false;
                    processingSyncContext.SyncHandle.WaitOne();
                    processingSyncContext.Queue.Dequeue(out chunk);


                    if (chunk == null && processingSyncContext.Finished)
                    {
                        return;
                    }

                    using (var memoryStream = new MemoryStream(chunk.Data))
                    {
                        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                        {
                            _log.Debug($"Decompressing chunk : {chunk}");
                            byte[] buffer = new byte[chunk.DecompressedSize];
                            int read = gzipStream.Read(buffer, 0, chunk.DecompressedSize);
                            _log.Debug($"READ : {read}");

                            writeSyncContext.Queue.Enqueue(new DataChunk(chunk.Part, chunk.Position, read, buffer));
                            writeSyncContext.SyncHandle.Set();
                        }
                    }
                    processingSyncContext.SyncHandle.Release();
                }
            }
            catch(Exception exception)
            {
                processingSyncContext.SetError(exception);
            }
            finally
            {
                processingSyncContext.DoneEvent.Set();
            }
        }

        protected override void DoWriting(ProcessingSyncContext<AutoResetEvent> syncContext, string outputFileName)
        {
            try
            {
                using (var outputFileStream = File.OpenWrite(outputFileName))
                {
                    while (true)
                    {
                        DataChunk chunk = null;
                        bool canFinish = false;
                        syncContext.Queue.Dequeue(out chunk);
                        if (chunk == null && syncContext.Finished)
                        {
                            return;
                        }

                        if (chunk == null)
                        {
                            syncContext.SyncHandle.WaitOne();
                            continue;
                        }

                        _log.Debug($"Writing chunk : {chunk}");

                        if (outputFileStream.Length < chunk.Position)
                        {
                            outputFileStream.SetLength(chunk.Position + chunk.DecompressedSize);
                        }
                        outputFileStream.Seek(chunk.Position, SeekOrigin.Begin);
                        outputFileStream.Write(chunk.Data, 0, chunk.DecompressedSize);

                    }
                }
            }
            catch(Exception exception)
            {
                syncContext.SetError(exception);
            }
            finally
            {
                syncContext.DoneEvent.Set();
            }
        }

        protected override void PrepareInputFile(Stream inputFileStream, ReadProcessWriteInput input, out int chunkSize)
        {
            inputFileStream.Position = FILE_HEADER_LENGTH;
            byte[] buffer = new byte[sizeof(int)];
            inputFileStream.Read(buffer, 0, sizeof(int));
            chunkSize = BitConverter.ToInt32(buffer);
        }

        protected override bool ReadNextChunk(Stream inputStream, int chunkSize, byte[] buffer, ref int part, ref long position, out DataChunk dataChunk)
        {
            dataChunk = null;
            if (inputStream.Position >= inputStream.Length)
            {
                return false;
            }

            inputStream.Read(buffer, 0, sizeof(int));
            part = BitConverter.ToInt32(buffer);
            inputStream.Read(buffer, 0, sizeof(long));
            position = BitConverter.ToInt64(buffer);
            inputStream.Read(buffer, 0, sizeof(int));
            int compressedChunkSize = BitConverter.ToInt32(buffer);
            int read = inputStream.Read(buffer, 0, compressedChunkSize);
            byte[] chunk = new byte[read];
            Array.Copy(buffer, chunk, read);
            dataChunk = new DataChunk(part, position, chunkSize, chunk);
            return true;
        }
    }
}
