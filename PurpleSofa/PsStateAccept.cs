namespace PurpleSofa;

/// <summary>
///     State accept.
/// </summary>
internal class PsStateAccept : PsState
{
    /// <summary>
    ///     To String.
    /// </summary>
    /// <returns>socket remote endpoint</returns>
    public override string ToString()
    {
        return $"Socket: {Socket.PxSocketRemoteEndPoint()}";
    }
}