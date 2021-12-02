using System;
using System.Net;
using System.Net.Sockets;

namespace PurpleSofa
{
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
        ///     ReadBufferSize for read(receive).
        /// </summary>
        public int ReadBufferSize { get; init; } = 2048;
        
        /// <summary>
        ///     Server socket.
        /// </summary>
        private Socket? _clientSocket;

        /// <summary>
        ///     Session manager.
        /// </summary>
        private PsSessionManager? _sessionManager;
        
        /// <summary>
        ///     Handler connect.
        /// </summary>
        private PsHandlerConnect? _handlerConnect;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="callback">callback</param>
        /// <param name="host">host</param>
        /// <param name="port">port</param>
        public PsClient(PsCallback callback, string host, int port)
        {
            _callback = callback;
            _host = host;
            _port = port;
        }
        
        /// <summary>
        ///     Connect.
        /// </summary>
        /// <exception cref="PsClientException">client error</exception>
        public void Connect()
        {
            try
            {
                // init
                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // manager
                _sessionManager = new PsSessionManager(1);
                _sessionManager.StartTimeoutTask();

                // start client
                var remoteEndpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
                _handlerConnect = new PsHandlerConnect(remoteEndpoint, _callback, ReadBufferSize, _sessionManager);
                _handlerConnect.Prepare(new PsStateConnect
                {
                    Socket = _clientSocket
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
            // shutdown manager
            _sessionManager?.ShutdownTimeoutTask();
            
            // close
            _clientSocket?.Close();
            
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
        {}
    }
}