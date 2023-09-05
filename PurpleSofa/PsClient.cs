using System;
using System.Net;
using System.Net.Sockets;

namespace PurpleSofa;

/// <summary>
///     Client.
/// </summary>
public class PsClient
{
    /// <summary>
    ///     Callback.
    /// </summary>
    private readonly PsCallback _callback;

    /// <summary>
    ///     Host.
    /// </summary>
    private readonly string _host;

    /// <summary>
    ///     Port.
    /// </summary>
    private readonly int _port;

    /// <summary>
    ///     Address family.
    /// </summary>
    private readonly PsSocketAddressFamily _socketAddressFamily;

    /// <summary>
    ///     Server socket.
    /// </summary>
    private Socket? _clientSocket;

    /// <summary>
    ///     Handler connect.
    /// </summary>
    private PsHandlerConnect? _handlerConnect;

    /// <summary>
    ///     Session manager.
    /// </summary>
    private PsSessionManager? _sessionManager;

    /// <summary>
    ///     Constructor for ipv4.
    /// </summary>
    /// <param name="callback">callback</param>
    /// <param name="host">host</param>
    /// <param name="port">port</param>
    public PsClient(PsCallback callback, string host, int port) : this(callback, PsSocketAddressFamily.Ipv4, host, port)
    {
    }

    /// <summary>
    ///     Constructor for ipv4 or ipv6.
    /// </summary>
    /// <param name="callback">callback</param>
    /// <param name="socketAddressFamily">address family</param>
    /// <param name="host">host</param>
    /// <param name="port">port</param>
    public PsClient(PsCallback callback, PsSocketAddressFamily socketAddressFamily, string host, int port)
    {
        _callback = callback;
        _socketAddressFamily = socketAddressFamily;
        _host = host;
        _port = port;
    }

    /// <summary>
    ///     ReadBufferSize for read(receive).
    /// </summary>
    public int ReadBufferSize { get; init; } = 2048;

    /// <summary>
    ///     Connect.
    /// </summary>
    /// <exception cref="PsClientException">client error</exception>
    public void Connect()
    {
        try
        {
            // init
            _clientSocket = new Socket(PsSocketAddressFamilyResolver.Resolve(_socketAddressFamily), SocketType.Stream,
                ProtocolType.Tcp);
            if (_socketAddressFamily == PsSocketAddressFamily.Ipv6)
            {
                _clientSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                PsLogger.Info("Ipv4 socket is treated as ipv6 socket");
            }

            // manager
            _sessionManager = new PsSessionManager(1);
            _sessionManager.StartTimeoutTask();

            // start client
            _handlerConnect = new PsHandlerConnect(_callback, ReadBufferSize, _sessionManager);
            _handlerConnect.Prepare(new PsStateConnect
            {
                Socket = _clientSocket,
                RemoteEndPoint = new IPEndPoint(IPAddress.Parse(_host), _port)
            });
            PsLogger.Info($"Client connect to {_host}:{_port} " +
                          $"(readBufferSize:{ReadBufferSize})");
        }
        catch (Exception e)
        {
            PsLogger.Error(e);
            throw new PsClientException(e);
        }
    }

    /// <summary>
    ///     Disconnect.
    /// </summary>
    public void Disconnect()
    {
        // close
        _clientSocket?.Close();

        // shutdown timeout
        _sessionManager?.ShutdownTimeoutTask();

        // shutdown handler
        _handlerConnect?.Shutdown();
    }
}

/// <summary>
///     Client Exception.
/// </summary>
public class PsClientException : Exception
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="e">exception</param>
    internal PsClientException(Exception e) : base(e.ToString())
    {
    }
}