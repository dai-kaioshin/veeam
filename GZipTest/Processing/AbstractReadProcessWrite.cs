using GZipTest.Chunks;
using GZipTest.Queue;
using System;
using System.IO;
using System.Threading;

namespace GZipTest.Processing
{
    abstract class AbstractReadProcessWrite : IReadProcessWrite
    {
        protected static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(AbstractReadProcessWrite));

        protected static string FILE_HEADER = "Veam-Compression-v1.0";

        protected static int FILE_HEADER_LENGTH = 21;

        public void ReadProcessWrite(ReadProcessWriteInput input)
        {
            CheckPreconditions(input);
            try
            {
                CreateOutputFile(input);

                int processingThreads = Environment.ProcessorCount;

                ProcessingSyncContext<Semaphore> processingSyncContext = new ProcessingSyncContext<Semaphore>(new FixedSizeQueue<DataChunk>((uint)(2 * processingThreads)), new Semaphore(0, processingThreads));

                ProcessingSyncContext<AutoResetEvent> writingSyncContext = new ProcessingSyncContext<AutoResetEvent>(new ThreadSafeQueue<DataChunk>(), new AutoResetEvent(false));

                StartProcessingThreads(processingSyncContext, writingSyncContext, processingThreads);
                StartWritingThread(writingSyncContext, input.OutputFileName);
                processingSyncContext.SyncHandle.Release(processingThreads);
                ReadData(input, processingSyncContext, writingSyncContext);
            }
            catch(Exception)
            {
                if(File.Exists(input.OutputFileName))
                {
                    try
                    {
                        File.Delete(input.OutputFileName);
                    }
                    catch(Exception ex)
                    {
                        _log.Error("Failed to delete output file after error.", ex);
                    }
                }
                throw;
            }
        }

        protected virtual void CheckPreconditions(ReadProcessWriteInput input)
        {
            if (!File.Exists(input.InputFileName))
            {
                throw new ProcessingException($"File : {input.InputFileName} does not exist.");
            }
            if (File.Exists(input.OutputFileName))
            {
                throw new ProcessingException($"Output file {input.OutputFileName} already exists.");
            }
        }

        protected virtual void CreateOutputFile(ReadProcessWriteInput input)
        {
            using (var outputFileStream = File.Create(input.OutputFileName))
            {
            }
        }

        protected virtual void PrepareInputFile(Stream inputFileStream, ReadProcessWriteInput input, out int chunkSize)
        {
            chunkSize = input.ChunkSize;
        }

        private void ReadData(ReadProcessWriteInput input, ProcessingSyncContext<Semaphore> processingSyncContest, ProcessingSyncContext<AutoResetEvent> writingSyncContext)
        {
            bool errorOccured = false;
            try
            {
                using (FileStream fileStream = new FileStream(input.InputFileName, FileMode.Open, FileAccess.Read))
                {
                    int chunkSize;
                    PrepareInputFile(fileStream, input, out chunkSize);
                    byte[] buffer = new byte[chunkSize * 2];
                    int part = 0;
                    long position = 0;
                    DataChunk chunk;
                    while (ReadNextChunk(fileStream, chunkSize, buffer, ref part, ref position, out chunk))
                    {
                        bool enqueued = false;
                        while (!enqueued)
                        {
                            _log.Debug($"Enqueueing chunk : {chunk}");
                            enqueued = processingSyncContest.Queue.Enqueue(chunk, 100);
                            if(!enqueued)
                            {
                                Exception exception = processingSyncContest.Exception;
                                if(exception != null)
                                {
                                    throw exception;
                                }
                            }
                        }
                    }
                }
            }
            catch(ProcessingException processingException)
            {
                errorOccured = true;
                throw;
            }
            catch(Exception exception)
            {
                errorOccured = true;
                throw new ProcessingException("Unexpected error occured.", exception);
            }
            finally
            {
                processingSyncContest.SetFinished();
                int count;
                while((count = processingSyncContest.SyncHandle.Release()) > 1){ }

                processingSyncContest.DoneEvent.WaitOne();

                writingSyncContext.SetFinished();
                writingSyncContext.DoneEvent.WaitOne();

                if(!errorOccured)
                {
                    Exception exception = processingSyncContest.Exception;
                    if(exception != null)
                    {
                        if(exception.GetType() == typeof(ProcessingException))
                        {
                            throw exception;
                        }
                        else
                        {
                            throw new ProcessingException("Unexpected error occured.", exception);
                        }
                    }
                    exception = writingSyncContext.Exception;
                    if (exception != null)
                    {
                        if (exception.GetType() == typeof(ProcessingException))
                        {
                            throw exception;
                        }
                        else
                        {
                            throw new ProcessingException("Unexpected error occured.", exception);
                        }
                    }
                }
            }
        }

        private void StartProcessingThreads(ProcessingSyncContext<Semaphore> processingSyncContext, ProcessingSyncContext<AutoResetEvent> writeSyncContext, int processingThreads)
        {
            for(int i = 0; i < processingThreads; i++)
            {
                new Thread(() => DoProcessing(processingSyncContext, writeSyncContext)).Start();
            }
        }

        private void StartWritingThread(ProcessingSyncContext<AutoResetEvent> writingSyncContext, string outputFileName)
        {
            new Thread(() => DoWriting(writingSyncContext, outputFileName)).Start();
        }

        protected abstract bool ReadNextChunk(Stream inputStream, int chunkSize, byte[] buffer, ref int part, ref long position, out DataChunk dataChunk);

        protected abstract void DoProcessing(ProcessingSyncContext<Semaphore> processingSyncContext, ProcessingSyncContext<AutoResetEvent> writeSyncContext);

        protected abstract void DoWriting(ProcessingSyncContext<AutoResetEvent> syncContext, string outputFileName);
    }
}
