using GZipTest.Chunks;
using GZipTest.Processing.Process;
using GZipTest.Processing.Write;
using GZipTest.Queue;
using System;
using System.IO;
using System.Threading;

namespace GZipTest.Processing
{
    abstract class AbstractReadProcessWrite : IReadProcessWrite
    {
        protected static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(AbstractReadProcessWrite));

        public void ReadProcessWrite(ReadProcessWriteInput input)
        {
            CheckPreconditions(input);
            try
            {
                CreateOutputFile(input);

                int processingThreads = Environment.ProcessorCount;

                ProcessingSyncContext[] processingSyncContexts = new ProcessingSyncContext[processingThreads];
                FixedSizeQueue<DataChunk> processingQueue = new FixedSizeQueue<DataChunk>((uint)(2 * processingThreads));
                object processingSyncRoot = new object();
                AutoResetEvent processingSyncHandle = new AutoResetEvent(false);
                AutoResetEvent[] processingFinishedEvents = new AutoResetEvent[processingThreads];
                ProcessingSyncContext.FinishedFlag processingContextFinishedFlag = new ProcessingSyncContext.FinishedFlag();
                for(int i = 0; i < processingThreads; i ++)
                {
                    processingFinishedEvents[i] = new AutoResetEvent(false);
                    processingSyncContexts[i] = new ProcessingSyncContext(processingQueue, processingSyncHandle, processingSyncRoot, processingFinishedEvents[i], processingContextFinishedFlag);
                }
                

                ProcessingSyncContext writingSyncContext = new ProcessingSyncContext(new ThreadSafeQueue<DataChunk>());

                StartProcessingThreads(processingSyncContexts, writingSyncContext, processingThreads);
                StartWritingThread(writingSyncContext, input);
                ReadData(input, processingSyncContexts[0], processingFinishedEvents, writingSyncContext);
            }
            catch(Exception exception)
            {
                _log.Error(exception);
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

        private void CreateOutputFile(ReadProcessWriteInput input)
        {
            using (var file = File.Create(input.OutputFileName))
            {
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

        protected abstract IDataReader CreateDataReader(ReadProcessWriteInput input);

        protected abstract IDataWriter CreateDataWriter(ReadProcessWriteInput input);

        protected abstract IDataProcessor CreateDataProcessor();

        private void ReadData(ReadProcessWriteInput input, ProcessingSyncContext processingSyncContest, AutoResetEvent[] processingDoneEvents, ProcessingSyncContext writingSyncContext)
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
                            }
                        }
                        processingSyncContest.SyncHandle.Set();
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
                processingSyncContest.SyncHandle.Set();

                WaitHandle.WaitAll(processingDoneEvents);

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

        private void StartProcessingThreads(ProcessingSyncContext[] processingSyncContexts, ProcessingSyncContext writeSyncContext, int processingThreads)
        {
            IDataProcessor dataProcessor = CreateDataProcessor();
            for(int i = 0; i < processingThreads; i++)
            {
                ProcessingSyncContext context = processingSyncContexts[i];
                string threadName = "ProcessingThread-" + i;
                new Thread(() => DoProcessing(context, writeSyncContext, dataProcessor))
                {
                    Name = threadName
                }.Start();
            }
        }

        private void StartWritingThread(ProcessingSyncContext writingSyncContext, ReadProcessWriteInput input)
        {
            new Thread(() => DoWriting(writingSyncContext, input)) 
            { 
                Name = "WriterThread" 
            }.Start();
        }

        private void DoProcessing(ProcessingSyncContext processingSyncContext, ProcessingSyncContext writeSyncContext, IDataProcessor dataProcessor)
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

                    if (!dequeued)
                    {
                        processingSyncContext.SyncHandle.WaitOne(100);
                        continue;
                    }
              
                    writeSyncContext.Queue.Enqueue(dataProcessor.ProcessData(chunk));
                    writeSyncContext.SyncHandle.Set();
                }
            }
            catch (Exception exception)
            {
                _log.Error($"Error during compressing: {exception}");
                processingSyncContext.SetError(new ProcessingException($"Exception during compression in thread Thread : {Thread.CurrentThread.Name}", exception));
            }
            finally
            {
                _log.Debug($"Compressing thread : {Thread.CurrentThread.Name} : done.");
                processingSyncContext.DoneEvent.Set();
            }
        }

        private void DoWriting(ProcessingSyncContext syncContext, ReadProcessWriteInput input)
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
                            syncContext.SyncHandle.WaitOne(100);
                            continue;
                        }
                        dataWriter.WriteChunk(chunk);
                    }
                }
            }
            catch (Exception exception)
            {
                _log.Error($"Error during writing: {exception}");
                syncContext.SetError(new ProcessingException($"Error writing compressed data to output file : {exception.Message}", exception));
            }
            finally
            {
                _log.Debug($"Writer thread done.");
                syncContext.DoneEvent.Set();
            }
        }
    }
}
