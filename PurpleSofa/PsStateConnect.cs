using System.Net;

namespace PurpleSofa;

/// <summary>
///     State connect.
/// </summary>
internal class PsStateConnect : PsState
{
    /// <summary>
    ///     Remote end point.
    /// </summary>
    internal IPEndPoint RemoteEndpoint { get; set; } = null!;

    /// <summary>
    ///     To String.
    /// </summary>
    /// <returns>socket local endpoint</returns>
    public override string ToString()
    {
        return $"Socket connect - LocalEndPoint: {LocalEndPoint}, RemoteEndPoint: {RemoteEndPoint}";
    }
}