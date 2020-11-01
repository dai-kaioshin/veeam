using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace GZipTest.Queue
{
    public class FixedSizeQueue<T> : IQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();

        private readonly uint _maxSize;

        private readonly AutoResetEvent _queueSizeChangedEvent = new AutoResetEvent(false);

        public FixedSizeQueue(uint maxSize)
        {
            Debug.Assert(maxSize > 0, "maxSize has to be greater then 0");
            _maxSize = (uint)maxSize;
        }

        public bool Enqueue(T item, int timeout = 100)
        {
            bool enqueued = false;
            while (!enqueued)
            { 
                lock (_queue)
                {
                    if(_queue.Count >= _maxSize)
                    {
                        bool signaled = _queueSizeChangedEvent.WaitOne(timeout);
                        if (!signaled)
                        {
                            return false;
                        }
                    }
                    _queue.Enqueue(item);
                    enqueued = true;
                }
            }
            return enqueued;
        }

        public bool Dequeue(out T item)
        {
            item = default;
            bool result = false;
            lock (_queue)
            {
                if(_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    result = true;
                    _queueSizeChangedEvent.Set();
                }
            }
            return result;
        }
    }
}
