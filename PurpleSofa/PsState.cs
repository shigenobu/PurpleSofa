using System.Net;
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
    private readonly Socket _socket = null!;

    /// <summary>
    ///     Local endpoint.
    /// </summary>
    protected readonly EndPoint? LocalEndPoint;

    /// <summary>
    ///     Remote endpoint.
    /// </summary>
    protected readonly EndPoint? RemoteEndPoint;

    /// <summary>
    ///     Socket.
    /// </summary>
    internal Socket Socket
    {
        get => _socket;
        init
        {
            _socket = value;
            LocalEndPoint = _socket.PxSocketLocalEndPoint();
            RemoteEndPoint = _socket.PxSocketRemoteEndPoint();
        }
    }
}