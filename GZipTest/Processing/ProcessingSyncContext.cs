using GZipTest.Chunks;
using GZipTest.Queue;
using System;
using System.Threading;

namespace GZipTest.Processing
{
    internal class ProcessingSyncContext
    {
        public IQueue<DataChunk> Queue { get; }

        public object SyncRoot { get; }

        public AutoResetEvent SyncHandle { get; }

        public AutoResetEvent DoneEvent { get; }


        private Exception _exception = null;

        internal class FinishedFlag
        {
            internal bool Finished
            {
                get; set;
            } = false;
        }

        private FinishedFlag _finished = new FinishedFlag();


        internal ProcessingSyncContext(IQueue<DataChunk> queue)
        {
            Queue = queue;
            SyncHandle = new AutoResetEvent(false);
            DoneEvent = new AutoResetEvent(false);
            SyncRoot = new object();
        }

        internal ProcessingSyncContext(IQueue<DataChunk> queue, AutoResetEvent syncHandle, object syncRoot, AutoResetEvent doneEvent, FinishedFlag finishedFlag)
        {
            Queue = queue;
            SyncHandle = syncHandle;
            SyncRoot = syncRoot;
            DoneEvent = doneEvent;
            _finished = finishedFlag;
        }

        internal bool Finished
        {
            get
            {
                lock(SyncRoot)
                {
                    return _finished.Finished;
                }
            }
        }

        internal void SetFinished()
        {
            lock(SyncRoot)
            {
                _finished.Finished = true;
            }
        }

        internal Exception Exception
        {
            get
            {
                lock (SyncRoot)
                {
                    return _exception;
                }
            }
        }

        internal void SetError(Exception exception)
        {
            lock(SyncRoot)
            {
                if(_exception == null)
                {
                    _exception = exception;
                }
            }
        }
    }
}
