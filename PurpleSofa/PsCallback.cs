using System.Reflection;
using System.Runtime.CompilerServices;

namespace PurpleSofa;

/// <summary>
///     Callback.
/// </summary>
public abstract class PsCallback
{
    /// <summary>
    ///     Contains async.
    /// </summary>
    /// <param name="callback">callback</param>
    /// <returns>if contains, return true</returns>
    internal static bool ContainsAsync(PsCallback callback)
    {
        var methodNames = new List<string> {"OnOpen", "OnMessage", "OnClose"};
        var attType = typeof(AsyncStateMachineAttribute);
        foreach (var methodInfo in callback.GetType().GetMethods())
        {
            if (!methodNames.Contains(methodInfo.Name)) continue;

            var attrib = methodInfo.GetCustomAttribute(attType);
            if (attrib != null) return true;
        }

        return false;
    }

    /// <summary>
    ///     Open handler.
    /// </summary>
    /// <param name="session">session</param>
    public virtual void OnOpen(PsSession session)
    {
    }

    /// <summary>
    ///     Message handler.
    /// </summary>
    /// <param name="session">session</param>
    /// <param name="message">message</param>
    public abstract void OnMessage(PsSession session, byte[] message);

    /// <summary>
    ///     Close handler.
    /// </summary>
    /// <param name="session">session</param>
    /// <param name="closeReason">close reason</param>
    public virtual void OnClose(PsSession session, PsCloseReason closeReason)
    {
    }
}