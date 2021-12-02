using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PurpleSofa
{
    /// <summary>
    ///     Handler read.
    /// </summary>
    public class PsHandlerRead : PsHandler<PsStateRead>
    {
        /// <summary>
        ///     Invalid read size.
        /// </summary>
        private const int InvalidRead = 0;
        
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
        ///     Cancellation token for close task.
        /// </summary>
        private readonly CancellationTokenSource _tokenSourceClose;
        
        /// <summary>
        ///     Close task.
        /// </summary>
        private readonly Task _taskClose;
        
        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="callback">callback</param>
        /// <param name="readBufferSize">read buffer size</param>
        /// <param name="sessionManager">session manager</param>
        public PsHandlerRead(PsCallback callback, int readBufferSize, PsSessionManager sessionManager)
        {
            _callback = callback;
            _readBufferSize = readBufferSize;
            _sessionManager = sessionManager;

            _tokenSourceClose = new CancellationTokenSource();
            _taskClose = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    // check cancel
                    if (_tokenSourceClose.Token.IsCancellationRequested)
                    {
                        PsLogger.Info($"Cancel close task: {_tokenSourceClose.Token.GetHashCode()}");
                        return;
                    }
                    
                    // read from queue
                    PsStateRead? stateRead;
                    if ((stateRead = sessionManager.CloseQueue.Poll()) != null)
                    {
                        new Task((state) =>
                        {
                            PsLogger.Debug(() => $"Close state: {state}");
                            Completed(InvalidRead, (PsStateRead)state!);    
                        }, stateRead).Start();
                    }
                }
            }, _tokenSourceClose.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }
        
        /// <summary>
        ///     Prepare.
        /// </summary>
        /// <param name="state">state</param>
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
                PsLogger.Debug(() => e);
                Failed(state);
            }
        }

        /// <summary>
        ///     Complete.
        /// </summary>
        /// <param name="result">async result.</param>
        public override void Complete(IAsyncResult result)
        {
            // get state
            if (!GetState(result, out var state))
            {
                PsLogger.Debug(() => $"When read, no state result: {result}");
                return;
            }

            try
            {
                // read
                // TODO in short terms many message is received, message is concat with previous message
                // TODO 短時間に猛烈にメッセージを投げると、メッセージがくっついて受信されてしまうケースがある
                int read = state!.Socket.EndReceive(result);
                Completed(read, state);
            }
            catch (Exception e)
            {
                PsLogger.Debug(() => e);
                if (e is ObjectDisposedException or SocketException { SocketErrorCode: SocketError.ConnectionReset }) 
                    state!.CloseReason = PsCloseReason.PeerClose;
                Failed(state!);
            }
        }

        /// <summary>
        ///     Failed.
        /// </summary>
        /// <param name="state">state</param>
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
                // close socket
                state.Socket.Close();
                
                // if never close handler called, true
                if (!session.CloseHandlerCalled)
                {
                    session.CloseHandlerCalled = true;
                    _callback.OnClose(session, state.CloseReason);
                }
            }
        }

        /// <summary>
        ///     Completed for 'Complete' method and close task.
        /// </summary>
        /// <param name="read">read size</param>
        /// <param name="state">state</param>
        private void Completed(int read, PsStateRead state)
        {
            // check size
            PsSession? session;
            if (read <= InvalidRead)
            {
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
                    // close socket
                    state.Socket.Close();
                    
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

        /// <summary>
        ///     Shutdown.
        /// </summary>
        public override void Shutdown()
        {
            // shutdown read
            if (!_taskClose.IsCanceled) _tokenSourceClose.Cancel();
        }
    }
}