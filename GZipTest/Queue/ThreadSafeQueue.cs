using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Queue
{
    class ThreadSafeQueue<T> : IQueue<T>
    {
        private Queue<T> _queue = new Queue<T>();

        public bool Enqueue(T item, int timeout = -1)
        {
            bool locked = false;
            try
            {
                Monitor.TryEnter(_queue, timeout, ref locked);
                if (locked)
                {
                    _queue.Enqueue(item);
                    return true;
                }
                return false;
            }
            finally
            {
                if(locked)
                {
                    Monitor.Exit(_queue);
                }
            }
        }

        public bool Dequeue(out T item)
        {
            item = default;
            bool result = false;
            lock(_queue)
            {
                if(_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    result = true;
                }
            }
            return result;
        }
    }
}
