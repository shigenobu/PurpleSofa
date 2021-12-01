using System;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace PurpleSofa
{
    public class PsHandlerRead : PsHandler<PsStateRead>
    {
        private const int InvalidRead = 0;
        
        private readonly PsCallback _callback;

        private readonly int _readBufferSize;

        private readonly PsSessionManager _sessionManager;

        private readonly CancellationTokenSource _closeTokenSource;
        
        private readonly Task _closeTask;
        
        public PsHandlerRead(PsCallback callback, int readBufferSize, PsSessionManager sessionManager)
        {
            _callback = callback;
            _readBufferSize = readBufferSize;
            _sessionManager = sessionManager;

            _closeTokenSource = new CancellationTokenSource();
            _closeTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    // check cancel
                    if (_closeTokenSource.Token.IsCancellationRequested)
                    {
                        PsLogger.Debug(() => $"Cancel close task: {_closeTokenSource}");
                        return;
                    }
                    
                    // read from queue
                    PsStateRead? stateRead;
                    if ((stateRead = sessionManager.CloseQueue.Poll()) != null)
                    {
                        new Task((state) =>
                        {
                            PsLogger.Debug(() => $"Close state: {state}");
                            Completed(InvalidRead, stateRead);    
                        }, stateRead).Start();
                    }
                }
            }, _closeTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        
        public override void Prepare(PsStateRead state)
        {
            try
            {
                state.Socket.BeginReceive(
                    state.Buffer!, 
                    0, 
                    state.Buffer!.Length, 
                    SocketFlags.None,
                    Complete, 
                    state);
            }
            catch (Exception e)
            {
                PsLogger.Error(e);
                Failed(state);
            }
        }

        public override void Complete(IAsyncResult result)
        {
            // get state
            if (!GetState(result, out var state))
            {
                PsLogger.Error($"When read, no state result: {result}");
                return;
            }

            try
            {
                // read
                int read = state!.Socket.EndReceive(result);
                Completed(read, state);
            }
            catch (Exception e)
            {
                PsLogger.Error(e);
                Failed(state!);
            }
        }

        public override void Failed(PsStateRead state)
        {
            PsLogger.Debug(() => $"Read failed: {state}");
            
            // force close
            if (state.CloseReason == PsCloseReason.None)
            {
                state.CloseReason = PsCloseReason.Failed;
            }
            var session = _sessionManager.By(state.Socket);
            PsLogger.Debug(() => $"Close session at failed: {session}");
            if (session == null) return;
            lock (session)
            {
                // if never close handler called, true
                if (!session.CloseHandlerCalled)
                {
                    session.CloseHandlerCalled = true;
                    _callback.OnClose(session, state.CloseReason);
                }
            }
        }

        private void Completed(int read, PsStateRead state)
        {
            // check size
            PsSession? session;
            if (read <= InvalidRead)
            {
                // close socket
                state.Socket.Close();

                // callback
                if (state.CloseReason == PsCloseReason.None)
                {
                    state.CloseReason = PsCloseReason.PeerClose;
                }
                session = _sessionManager.By(state.Socket);
                PsLogger.Debug(() => $"Close session: {session}");
                if (session == null) return;
                lock (session)
                {
                    // if never close handler called, true
                    if (!session.CloseHandlerCalled)
                    {
                        session.CloseHandlerCalled = true;
                        _callback.OnClose(session, state.CloseReason);
                    }
                }
                return;
            }
            
            // callback
            session = _sessionManager.Get(state.Socket);
            PsLogger.Debug(() => $"Read session: {session}");
            if (session == null) return;
            lock (session)
            {
                // if called close by self is false and timeout is false, true
                if (!session.SelfClosed && !session.IsTimeout())
                {
                    byte[] message = new byte[read];
                    Buffer.BlockCopy(state.Buffer!, 0, message, 0, message.Length);
                    session.UpdateTimeout();
                    _callback.OnMessage(session, message);    
                }
            }
            
            // next read
            state.Buffer = new byte[_readBufferSize];
            Prepare(state);
        }

        public override void Shutdown()
        {
            _closeTokenSource.Cancel();
            _closeTask.Dispose();
        }
    }
}