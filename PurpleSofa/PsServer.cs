using System.Net;
using System.Net.Sockets;

namespace PurpleSofa;

/// <summary>
///     Server.
/// </summary>
public class PsServer
{
    /// <summary>
    ///     Default ip v4 host.
    /// </summary>
    private const string DefaultIpv4Host = "0.0.0.0";

    /// <summary>
    ///     Default ip v6 host.
    /// </summary>
    private const string DefaultIpv6Host = "::";

    /// <summary>
    ///     Callback.
    /// </summary>
    private readonly PsCallback _callback;

    /// <summary>
    ///     Handler accept.
    /// </summary>
    private PsHandlerAccept? _handlerAccept;

    /// <summary>
    ///     Server socket.
    /// </summary>
    private Socket? _serverSocket;

    /// <summary>
    ///     Session manager.
    /// </summary>
    private PsSessionManager? _sessionManager;

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="callback">callback</param>
    public PsServer(PsCallback callback)
    {
        _callback = callback;
    }

    /// <summary>
    ///     Address family, default ipv4.
    /// </summary>
    public PsSocketAddressFamily SocketAddressFamily { get; init; } = PsSocketAddressFamily.Ipv4;

    /// <summary>
    ///     Host.
    /// </summary>
    public string Host { get; set; } = DefaultIpv4Host;

    /// <summary>
    ///     Port.
    /// </summary>
    public int Port { get; init; } = 8710;

    /// <summary>
    ///     Backlog.
    /// </summary>
    public int Backlog { get; init; } = 1024;

    /// <summary>
    ///     ReadBufferSize for read(receive).
    /// </summary>
    public int ReadBufferSize { get; init; } = 4096;

    /// <summary>
    ///     ReceiveBufferSize for socket option.
    /// </summary>
    public int ReceiveBufferSize { get; init; } = 1024 * 1024 * 128;

    /// <summary>
    ///     Divide.
    ///     It's client connections divided number.
    /// </summary>
    public int Divide { get; init; } = 10;

    /// <summary>
    ///     Start.
    /// </summary>
    /// <exception cref="PsServerException">server error</exception>
    public void Start()
    {
        try
        {
            // check v6
            if (SocketAddressFamily == PsSocketAddressFamily.Ipv6 &&
                IPAddress.Parse(Host).AddressFamily == AddressFamily.InterNetwork &&
                Host.Equals(DefaultIpv4Host))
            {
                Host = DefaultIpv6Host;
                PsLogger.Info($"Change host from '{DefaultIpv4Host}' to '{DefaultIpv6Host}', so ipv6 is adapted");
            }

            // init
            _serverSocket = new Socket(PsSocketAddressFamilyResolver.Resolve(SocketAddressFamily), SocketType.Stream,
                ProtocolType.Tcp);
            _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            if (SocketAddressFamily == PsSocketAddressFamily.Ipv6)
            {
                _serverSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                PsLogger.Info("Ipv4 socket is treated as ipv6 socket");
            }

            if (ReceiveBufferSize > 0)
                _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer,
                    ReceiveBufferSize);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Parse(Host), Port));
            _serverSocket.Listen(Backlog);

            // manager
            _sessionManager = new PsSessionManager(Divide);
            _sessionManager.StartTimeoutTask();

            // start server
            _handlerAccept = new PsHandlerAccept(_callback, ReadBufferSize, _sessionManager);
            _handlerAccept.Prepare(new PsStateAccept
            {
                ConnectionId = Guid.Empty, // fake, for not used 
                Socket = _serverSocket
            });
            PsLogger.Info($"Server listening on {Host}:{Port} " +
                          $"(backlog:{Backlog}, readBufferSize:{ReadBufferSize}, receiveBufferSize:{ReceiveBufferSize})");
        }
        catch (Exception e)
        {
            PsLogger.Error(e);
            throw new PsServerException(e);
        }
    }

    /// <summary>
    ///     Wait for.
    /// </summary>
    public void WaitFor()
    {
        _handlerAccept?.TaskAccept?.Wait();
    }

    /// <summary>
    ///     Get session count.
    /// </summary>
    /// <returns>session count</returns>
    public long GetSessionCount()
    {
        return _sessionManager?.GetSessionCount() ?? 0;
    }

    /// <summary>
    ///     Shutdown.
    /// </summary>
    public void Shutdown()
    {
        // sleep 1 seconds
        Thread.Sleep(1000);

        // shutdown timeout
        _sessionManager?.ShutdownTimeoutTask();

        // shutdown handler
        _handlerAccept?.Shutdown();

        // close
        _serverSocket?.Close();
    }
}

/// <summary>
///     Server Exception.
/// </summary>
public class PsServerException : Exception
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="e">exception</param>
    internal PsServerException(Exception e) : base(e.ToString())
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="msg">msg</param>
    internal PsServerException(string msg) : base(msg)
    {
    }
}