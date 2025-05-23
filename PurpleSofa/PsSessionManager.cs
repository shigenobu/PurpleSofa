using System.Collections.Concurrent;
using System.Net.Sockets;

namespace PurpleSofa;

/// <summary>
///     Session manager.
/// </summary>
public class PsSessionManager
{
    /// <summary>
    ///     Close queue.
    /// </summary>
    private readonly PsQueue<PsStateRead> _closeQueue;

    /// <summary>
    ///     Divide.
    /// </summary>
    private readonly int _divide;

    /// <summary>
    ///     Session locks.
    /// </summary>
    private readonly List<PsLock> _sessionLocks;

    /// <summary>
    ///     Sessions.
    /// </summary>
    private readonly List<ConcurrentDictionary<Socket, PsSession>> _sessions;

    /// <summary>
    ///     Session count.
    /// </summary>
    private long _sessionCount;

    /// <summary>
    ///     Close task.
    /// </summary>
    private Task? _taskClose;

    /// <summary>
    ///     Timeout task.
    /// </summary>
    private Task? _taskTimeout;

    /// <summary>
    ///     Cancellation token for close task.
    /// </summary>
    private CancellationTokenSource? _tokenSourceClose;

    /// <summary>
    ///     Cancellation token for timeout task.
    /// </summary>
    private CancellationTokenSource? _tokenSourceTimeout;

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="divide">divide</param>
    internal PsSessionManager(int divide)
    {
        _divide = divide;

        _sessionLocks = new List<PsLock>(divide);
        for (var i = 0; i < divide; i++) _sessionLocks.Add(new PsLock());

        _sessions = new List<ConcurrentDictionary<Socket, PsSession>>(divide);
        for (var i = 0; i < divide; i++) _sessions.Add(new ConcurrentDictionary<Socket, PsSession>());

        _closeQueue = new PsQueue<PsStateRead>();
    }

    /// <summary>
    ///     Start timeout task.
    /// </summary>
    internal void StartTimeoutTask()
    {
        // run once
        if (_taskTimeout != null) return;

        var delay = 1000 / _divide;
        _tokenSourceTimeout = new CancellationTokenSource();
        _taskTimeout = Task.Factory.StartNew(async () =>
        {
            PsLogger.Info($"Start timeout task -> divide:{_divide}");

            var taskNo = 0;
            while (true)
            {
                // check cancel
                if (_tokenSourceTimeout.Token.IsCancellationRequested)
                {
                    PsLogger.Info($"Cancel timeout task:{_tokenSourceTimeout.Token.GetHashCode()}");
                    return;
                }

                // delay
                await Task.Delay(delay);

                // increment task no
                taskNo++;
                if (taskNo >= _divide) taskNo = 0;

                // timeout
                using (await _sessionLocks[taskNo].LockAsync())
                {
                    foreach (var (socket, session) in _sessions[taskNo])
                        using (await session.Lock.LockAsync())
                        {
                            // if called close by self is false and timeout is true, true
                            if (!session.SelfClosed && session.IsTimeout())
                                _closeQueue.Add(new PsStateRead
                                {
                                    Socket = socket,
                                    CloseReason = PsCloseReason.Timeout
                                });
                        }
                }
            }
        }, _tokenSourceTimeout.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    /// <summary>
    ///     Shutdown timeout task.
    /// </summary>
    internal void ShutdownTimeoutTask()
    {
        if (_taskTimeout == null) return;
        if (_tokenSourceTimeout == null) return;
        if (_taskTimeout.IsCanceled) return;

        // cancel
        _tokenSourceTimeout.Cancel();
        _taskTimeout = null;
        _tokenSourceTimeout = null;

        Task.Run(async () =>
        {
            // shutdown all sessions
            PsLogger.Info("Closing connections at shutdown");
            for (var i = 0; i < _divide; i++)
                using (await _sessionLocks[i].LockAsync())
                {
                    foreach (var (socket, session) in _sessions[i])
                        using (await session.Lock.LockAsync())
                        {
                            // if called close by self is false and shutdown handler is not called, true
                            if (session is {SelfClosed: false, ShutdownHandlerCalled: false})
                            {
                                session.ShutdownHandlerCalled = true;
                                _closeQueue.Add(new PsStateRead
                                {
                                    Socket = socket,
                                    CloseReason = PsCloseReason.Shutdown
                                });
                            }
                        }
                }

            PsLogger.Info($"Shutdown timeout task -> divide:{_divide}");
        });
    }

    /// <summary>
    ///     Start close task.
    /// </summary>
    /// <param name="completed">completed action</param>
    internal void StartCloseTask(Action<int, PsStateRead> completed)
    {
        // run once
        if (_taskClose != null) return;

        _tokenSourceClose = new CancellationTokenSource();
        _taskClose = Task.Factory.StartNew(() =>
        {
            PsLogger.Info("Start close task");

            while (true)
            {
                // check cancel
                if (_tokenSourceClose.Token.IsCancellationRequested)
                {
                    PsLogger.Info($"Cancel close task:{_tokenSourceClose.Token.GetHashCode()}");
                    return;
                }

                // read from queue
                PsStateRead? stateRead;
                // ReSharper disable once InconsistentlySynchronizedField
                if ((stateRead = _closeQueue.Poll()) != null)
                {
                    var t = Task.Run(() =>
                    {
                        PsLogger.Debug(() => $"Close state:{stateRead}");
                        completed(PsHandlerRead.InvalidRead, stateRead);
                    });
                    t.ContinueWith(comp =>
                    {
                        if (comp.Exception is { } e) PsLogger.Debug(() => e.InnerExceptions);
                    });
                }
            }
        }, _tokenSourceClose.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    /// <summary>
    ///     Shutdown close task.
    /// </summary>
    internal void ShutdownCloseTask()
    {
        if (_taskClose == null) return;
        if (_tokenSourceClose == null) return;
        if (_taskClose.IsCanceled) return;

        // cancel
        _tokenSourceClose.Cancel();
        _taskClose = null;
        _tokenSourceClose = null;

        PsLogger.Info("Shutdown close task");
    }

    /// <summary>
    ///     Get mod.
    /// </summary>
    /// <param name="s">socket</param>
    /// <returns>mod</returns>
    private int GetMod(Socket s)
    {
        return Math.Abs(s.GetHashCode() % _divide);
    }

    /// <summary>
    ///     Try to get session.
    /// </summary>
    /// <param name="clientSocket">socket</param>
    /// <param name="session">session</param>
    /// <returns>if to get session, return true</returns>
    private bool TryGet(Socket clientSocket, out PsSession? session)
    {
        var mod = GetMod(clientSocket);
        return _sessions[mod].TryGetValue(clientSocket, out session);
    }

    /// <summary>
    ///     Async generate session.
    /// </summary>
    /// <param name="clientSocket">socket</param>
    /// <param name="connectionId">connection id</param>
    /// <returns>session</returns>
    internal async Task<PsSession> GenerateAsync(Socket clientSocket, Guid connectionId)
    {
        var mod = GetMod(clientSocket);

        PsSession? session;
        using (await _sessionLocks[mod].LockAsync())
        {
            if (!TryGet(clientSocket, out session))
            {
                var tmpSession = new PsSession(clientSocket, connectionId);
                session = _sessions[mod].GetOrAdd(clientSocket, tmpSession);
                session.CloseQueue = _closeQueue;
                if (tmpSession == session)
                {
                    Interlocked.Increment(ref _sessionCount);
                    PsLogger.Debug(() => $"Generate session:{session}");
                }
            }
        }

        return session!;
    }

    /// <summary>
    ///     Async get session.
    /// </summary>
    /// <param name="clientSocket">socket</param>
    /// <returns>session or null</returns>
    internal async Task<PsSession?> GetAsync(Socket clientSocket)
    {
        var mod = GetMod(clientSocket);

        PsSession? session;
        using (await _sessionLocks[mod].LockAsync())
        {
            if (!TryGet(clientSocket, out session)) return null;
        }

        return session;
    }

    /// <summary>
    ///     Async remove session.
    /// </summary>
    /// <param name="clientSocket">socket</param>
    /// <returns>removed session or null</returns>
    internal async Task<PsSession?> ByAsync(Socket clientSocket)
    {
        var mod = GetMod(clientSocket);
        PsSession? session;
        using (await _sessionLocks[mod].LockAsync())
        {
            if (!_sessions[mod].TryRemove(clientSocket, out session)) return null;

            Interlocked.Decrement(ref _sessionCount);
            PsLogger.Debug(() => $"By session:{session}");
        }

        return session;
    }

    /// <summary>
    ///     Get session count.
    /// </summary>
    /// <returns>session count</returns>
    public long GetSessionCount()
    {
        return Interlocked.Read(ref _sessionCount);
    }
}