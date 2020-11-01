using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Queue
{
    interface IQueue<T>
    {
        public bool Enqueue(T item, int timeout = 100);

        public bool Dequeue(out T item);
    }
}
