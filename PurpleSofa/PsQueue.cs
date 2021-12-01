using System.Collections.Concurrent;

namespace PurpleSofa
{
    /// <summary>
    ///     Queue.
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    public class PsQueue<T>
    {
        /// <summary>
        ///     Queue.
        /// </summary>
        private readonly ConcurrentQueue<T> _queue = new();

        /// <summary>
        ///     Add item to queue.
        /// </summary>
        /// <param name="item">item</param>
        public void Add(T item)
        {
            _queue.Enqueue(item);
        }

        /// <summary>
        ///     Get item from queue.
        /// </summary>
        /// <returns>item</returns>
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