using System;
using System.Net;
using System.Net.Sockets;

namespace PurpleSofa
{
    /// <summary>
    ///     Server.
    /// </summary>
    public class PsServer
    {
        /// <summary>
        ///     Callback.
        /// </summary>
        private readonly PsCallback _callback;

        /// <summary>
        ///     Host.
        /// </summary>
        public string Host { get; init; } = "0.0.0.0";

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
        public int ReadBufferSize { get; init; } = 2048;

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
        ///     Server socket.
        /// </summary>
        private Socket? _serverSocket;

        /// <summary>
        ///     Session manager.
        /// </summary>
        private PsSessionManager? _sessionManager;
        
        /// <summary>
        ///     Handler accept.
        /// </summary>
        private PsHandlerAccept? _handlerAccept;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="callback">callback</param>
        public PsServer(PsCallback callback)
        {
            _callback = callback;
        }

        /// <summary>
        ///     Start
        /// </summary>
        /// <exception cref="PsServerException">server error</exception>
        public void Start()
        {
            try
            {
                // init
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                if (ReceiveBufferSize > 0)
                {
                    _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, ReceiveBufferSize);
                }
                _serverSocket.Bind(new IPEndPoint(IPAddress.Parse(Host), Port));
                _serverSocket.Listen(Backlog);
                
                // manager
                _sessionManager = new PsSessionManager(Divide);
                _sessionManager.StartTimeoutTask();

                // start server
                _handlerAccept = new PsHandlerAccept(_callback, ReadBufferSize, _sessionManager);
                _handlerAccept.Prepare(new PsStateAccept
                {
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
            // shutdown manager
            _sessionManager?.ShutdownTimeoutTask();
            
            // close
            _serverSocket?.Close();
            
            // shutdown handler
            _handlerAccept?.Shutdown();
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
        {}
    }
}