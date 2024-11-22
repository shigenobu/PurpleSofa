using System.Reflection;
using System.Runtime.CompilerServices;

namespace PurpleSofa;

/// <summary>
///     Callback.
/// </summary>
public abstract class PsCallback
{
    /// <summary>
    ///     Synchronous method names.
    /// </summary>
    internal static readonly List<string> SynchronousMethodNames = ["OnOpen", "OnMessage", "OnClose"];

    /// <summary>
    ///     Use async callback.
    /// </summary>
    public virtual bool UseAsyncCallback { get; init; }

    /// <summary>
    ///     Contains async.
    /// </summary>
    /// <param name="callback">callback</param>
    /// <returns>if contains, return true</returns>
    internal static bool ContainsAsync(PsCallback callback)
    {
        var attType = typeof(AsyncStateMachineAttribute);
        foreach (var methodInfo in callback.GetType().GetMethods())
        {
            if (!SynchronousMethodNames.Contains(methodInfo.Name)) continue;

            var attrib = methodInfo.GetCustomAttribute(attType);
            if (attrib != null) return true;
        }

        return false;
    }

    /// <summary>
    ///     Open handler.
    /// </summary>
    /// <param name="session">session</param>
    [Obsolete("Use async methods instead with 'UseAsyncCallback' setting to 'true'.")]
    public virtual void OnOpen(PsSession session)
    {
    }

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
    ///     Message handler.
    /// </summary>
    /// <param name="session">session</param>
    /// <param name="message">message</param>
    [Obsolete("Use async methods instead with 'UseAsyncCallback' setting to 'true'.")]
    public virtual void OnMessage(PsSession session, byte[] message)
    {
    }

    /// <summary>
    ///     Async message handler.
    /// </summary>
    /// <param name="session">session</param>
    /// <param name="message">message</param>
    /// <returns>task</returns>
    public virtual Task OnMessageAsync(PsSession session, byte[] message)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Close handler.
    /// </summary>
    /// <param name="session">session</param>
    /// <param name="closeReason">close reason</param>
    [Obsolete("Use async methods instead with 'UseAsyncCallback' setting to 'true'.")]
    public virtual void OnClose(PsSession session, PsCloseReason closeReason)
    {
    }

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