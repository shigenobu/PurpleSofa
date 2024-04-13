using System.Net.Sockets;

namespace PurpleSofa;

/// <summary>
///     Handler read.
/// </summary>
internal class PsHandlerRead : PsHandler<PsStateRead>
{
    /// <summary>
    ///     Invalid read size.
    /// </summary>
    internal const int InvalidRead = 0;

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
    ///     Constructor.
    /// </summary>
    /// <param name="callback">callback</param>
    /// <param name="readBufferSize">read buffer size</param>
    /// <param name="sessionManager">session manager</param>
    internal PsHandlerRead(PsCallback callback, int readBufferSize, PsSessionManager sessionManager)
    {
        _callback = callback;
        _readBufferSize = readBufferSize;
        _sessionManager = sessionManager;
        _sessionManager.StartCloseTask(Completed);
    }

    /// <summary>
    ///     Prepare.
    /// </summary>
    /// <param name="state">state</param>
    internal override void Prepare(PsStateRead state)
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
    internal override void Complete(IAsyncResult result)
    {
        // get state
        if (!GetState(result, out var state))
        {
            PsLogger.Debug(() => $"When read, no state result:{result}");
            return;
        }

        try
        {
            // read
            // TODO in short terms many message is received, message is concat with previous message
            // TODO 短時間に猛烈にメッセージを投げると、メッセージがくっついて受信されてしまうケースがある
            var read = state!.Socket.EndReceive(result);
            Completed(read, state);
        }
        catch (Exception e)
        {
            PsLogger.Debug(() => e);
            if (e is ObjectDisposedException or SocketException {SocketErrorCode: SocketError.ConnectionReset})
                state!.CloseReason = PsCloseReason.PeerClose;
            Failed(state!);
        }
    }

    /// <summary>
    ///     Failed.
    /// </summary>
    /// <param name="state">state</param>
    internal override void Failed(PsStateRead state)
    {
        PsLogger.Debug(() => $"Read failed:{state}");

        // force close
        Task.Run(async () =>
        {
            if (state.CloseReason == PsCloseReason.None) state.CloseReason = PsCloseReason.Failed;
            var session = await _sessionManager.ByAsync(state.Socket);
            PsLogger.Debug(() => $"Close session at failed:{session}");
            if (session == null) return;

            using (await session.Lock.LockAsync())
            {
                // close socket
                state.Socket.Close();

                // if never close handler called, true
                if (!session.CloseHandlerCalled)
                {
                    session.CloseHandlerCalled = true;
                    if (_callback.CallbackMode == PsCallbackMode.Sync)
                        // ReSharper disable once MethodHasAsyncOverload
                        _callback.OnClose(session, state.CloseReason);
                    else
                        await _callback.OnCloseAsync(session, state.CloseReason);
                }
            }
        });
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
            Task.Run(async () =>
            {
                if (state.CloseReason == PsCloseReason.None) state.CloseReason = PsCloseReason.PeerClose;
                session = await _sessionManager.ByAsync(state.Socket);
                PsLogger.Debug(() => $"Close session:{session}");
                if (session == null) return;

                using (await session.Lock.LockAsync())
                {
                    // close socket
                    state.Socket.Close();

                    // if never close handler called, true
                    if (!session.CloseHandlerCalled)
                    {
                        session.CloseHandlerCalled = true;
                        if (_callback.CallbackMode == PsCallbackMode.Sync)
                            // ReSharper disable once MethodHasAsyncOverload
                            _callback.OnClose(session, state.CloseReason);
                        else
                            await _callback.OnCloseAsync(session, state.CloseReason);
                    }
                }
            });
            return;
        }

        // callback
        Task.Run(async () =>
        {
            session = await _sessionManager.GetAsync(state.Socket);
            PsLogger.Debug(() => $"Read session:{session}, size:{read}");
            if (session == null) return;

            using (await session.Lock.LockAsync())
            {
                // if called close by self is false and timeout is false, true
                if (!session.SelfClosed && !session.IsTimeout())
                {
                    var message = new byte[read];
                    Buffer.BlockCopy(state.Buffer!, 0, message, 0, message.Length);
                    session.UpdateTimeout();
                    if (_callback.CallbackMode == PsCallbackMode.Sync)
                        // ReSharper disable once MethodHasAsyncOverload
                        _callback.OnMessage(session, message);
                    else
                        await _callback.OnMessageAsync(session, message);
                }
            }

            // next read
            state.Buffer = new byte[_readBufferSize];
            Prepare(state);
        });
    }

    /// <summary>
    ///     Shutdown.
    /// </summary>
    internal override void Shutdown()
    {
        // shutdown close
        _sessionManager.ShutdownCloseTask();
    }
}