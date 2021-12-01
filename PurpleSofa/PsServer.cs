using System;
using System.Net;
using System.Net.Sockets;

namespace PurpleSofa
{
    public class PsServer
    {
        private readonly PsCallback _callback;

        public string Host { get; set; } = "0.0.0.0";

        public int Port { get; set; } = 8710;

        public int Backlog { get; set; } = 1024;

        public int ReadBufferSize { get; set; } = 2048;

        public int ReceiveBufferSize { get; set; } = 1024 * 1024 * 128;

        public int Devide { get; set; } = 10;

        public PsShutdownExecutor ShutdownExecutor { get; set; } = new PsShutdownExecutor();

        private Socket? _serverSocket;

        private PsSessionManager? _sessionManager;
        
        private PsHandlerAccept? _handlerAccept;
        

        public PsServer(PsCallback callback)
        {
            _callback = callback;
        }

        public void Start()
        {
            // set shutdown executor
            var shutdown = new PsShutdown(ShutdownExecutor);
            var domain = AppDomain.CurrentDomain;
            domain.ProcessExit += shutdown.Shutdown;    // TODO
            PsLogger.Info($"Register shutdown executor: {ShutdownExecutor.GetType().FullName}");
            
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
                PsLogger.Info($"Server listening on {Host}:{Port} " +
                              $"(backlog:{Backlog}, readBufferSize:{ReadBufferSize}, receiveBufferSize:{ReceiveBufferSize}");
                
                // manager
                _sessionManager = new PsSessionManager(Devide, shutdown);
                _sessionManager.StartTimeoutTask();
                
                // start server
                _handlerAccept = new PsHandlerAccept(_callback, ReadBufferSize, _sessionManager);
                _handlerAccept.Prepare(new PsStateAccept
                {
                    Socket = _serverSocket
                });
            }
            catch (Exception e)
            {
                PsLogger.Error(e);
                throw new PsServerException(e);
            }
        }

        public void Shutdown()
        {
            // shutdown manager
            _sessionManager?.ShutdownTimeoutTask();
            
            // close
            _serverSocket?.Close();
            
            // shutdown hander
            _handlerAccept?.Shutdown();
        }
    }

    public class PsServerException : Exception
    {
        public PsServerException(Exception e) : base(e.ToString())
        {}
    }
}