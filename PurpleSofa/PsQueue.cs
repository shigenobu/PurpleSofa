using System.Collections.Concurrent;

namespace PurpleSofa
{
    public class PsQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public void Add(T item)
        {
            _queue.Enqueue(item);
        }

        public T? Poll()
        {
            if (_queue.TryDequeue(out var item))
            {
                return item;
            }

            return default;
        }
    }
}