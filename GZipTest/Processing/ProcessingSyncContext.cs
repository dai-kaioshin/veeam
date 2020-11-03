using GZipTest.Chunks;
using GZipTest.Queue;
using System;
using System.Diagnostics;
using System.Threading;

namespace GZipTest.Processing
{
    internal class SyncContextBase
    {
        public IQueue<DataChunk> Queue { get; protected set; }

        public object SyncRoot { get; protected set; }

        public AutoResetEvent[] SyncEvents { get; protected set; }


        private Exception _exception = null;


        private bool _finished = false;

        internal bool Finished
        {
            get
            {
                lock (SyncRoot)
                {
                    return _finished;
                }
            }
        }

        internal void SetFinished()
        {
            lock (SyncRoot)
            {
                _finished = true;
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
            lock (SyncRoot)
            {
                if (_exception == null)
                {
                    _exception = exception;
                }
            }
        }

        internal void SetAllSyncEvents()
        {
            for(int i = 0; i < SyncEvents.Length; i++)
            {
                SyncEvents[i].Set();
            }
        }

        internal void WaitAllSyncEvents()
        {
            WaitHandle.WaitAll(SyncEvents);
        }

        internal void WaitAnySyncEvent()
        {
            WaitHandle.WaitAny(SyncEvents);
        }
    }

    internal class WritingSyncContext : SyncContextBase
    {
        public AutoResetEvent DoneEvent { get; }

        public WritingSyncContext(int numThreads, int queueSize)
        {
            Debug.Assert(numThreads > 0, "numThreads must be > 0.");
            Queue = new FixedSizeQueue<DataChunk>(queueSize);
            SyncEvents = new AutoResetEvent[numThreads];
            for(int i  = 0; i < numThreads; i++)
            {
                SyncEvents[i] = new AutoResetEvent(false);
            }
            DoneEvent = new AutoResetEvent(false);
            SyncRoot = new object();
        }
    }

    internal class ProcessingSyncContext : SyncContextBase
    { 
        public AutoResetEvent[] DoneEvents { get; }

        internal class FinishedFlag
        {
            internal bool Finished
            {
                get; set;
            } = false;
        }

        internal ProcessingSyncContext(int numThreads, int queueSize)
        {
            Debug.Assert(numThreads > 0, "numThreads must be > 0.");
            Queue = new FixedSizeQueue<DataChunk>(queueSize);
            SyncEvents = new AutoResetEvent[numThreads];
            DoneEvents = new AutoResetEvent[numThreads];
            for(int i = 0; i < numThreads; i++)
            {
                SyncEvents[i] = new AutoResetEvent(false);
                DoneEvents[i] = new AutoResetEvent(false);
            }
            SyncRoot = new object();
        }

        internal void WaitAllDoneEvents()
        {
            WaitHandle.WaitAll(DoneEvents);
        }
    }
}
