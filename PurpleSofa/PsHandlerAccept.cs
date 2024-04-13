namespace PurpleSofa;

/// <summary>
///     Handler accept.
/// </summary>
internal class PsHandlerAccept : PsHandler<PsStateAccept>
{
    /// <summary>
    ///     Reset event for accept.
    /// </summary>
    private readonly ManualResetEventSlim _accepted = new(false);

    /// <summary>
    ///     Callback.
    /// </summary>
    private readonly PsCallback _callback;

    /// <summary>
    ///     Handler read.
    /// </summary>
    private readonly PsHandlerRead _handlerRead;

    /// <summary>
    ///     Read buffer size.
    /// </summary>
    private readonly int _readBufferSize;

    /// <summary>
    ///     Session manager.
    /// </summary>
    private readonly PsSessionManager _sessionManager;

    /// <summary>
    ///     Cancellation token for accept task.
    /// </summary>
    private readonly CancellationTokenSource _tokenSourceAccept;

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="callback">callback</param>
    /// <param name="readBufferSize">read buffer size</param>
    /// <param name="sessionManager">session manager</param>
    internal PsHandlerAccept(PsCallback callback, int readBufferSize, PsSessionManager sessionManager)
    {
        _callback = callback;
        _readBufferSize = readBufferSize;
        _sessionManager = sessionManager;
        _handlerRead = new PsHandlerRead(_callback, _readBufferSize, _sessionManager);

        _tokenSourceAccept = new CancellationTokenSource();
    }

    /// <summary>
    ///     Accept task.
    /// </summary>
    public Task? TaskAccept { get; private set; }

    /// <summary>
    ///     Prepare.
    /// </summary>
    /// <param name="state">state</param>
    internal override void Prepare(PsStateAccept state)
    {
        TaskAccept = Task.Factory.StartNew(() =>
        {
            while (true)
            {
                // check cancel
                if (_tokenSourceAccept.Token.IsCancellationRequested)
                {
                    PsLogger.Info($"Cancel accept task:{_tokenSourceAccept.Token.GetHashCode()}");
                    return;
                }

                // signal off
                _accepted.Reset();

                try
                {
                    // accept
                    state.Socket.BeginAccept(Complete, state);
                }
                catch (Exception e)
                {
                    PsLogger.Debug(() => e);
                    Failed(state);
                }

                // wait until signal on
                _accepted.Wait();
            }
        }, _tokenSourceAccept.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    /// <summary>
    ///     Complete.
    /// </summary>
    /// <param name="result">async result</param>
    internal override void Complete(IAsyncResult result)
    {
        // signal on
        _accepted.Set();

        // get state
        if (!GetState(result, out var state))
        {
            PsLogger.Debug(() => $"When accepted, no state result:{result}");
            return;
        }

        try
        {
            // accept
            var clientSocket = state!.Socket.EndAccept(result);

            // callback
            var connectionId = Guid.NewGuid();
            Task.Run(async () =>
            {
                var session = await _sessionManager.GenerateAsync(clientSocket, connectionId);
                PsLogger.Debug(() => $"Accepted session:{session}");

                using (await session.Lock.LockAsync())
                {
                    session.UpdateTimeout();
                    if (_callback.CallbackMode == PsCallbackMode.Sync)
                        // ReSharper disable once MethodHasAsyncOverload
                        _callback.OnOpen(session);
                    else
                        await _callback.OnOpenAsync(session);
                }
            });

            // read
            var stateRead = new PsStateRead
            {
                ConnectionId = connectionId,
                Socket = clientSocket,
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
    internal override void Failed(PsStateAccept state)
    {
        PsLogger.Debug(() => $"Accept failed:{state}");
    }

    /// <summary>
    ///     Shutdown.
    /// </summary>
    internal override void Shutdown()
    {
        // shutdown accept
        if (TaskAccept is {IsCanceled: false}) _tokenSourceAccept.Cancel();

        // shutdown read
        _handlerRead.Shutdown();
    }
}