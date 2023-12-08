namespace PurpleSofa;

/// <summary>
///     Handler connect.
/// </summary>
internal class PsHandlerConnect : PsHandler<PsStateConnect>
{
    /// <summary>
    ///     Callback.
    /// </summary>
    private readonly PsCallback _callback;

    /// <summary>
    ///     Reset event for connect.
    /// </summary>
    private readonly ManualResetEventSlim _connected = new(false);

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
    ///     Constructor.
    /// </summary>
    /// <param name="callback">callback</param>
    /// <param name="readBufferSize">read buffer size</param>
    /// <param name="sessionManager">session manager</param>
    internal PsHandlerConnect(PsCallback callback, int readBufferSize,
        PsSessionManager sessionManager)
    {
        _callback = callback;
        _readBufferSize = readBufferSize;
        _sessionManager = sessionManager;
        _handlerRead = new PsHandlerRead(_callback, _readBufferSize, _sessionManager);
    }

    /// <summary>
    ///     Prepare.
    /// </summary>
    /// <param name="state">state</param>
    internal override void Prepare(PsStateConnect state)
    {
        // signal off
        _connected.Reset();

        try
        {
            // connect
            state.Socket.BeginConnect(state.RemoteEndPoint, Complete, state);
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
    internal override void Complete(IAsyncResult result)
    {
        // signal on
        _connected.Set();

        // get state
        if (!GetState(result, out var state))
        {
            PsLogger.Debug(() => $"When connected, no state result:{result}");
            return;
        }

        try
        {
            // connect
            state!.Socket.EndConnect(result);

            // callback
            var session = _sessionManager.Generate(state.Socket, state.ConnectionId);
            PsLogger.Debug(() => $"Connected session:{session}");
            lock (session)
            {
                session.UpdateTimeout();
                _callback.OnOpen(session);
            }

            // read
            var stateRead = new PsStateRead
            {
                ConnectionId = state.ConnectionId,
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
    internal override void Failed(PsStateConnect state)
    {
        PsLogger.Debug(() => $"Connect failed:{state}");
    }

    /// <summary>
    ///     Shutdown.
    /// </summary>
    internal override void Shutdown()
    {
        // shutdown read
        _handlerRead.Shutdown();
    }
}