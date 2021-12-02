using System;
using System.Net;
using System.Threading;

namespace PurpleSofa
{
    /// <summary>
    ///     Handler connect.
    /// </summary>
    public class PsHandlerConnect : PsHandler<PsStateConnect>
    {
        /// <summary>
        ///     Reset event for connect.
        /// </summary>
        private readonly ManualResetEventSlim _connected = new(false);
        
        /// <summary>
        ///     Remote endpoint.
        /// </summary>
        private readonly IPEndPoint _remoteEndpoint;
        
        /// <summary>
        ///     Callback.
        /// </summary>
        private readonly PsCallback _callback;

        /// <summary>
        ///     Read buffer size.
        /// </summary>
        private readonly int _readBufferSize;

        /// <summary>
        ///     Session manager.
        /// </summary>
        private readonly PsSessionManager _sessionManager;

        /// <summary>
        ///     Handler read.
        /// </summary>
        private readonly PsHandlerRead _handlerRead;
        
        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="remoteEndpoint">remote endpoint</param>
        /// <param name="callback">callback</param>
        /// <param name="readBufferSize">read buffer size</param>
        /// <param name="sessionManager">session manager</param>
        public PsHandlerConnect(IPEndPoint remoteEndpoint, PsCallback callback, int readBufferSize, PsSessionManager sessionManager)
        {
            _remoteEndpoint = remoteEndpoint;
            _callback = callback;
            _readBufferSize = readBufferSize;
            _sessionManager = sessionManager;
            _handlerRead = new PsHandlerRead(_callback, _readBufferSize, _sessionManager);
        }
        
        /// <summary>
        ///     Prepare.
        /// </summary>
        /// <param name="state">state</param>
        public override void Prepare(PsStateConnect state)
        {
            // signal off
            _connected.Reset();

            try
            {
                // connect
                state.Socket.BeginConnect(_remoteEndpoint, Complete, state);
            }
            catch (Exception e)
            {
                PsLogger.Debug(() => e);
                Failed(state);
            }
        
            // wait until signal on
            _connected.Wait();
        }

        /// <summary>
        ///     Complete.
        /// </summary>
        /// <param name="result">async result</param>
        public override void Complete(IAsyncResult result)
        {
            // signal on
            _connected.Set();
            
            // get state
            if (!GetState(result, out var state))
            {
                PsLogger.Debug(() => $"When connected, no state result: {result}");
                return;
            }

            try
            {
                // connect
                state!.Socket.EndConnect(result);

                // callback
                var session = _sessionManager.Generate(state.Socket);
                PsLogger.Debug(() => $"Connected session: {session}");
                lock (session)
                {
                    session.UpdateTimeout();
                    _callback.OnOpen(session);
                }
            
                // read
                PsStateRead stateRead = new PsStateRead()
                {
                    Socket = state.Socket,
                    Buffer = new byte[_readBufferSize]
                };
                _handlerRead.Prepare(stateRead);
            }
            catch (Exception e)
            {
                PsLogger.Debug(() => e);
                Failed(state!);
            }
        }

        /// <summary>
        ///     Failed.
        /// </summary>
        /// <param name="state">state</param>
        public override void Failed(PsStateConnect state)
        {
            PsLogger.Debug(() => $"Connect failed: {state}");
        }

        /// <summary>
        ///     Shutdown.
        /// </summary>
        public override void Shutdown()
        {
            // shutdown read
            _handlerRead.Shutdown();
        }
    }
}