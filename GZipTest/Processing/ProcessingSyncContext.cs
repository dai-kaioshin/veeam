using GZipTest.Chunks;
using GZipTest.Queue;
using System;
using System.Threading;

namespace GZipTest.Processing
{
    internal class ProcessingSyncContext<TSyncHandle> where TSyncHandle : WaitHandle
    {
        public IQueue<DataChunk> Queue { get; }

        public object SyncRoot { get; }

        public TSyncHandle SyncHandle { get; }

        public AutoResetEvent DoneEvent { get; }

        private bool _finishedFlag = false;

        private Exception _exception = null;



        internal ProcessingSyncContext(IQueue<DataChunk> queue, TSyncHandle syncHandle)
        {
            Queue = queue;
            SyncHandle = syncHandle;
            DoneEvent = new AutoResetEvent(false);
            SyncRoot = new object();

        }

        internal bool Finished
        {
            get
            {
                lock(SyncRoot)
                {
                    return _finishedFlag;
                }
            }
        }

        internal void SetFinished()
        {
            lock(SyncRoot)
            {
                _finishedFlag = true;
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
