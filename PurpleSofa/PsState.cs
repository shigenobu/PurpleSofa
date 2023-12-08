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
    private EndPoint? _localEndPoint;

    /// <summary>
    ///     Remote endpoint.
    /// </summary>
    private EndPoint? _remoteEndPoint;

    /// <summary>
    ///     Local endpoint.
    /// </summary>
    internal EndPoint LocalEndPoint
    {
        get
        {
            _localEndPoint ??= _socket.PxSocketLocalEndPoint();
            return _localEndPoint!;
        }
        init => _localEndPoint = value;
    }

    /// <summary>
    ///     Remote endpoint.
    /// </summary>
    internal EndPoint RemoteEndPoint
    {
        get
        {
            _remoteEndPoint ??= _socket.PxSocketLocalEndPoint();
            return _remoteEndPoint!;
        }
        init => _remoteEndPoint = value;
    }

    /// <summary>
    ///     Socket.
    /// </summary>
    internal Socket Socket
    {
        get => _socket;
        init
        {
            _socket = value;
            var initLocalEndPoint = _socket.PxSocketLocalEndPoint();
            if (initLocalEndPoint != null) _localEndPoint = initLocalEndPoint;
            var initRemoteEndPoint = _socket.PxSocketRemoteEndPoint();
            if (initRemoteEndPoint != null) _remoteEndPoint = initRemoteEndPoint;
        }
    }

    /// <summary>
    ///     Connection id.
    /// </summary>
    internal Guid ConnectionId { get; init; }
}