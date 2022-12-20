namespace PurpleSofa;

/// <summary>
///     Close reason.
/// </summary>
public enum PsCloseReason
{
    /// <summary>
    ///     None.
    /// </summary>
    None = default,

    /// <summary>
    ///     Peer close.
    /// </summary>
    PeerClose,

    /// <summary>
    ///     Self close.
    /// </summary>
    SelfClose,

    /// <summary>
    ///     Failed.
    /// </summary>
    Failed,

    /// <summary>
    ///     Timeout.
    /// </summary>
    Timeout,

    /// <summary>
    ///     Shutdown.
    /// </summary>
    Shutdown
}