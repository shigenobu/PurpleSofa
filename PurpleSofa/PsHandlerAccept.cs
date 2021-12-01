using System;
using System.Data;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace PurpleSofa
{
    public class PsHandlerAccept : PsHandler<PsStateAccept>
    {
        private readonly ManualResetEventSlim _accepted = new(false);

        private readonly PsCallback _callback;

        private readonly int _readBufferSize;

        private readonly PsSessionManager _sessionManager;

        private readonly PsHandlerRead _handlerRead;

        public PsHandlerAccept(PsCallback callback, int readBufferSize, PsSessionManager sessionManager)
        {
            _callback = callback;
            _readBufferSize = readBufferSize;
            _sessionManager = sessionManager;
            _handlerRead = new PsHandlerRead(_callback, _readBufferSize, _sessionManager);
        }

        public override void Prepare(PsStateAccept state)
        {
            while (true)
            {
                // signal off
                _accepted.Reset();

                try
                {
                    // accept
                    state.Socket.BeginAccept(Complete, state);
                }
                catch (Exception e)
                {
                    PsLogger.Error(e);
                    Failed(state);
                }
        
                // wait until signal on
                _accepted.Wait();
            }
        }

        public override void Complete(IAsyncResult result)
        {
            // signal on
            _accepted.Set();
            
            // get state
            if (!GetState(result, out var state))
            {
                PsLogger.Error($"When accepted, no state result: {result}");
                return;
            }

            try
            {
                // accept
                Socket clientSocket = state!.Socket.EndAccept(result);

                // callback
                var session = _sessionManager.Generate(clientSocket);
                PsLogger.Debug(() => $"Accepted session: {session}");
                lock (session)
                {
                    session.UpdateTimeout();
                    _callback.OnOpen(session);
                }
            
                // read
                PsStateRead stateRead = new PsStateRead()
                {
                    Socket = clientSocket,
                    Buffer = new byte[_readBufferSize]
                };
                _handlerRead.Prepare(stateRead);
            }
            catch (Exception e)
            {
                PsLogger.Error(e);
                Failed(state!);
            }
        }

        public override void Failed(PsStateAccept state)
        {
            PsLogger.Debug(() => $"Accept failed: {state}");
        }

        public override void Shutdown()
        {
            _handlerRead.Shutdown();
        }
    }
}