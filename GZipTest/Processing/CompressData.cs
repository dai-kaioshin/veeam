using GZipTest.Chunks;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace GZipTest.Processing
{
    internal class CompressData : AbstractReadProcessWrite
    {

        protected override void CreateOutputFile(ReadProcessWriteInput input)
        {
            using (var outputFileStream = File.Create(input.OutputFileName))
            {
                byte[] ascii = Encoding.ASCII.GetBytes(FILE_HEADER);
                outputFileStream.Write(ascii);
                outputFileStream.Write(BitConverter.GetBytes(input.ChunkSize), 0, sizeof(int));
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
                    bool dequeued = processingSyncContext.Queue.Dequeue(out chunk);

                    if (dequeued)
                    {
                        _log.Debug($"Compressing chunk : {chunk}");
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
                            {
                                gzipStream.Write(chunk.Data, 0, chunk.Data.Length);
                                gzipStream.Flush();
                                DataChunk compressedChunk = new DataChunk(chunk.Part, chunk.Position, chunk.DecompressedSize, memoryStream.ToArray());

                                writeSyncContext.Queue.Enqueue(compressedChunk);
                                writeSyncContext.SyncHandle.Set();
                            }
                        }
                    }

                    processingSyncContext.SyncHandle.Release();

                    if(!dequeued && processingSyncContext.Finished)
                    {
                        return;
                    }
                }
            }
            catch(Exception exception)
            {
                processingSyncContext.SetError(new ProcessingException($"Exception during compression in thread Thread : {Thread.CurrentThread.Name}", exception));
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
                using (var outputFileStream = new FileStream(outputFileName, FileMode.Append))
                {
                    while (true)
                    {
                        DataChunk chunk = null;
                        bool canFinish = false;
                        syncContext.Queue.Dequeue(out chunk);
                        if (chunk == null && syncContext.Finished)
                        {
                            canFinish = true;
                        }
                        if (canFinish)
                        {
                            return;
                        }
                        if (chunk == null)
                        {
                            syncContext.SyncHandle.WaitOne();
                            continue;
                        }
                        _log.Debug($"Writing chunk : {chunk}");
                        outputFileStream.Write(BitConverter.GetBytes(chunk.Part), 0, sizeof(int));
                        outputFileStream.Write(BitConverter.GetBytes(chunk.Position), 0, sizeof(long));
                        outputFileStream.Write(BitConverter.GetBytes(chunk.Data.Length), 0, sizeof(int));
                        outputFileStream.Write(chunk.Data, 0, chunk.Data.Length);
                    }
                }
            }
            catch(Exception exception)
            {
                syncContext.SetError(new ProcessingException($"Error writing compressed data to output file : {exception.Message}", exception));
            }
            finally
            {
                syncContext.DoneEvent.Set();
            }
        }

        protected override bool ReadNextChunk(Stream inputStream, int chunkSize, byte[] buffer, ref int part, ref long position, out DataChunk dataChunk)
        {
            dataChunk = null;
            if (inputStream.Position >= inputStream.Length)
            {
                return false;
            }
            int read = inputStream.Read(buffer, 0, chunkSize);
            byte[] chunk = new byte[read];
            Array.Copy(buffer, chunk, read);
            dataChunk = new DataChunk(part++, position, chunk);
            position += read;
            return true;
        }
    }
}
