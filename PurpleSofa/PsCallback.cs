namespace PurpleSofa;

/// <summary>
///     Callback.
/// </summary>
public abstract class PsCallback
{
    /// <summary>
    ///     Async open handler.
    /// </summary>
    /// <param name="session">session</param>
    /// <returns>task</returns>
    public virtual Task OnOpenAsync(PsSession session)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Async message handler.
    /// </summary>
    /// <param name="session">session</param>
    /// <param name="message">message</param>
    /// <returns>task</returns>
    public abstract Task OnMessageAsync(PsSession session, byte[] message);

    /// <summary>
    ///     Async close handler.
    /// </summary>
    /// <param name="session">session</param>
    /// <param name="closeReason">close reason</param>
    /// <returns>task</returns>
    public virtual Task OnCloseAsync(PsSession session, PsCloseReason closeReason)
    {
        return Task.CompletedTask;
    }
}