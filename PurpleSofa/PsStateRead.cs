namespace PurpleSofa;

/// <summary>
///     State read.
/// </summary>
internal class PsStateRead : PsState
{
    /// <summary>
    ///     Volatile close reason.
    /// </summary>
    private volatile PsCloseReason _closeReason;

    /// <summary>
    ///     Buffer by read.
    /// </summary>
    internal byte[]? Buffer { get; set; }

    /// <summary>
    ///     Close reason.
    /// </summary>
    internal PsCloseReason CloseReason
    {
        get => _closeReason;
        set => _closeReason = value;
    }

    /// <summary>
    ///     To String.
    /// </summary>
    /// <returns>socket remote endpoint, close reason</returns>
    public override string ToString()
    {
        return
            $"Socket read - LocalEndPoint:{LocalEndPoint}, RemoteEndPoint:{RemoteEndPoint}, CloseReason:{CloseReason}";
    }
}