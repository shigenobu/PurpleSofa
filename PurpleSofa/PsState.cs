using System.Net.Sockets;

namespace PurpleSofa;

/// <summary>
///     State.
/// </summary>
internal class PsState
{
    /// <summary>
    ///     Socket.
    /// </summary>
    internal Socket Socket { get; init; } = null!;
}