using System.Collections.Concurrent;

namespace PurpleSofa;

/// <summary>
///     Queue.
/// </summary>
/// <typeparam name="T">type</typeparam>
internal class PsQueue<T>
{
    /// <summary>
    ///     Queue.
    /// </summary>
    private readonly ConcurrentQueue<T> _queue = new();

    /// <summary>
    ///     Reset event for queue.
    /// </summary>
    private readonly ManualResetEventSlim _queued = new(false);

    /// <summary>
    ///     Add item to queue.
    /// </summary>
    /// <param name="item">item</param>
    internal void Add(T item)
    {
        // enqueue
        _queue.Enqueue(item);

        // signal on
        _queued.Set();
    }

    /// <summary>
    ///     Get item from queue.
    /// </summary>
    /// <returns>item</returns>
    internal T? Poll()
    {
        // signal off
        _queued.Reset();

        // dequeue
        if (_queue.TryDequeue(out var item)) return item;

        // wait until signal on
        _queued.Wait();

        return default;
    }
}