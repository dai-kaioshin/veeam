namespace GZipTest.Queue
{
    interface IQueue<T>
    {
        public bool Enqueue(T item, int timeout = 100);

        public bool Dequeue(out T item);
    }
}
