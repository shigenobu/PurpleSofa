using System;
using System.Net;
using System.Net.Sockets;

namespace PurpleSofa;

/// <summary>
///     Multi client.
/// </summary>
public class PsMultiClient
{
    /// <summary>
    ///     Callback.
    /// </summary>
    private readonly PsCallback _callback;

    /// <summary>
    ///     Handler connect.
    /// </summary>
    private PsHandlerConnect? _handlerConnect;

    /// <summary>
    ///     Session manager.
    /// </summary>
    private PsSessionManager? _sessionManager;

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="callback">callback</param>
    public PsMultiClient(PsCallback callback)
    {
        _callback = callback;
    }

    /// <summary>
    ///     ReadBufferSize for read(receive).
    /// </summary>
    public int ReadBufferSize { get; init; } = 2048;

    /// <summary>
    ///     Divide.
    ///     It's client connections divided number.
    /// </summary>
    public int Divide { get; init; } = 10;

    /// <summary>
    ///     Init bundle.
    /// </summary>
    public void InitBundle()
    {
        // manager
        _sessionManager = new PsSessionManager(Divide);
        _sessionManager.StartTimeoutTask();

        // handler connect
        _handlerConnect = new PsHandlerConnect(_callback, ReadBufferSize, _sessionManager);
    }

    /// <summary>
    ///     Destroy bundle.
    /// </summary>
    public void DestroyBundle()
    {
        // shutdown timeout
        _sessionManager?.ShutdownTimeoutTask();

        // shutdown handler
        _handlerConnect?.Shutdown();
    }

    /// <summary>
    ///     Connect for ipv4.
    /// </summary>
    /// <param name="host">host</param>
    /// <param name="port">port</param>
    /// <returns>multi client socket</returns>
    public PsMultiClientSocket Connect(string host, int port)
    {
        return Connect(PsSocketAddressFamily.Ipv4, host, port);
    }

    /// <summary>
    ///     Connect for ipv4 or ipv6.
    /// </summary>
    /// <param name="socketAddressFamily">address family</param>
    /// <param name="host">host</param>
    /// <param name="port">port</param>
    /// <returns>multi client socket</returns>
    public PsMultiClientSocket Connect(PsSocketAddressFamily socketAddressFamily, string host, int port)
    {
        try
        {
            // init
            var clientSocket = new Socket(PsSocketAddressFamilyResolver.Resolve(socketAddressFamily), SocketType.Stream,
                ProtocolType.Tcp);
            if (socketAddressFamily == PsSocketAddressFamily.Ipv6)
            {
                clientSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                PsLogger.Info("Ipv4 socket is treated as ipv6 socket");
            }

            // start client
            _handlerConnect!.Prepare(new PsStateConnect
            {
                Socket = clientSocket,
                RemoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port)
            });
            PsLogger.Info($"Multi client connect to {host}:{port} " +
                          $"(readBufferSize:{ReadBufferSize})");

            return new PsMultiClientSocket
            {
                Socket = clientSocket
            };
        }
        catch (Exception e)
        {
            PsLogger.Error(e);
            throw new PsMultiClientException(e);
        }
    }

    /// <summary>
    ///     Disconnect.
    /// </summary>
    /// <param name="multiClientSocket">multi client socket</param>
    public void Disconnect(PsMultiClientSocket multiClientSocket)
    {
        // close
        multiClientSocket.Socket.Close();
    }
}

/// <summary>
///     Multi client socket.
/// </summary>
public class PsMultiClientSocket
{
    /// <summary>
    ///     Socket.
    /// </summary>
    public Socket Socket { get; init; } = null!;
}

/// <summary>
///     Multi client Exception.
/// </summary>
public class PsMultiClientException : Exception
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="e">exception</param>
    internal PsMultiClientException(Exception e) : base(e.ToString())
    {
    }
}