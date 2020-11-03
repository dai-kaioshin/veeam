using GZipTest.Chunks;
using GZipTest.Processing.Process;
using GZipTest.Processing.Write;
using System;
using System.IO;
using System.Threading;

namespace GZipTest.Processing
{
    abstract class AbstractReadProcessWrite : IReadProcessWrite
    {
        protected static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(AbstractReadProcessWrite));

        public event ProcessingProgress ProcessingProgress;

        public void ReadProcessWrite(ReadProcessWriteInput input)
        {
            bool error = false;
            Thread[] processors = null;
            Thread writer = null;
            CheckPreconditions(input);
            try
            {
                CreateOutputFile(input);

                int processingThreads = Environment.ProcessorCount;

                using (IDataReader dataReader = CreateDataReader(input))
                {
                    ProcessingSyncContext processingSyncContext = new ProcessingSyncContext(processingThreads, processingThreads * 2);
                    WritingSyncContext writingSyncContext = new WritingSyncContext(processingThreads, processingThreads * 2);

                    long totalChunks = dataReader.Chunks;

                    processors = StartProcessingThreads(processingSyncContext, writingSyncContext, processingThreads);
                    writer = StartWritingThread(writingSyncContext, input, totalChunks);
                    ReadData(dataReader, processingSyncContext, writingSyncContext);
                }
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                error = true;
                throw;
            }
            finally
            {
                if (processors != null)
                {
                    for (int i = 0; i < processors.Length; i++)
                    {
                        JoinOrAbortThread(processors[i]);
                    }
                    JoinOrAbortThread(writer);
                }
                if (error)
                {
                    if (File.Exists(input.OutputFileName))
                    {
                        try
                        {
                            File.Delete(input.OutputFileName);
                        }
                        catch (Exception ex)
                        {
                            _log.Error("Failed to delete output file after error.", ex);
                        }
                    }
                }
            }
        }
        protected abstract IDataReader CreateDataReader(ReadProcessWriteInput input);

        protected abstract IDataWriter CreateDataWriter(ReadProcessWriteInput input);

        protected abstract IDataProcessor CreateDataProcessor();

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

        private void CreateOutputFile(ReadProcessWriteInput input)
        {
            using (var file = File.Create(input.OutputFileName))
            {
            }
        }

        private void CheckContextException(SyncContextBase syncContext)
        {
            Exception exception = syncContext.Exception;
            if(exception != null)
            {
                if(exception.GetType() == typeof(ProcessingException))
                {
                    throw exception;
                }
                throw new ProcessingException($"Unexpected error : {exception.Message}", exception);
            }
        }

        private void ReadData(IDataReader dataReader, ProcessingSyncContext processingSyncContest, WritingSyncContext writingSyncContext)
        {
            bool errorOccured = false;
            try
            {
                DataChunk chunk;
                while (dataReader.ReadNext(out chunk))
                {
                    bool enqueued = false;
                    while (!enqueued)
                    {
                        _log.Debug($"Enqueueing chunk : {chunk}");
                        enqueued = processingSyncContest.Queue.Enqueue(chunk, 100);
                        if (!enqueued)
                        {
                            CheckContextException(processingSyncContest);
                        }
                    }
                    CheckContextException(processingSyncContest);
                    CheckContextException(writingSyncContext);

                    processingSyncContest.SetAllSyncEvents();
                }
            }
            catch(ProcessingException)
            {
                errorOccured = true;
                throw;
            }
            catch(Exception exception)
            {
                errorOccured = true;
                throw new ProcessingException($"Unexpected error occured : {exception.Message}", exception);
            }
            finally
            {
                processingSyncContest.SetFinished();
                processingSyncContest.SetAllSyncEvents();

                processingSyncContest.WaitAllDoneEvents();

                writingSyncContext.SetFinished();
                writingSyncContext.SyncEvents[0].Set();
                writingSyncContext.DoneEvent.WaitOne();

                if(!errorOccured)
                {
                    CheckContextException(processingSyncContest);
                    CheckContextException(writingSyncContext);
                }
            }
        }

        private Thread[] StartProcessingThreads(ProcessingSyncContext processingSyncContext, WritingSyncContext writeSyncContext, int processingThreads)
        {
            IDataProcessor dataProcessor = CreateDataProcessor();
            Thread[] threads = new Thread[processingThreads];
            for(int i = 0; i < processingThreads; i++)
            {
                string threadName = "ProcessingThread-" + i;
                int threadIdx = i;
                Thread thread = new Thread(() => DoProcessing(processingSyncContext, writeSyncContext, dataProcessor, threadIdx))
                {
                    Name = threadName,
                    Priority = ThreadPriority.AboveNormal
                };
                threads[i] = thread;
                thread.Start();
            }
            return threads;
        }

        private Thread StartWritingThread(WritingSyncContext writingSyncContext, ReadProcessWriteInput input, long totalChunks)
        {
            Thread thread = new Thread(() => DoWriting(writingSyncContext, input, totalChunks))
            {
                Name = "WriterThread",
                Priority = ThreadPriority.AboveNormal
            };
            thread.Start();
            return thread;
        }

        private void DoProcessing(ProcessingSyncContext processingSyncContext, WritingSyncContext writeSyncContext, IDataProcessor dataProcessor, int threadIdx)
        {
            try
            {
                while (true)
                {
                    DataChunk chunk = null;
                    bool canFinish = false;

                    bool dequeued = processingSyncContext.Queue.Dequeue(out chunk);

                    if (!dequeued && processingSyncContext.Finished)
                    {
                        _log.Debug($"Processing thread {Thread.CurrentThread.Name} exists.");
                        return;
                    }
                    Exception exception = writeSyncContext.Exception;

                    if (exception != null)
                    {
                        processingSyncContext.SetError(exception);
                        processingSyncContext.SetFinished();
                        return;
                    }

                    exception = processingSyncContext.Exception;

                    if(exception != null)
                    {
                        return;
                    }

                    if (!dequeued)
                    {
                        processingSyncContext.SyncEvents[threadIdx].WaitOne();
                        continue;
                    }

                    bool enqueued = false;
                    DataChunk processedChunk = dataProcessor.ProcessData(chunk);
                    while (!enqueued)
                    {
                        enqueued = writeSyncContext.Queue.Enqueue(processedChunk, 100);
                        if(!enqueued)
                        {
                            exception = writeSyncContext.Exception;
                            if(exception != null)
                            {
                                processingSyncContext.SetError(exception);
                                processingSyncContext.SetFinished();
                                return;
                            }
                        }
                    }
                    writeSyncContext.SyncEvents[threadIdx].Set();
                }
            }
            catch (Exception exception)
            {
                _log.Error($"Error during processing: {exception}");
                processingSyncContext.SetError(new ProcessingException($"Exception during processing in Thread {Thread.CurrentThread.Name} : {exception.Message}", exception));
            }
            finally
            {
                _log.Debug($"Processing thread : {Thread.CurrentThread.Name} : done.");
                processingSyncContext.DoneEvents[threadIdx].Set();
            }
        }

        private void DoWriting(WritingSyncContext syncContext, ReadProcessWriteInput input, long totalChunks)
        {
            try
            {
                using (var dataWriter = CreateDataWriter(input))
                {
                    int chunksProcessed = 0;
                    double percentDone = 0.0d, prevNotifiedPercentDone = 0.0d;

                    while (true)
                    {
                        DataChunk chunk = null;
                        bool canFinish = false;
                        bool dequeued = syncContext.Queue.Dequeue(out chunk);
                        if (!dequeued && syncContext.Finished)
                        {
                            return;
                        }
                        if (!dequeued)
                        {
                            syncContext.WaitAnySyncEvent();
                            continue;
                        }
                        dataWriter.WriteChunk(chunk);
                        chunksProcessed++;
                        percentDone = chunksProcessed / (double)totalChunks;
                        if(percentDone - prevNotifiedPercentDone >= 0.01)
                        {
                            ProcessingProgress?.Invoke(Math.Ceiling(percentDone * 100));
                            prevNotifiedPercentDone = percentDone;
                        }

                    }
                }
            }
            catch (Exception exception)
            {
                _log.Error($"Error during writing: {exception}");
                syncContext.SetError(new ProcessingException($"Error writing data to output file : {exception.Message}", exception));
            }
            finally
            {
                _log.Debug($"Writer thread done.");
                syncContext.DoneEvent.Set();
            }
        }

        private void JoinOrAbortThread(Thread thread)
        {
            bool joined = false;
            try
            {
                joined = thread.Join(100);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            finally
            {
                if (!joined)
                {
                    _log.Error($"Thread {thread.Name} won't join - aborting.");
                    thread.Abort();
                }
            }
        }
    }
}
