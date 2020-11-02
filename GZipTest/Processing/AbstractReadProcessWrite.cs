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
        protected abstract IDataReader CreateDataReader(ReadProcessWriteInput input);

        protected abstract IDataWriter CreateDataWriter(ReadProcessWriteInput input);

        protected abstract IDataProcessor CreateDataProcessor();

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
                int processingQueueSize = processingThreads * 2;
                

                ProcessingSyncContext processingSyncContext = new ProcessingSyncContext(processingThreads, processingQueueSize);
                WritingSyncContext writingSyncContext = new WritingSyncContext(processingThreads);

                processors = StartProcessingThreads(processingSyncContext, writingSyncContext, processingThreads);
                writer = StartWritingThread(writingSyncContext, input);
                ReadData(input, processingSyncContext, writingSyncContext);
            }
            catch(Exception exception)
            {
                _log.Error(exception);
                error = true;
                throw;
            }
            finally
            {
                if(processors != null)
                {
                    for(int i = 0; i < processors.Length; i++)
                    {
                        JoinOrAbortThread(processors[i]);
                    }
                    JoinOrAbortThread(writer);
                }
                if(error)
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

        private void ReadData(ReadProcessWriteInput input, ProcessingSyncContext processingSyncContest, WritingSyncContext writingSyncContext)
        {
            bool errorOccured = false;
            try
            {
                using (IDataReader reader = CreateDataReader(input))
                {
                    DataChunk chunk;
                    while (reader.ReadNext(out chunk))
                    {
                        bool enqueued = false;
                        while (!enqueued)
                        {
                            _log.Debug($"Enqueueing chunk : {chunk}");
                            enqueued = processingSyncContest.Queue.Enqueue(chunk, 100);
                            if (!enqueued)
                            {
                                Exception exception = processingSyncContest.Exception;
                                if (exception != null)
                                {
                                    throw exception;
                                }
                                exception = writingSyncContext.Exception;
                                if (exception != null)
                                {
                                    throw exception;
                                }
                            }
                        }
                        processingSyncContest.SetAllSyncEvents();
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
                processingSyncContest.SetAllSyncEvents();

                processingSyncContest.WaitAllDoneEvents();

                writingSyncContext.SetFinished();
                writingSyncContext.SyncEvents[0].Set();
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
                    Name = threadName
                };
                threads[i] = thread;
                thread.Start();
            }
            return threads;
        }

        private Thread StartWritingThread(WritingSyncContext writingSyncContext, ReadProcessWriteInput input)
        {
            Thread thread = new Thread(() => DoWriting(writingSyncContext, input))
            {
                Name = "WriterThread"
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
                        _log.Debug($"Compressing thread {Thread.CurrentThread.Name} exists.");
                        return;
                    }
                    Exception exception = writeSyncContext.Exception;

                    if (exception != null)
                    {
                        processingSyncContext.SetError(exception);
                        processingSyncContext.SetFinished();
                        return;
                    }

                    if (!dequeued)
                    {
                        processingSyncContext.SyncEvents[threadIdx].WaitOne();
                        continue;
                    }

                    throw new Exception("SOME ERROR");

                    bool enqueued = false;
                    DataChunk processedChunk = dataProcessor.ProcessData(chunk);
                    while (!enqueued)
                    {
                        enqueued = writeSyncContext.Queue.Enqueue(processedChunk);
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

        private void DoWriting(WritingSyncContext syncContext, ReadProcessWriteInput input)
        {
            try
            {
                using (var dataWriter = CreateDataWriter(input))
                {
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
                    thread.Abort();
                }
            }
        }
    }
}
