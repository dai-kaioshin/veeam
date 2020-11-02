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
            _maxSize = maxSize;
        }

        public bool Enqueue(T item, int timeout = 100)
        {
            bool waited = false;
            while (true)
            { 
                lock (_queue)
                {
                    if(_queue.Count < _maxSize)
                    {
                        _queue.Enqueue(item);
                        return true;
                    }
                    if(waited && timeout != Timeout.Infinite)
                    {
                        return false;
                    }
                }
                waited = true;
                bool signaled = _queueSizeChangedEvent.WaitOne(timeout);
                if (!signaled)
                {
                    return false;
                }
            }
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
                }
            }
            if(result)
            {
                _queueSizeChangedEvent.Set();
            }
            return result;
        }
    }
}
