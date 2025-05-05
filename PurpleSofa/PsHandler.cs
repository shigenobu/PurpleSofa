namespace PurpleSofa;

/// <summary>
///     Handler.
/// </summary>
/// <typeparam name="T"></typeparam>
internal abstract class PsHandler<T> where T : PsState
{
    /// <summary>
    ///     Get state.
    /// </summary>
    /// <param name="result">async result</param>
    /// <param name="state">state</param>
    /// <returns>if cast is success, return true</returns>
    protected static bool GetState(IAsyncResult result, out T? state)
    {
        state = null;
        if (result.AsyncState != null) state = (T) result.AsyncState;
        return state != null;
    }

    // ReSharper disable once GrammarMistakeInComment
    /// <summary>
    ///     Prepare (Accept, Connect or Read)
    /// </summary>
    /// <param name="state">state</param>
    internal abstract void Prepare(T state);

    // ReSharper disable once GrammarMistakeInComment
    /// <summary>
    ///     Complete (Accept, Connect or Read)
    /// </summary>
    /// <param name="result"></param>
    internal abstract void Complete(IAsyncResult result);

    /// <summary>
    ///     Failed.
    /// </summary>
    /// <param name="state">state</param>
    internal abstract void Failed(T state);

    /// <summary>
    ///     Shutdown.
    /// </summary>
    internal abstract void Shutdown();
}