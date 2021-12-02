using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PurpleSofa
{
    /// <summary>
    ///     Session manager.
    /// </summary>
    public class PsSessionManager
    {
        /// <summary>
        ///     Divide.
        /// </summary>
        private readonly int _divide;
        
        /// <summary>
        ///     Session locks.
        /// </summary>
        private readonly List<object> _sessionLocks;

        /// <summary>
        ///     Sessions.
        /// </summary>
        private readonly List<ConcurrentDictionary<Socket, PsSession>> _sessions;

        /// <summary>
        ///     Session count.
        /// </summary>
        private long _sessionCount;

        /// <summary>
        ///     Close queue.
        /// </summary>
        public PsQueue<PsStateRead> CloseQueue { get; }

        /// <summary>
        ///     Cancellation token for timeout task.
        /// </summary>
        private CancellationTokenSource? _tokenSourceTimeout;
        
        /// <summary>
        ///     Timeout task.
        /// </summary>
        private Task? _taskTimeout;

        /// <summary>
        ///     Task no.
        /// </summary>
        private int _taskNo;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="divide">divide</param>
        internal PsSessionManager(int divide)
        {
            _divide = divide;
            
            _sessionLocks = new List<object>(divide);
            for (int i = 0; i < divide; i++)
            {
                _sessionLocks.Add(new object());
            }
            
            _sessions = new List<ConcurrentDictionary<Socket, PsSession>>(divide);
            for (int i = 0; i < divide; i++)
            {
                _sessions.Add(new ConcurrentDictionary<Socket, PsSession>());
            }

            CloseQueue = new PsQueue<PsStateRead>();
        }

        /// <summary>
        ///     Start timeout task.
        /// </summary>
        internal void StartTimeoutTask()
        {
            var delay = 1000 / _divide;
            _tokenSourceTimeout = new CancellationTokenSource();
            _taskTimeout = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    // check cancel
                    if (_tokenSourceTimeout.Token.IsCancellationRequested)
                    {
                        PsLogger.Info($"Cancel timeout task: {_tokenSourceTimeout.Token.GetHashCode()}");
                        return;
                    }
                
                    // delay
                    await Task.Delay(delay);
                
                    // increment task no
                    _taskNo++;
                    if (_taskNo >= _divide) _taskNo = 0;

                    // timeout
                    lock (_sessionLocks[_taskNo])
                    {
                        foreach (var pair in _sessions[_taskNo])
                        {
                            lock (pair.Value)
                            {
                                // if called close by self is false and timeout is true, true
                                if (!pair.Value.SelfClosed && pair.Value.IsTimeout())
                                {
                                    CloseQueue.Add(new PsStateRead
                                    {
                                        Socket = pair.Key,
                                        CloseReason = PsCloseReason.Timeout
                                    });
                                }
                            }
                        }
                    }
                }
            },  _tokenSourceTimeout.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        /// <summary>
        ///     Shutdown timeout task.
        /// </summary>
        internal void ShutdownTimeoutTask()
        {
            if (_taskTimeout == null) return;
            if (_tokenSourceTimeout == null) return;
            if (_taskTimeout.IsCanceled)
            {
                return;
            }
            
            // cancel
            _tokenSourceTimeout.Cancel();
            
            // shutdown all sessions
            PsLogger.Info($"Closing connections at shutdown");
            for (int i = 0; i < _divide; i++)
            {
                lock (_sessionLocks[i])
                {
                    foreach (var pair in _sessions[i])
                    {
                        lock (pair.Value)
                        {
                            // if called close by self is false and shutdown handler is not called, true
                            if (!pair.Value.SelfClosed && !pair.Value.ShutdownHandlerCalled)
                            {
                                pair.Value.ShutdownHandlerCalled = true;
                                CloseQueue.Add(new PsStateRead
                                {
                                    Socket = pair.Key,
                                    CloseReason = PsCloseReason.Shutdown
                                });
                            }
                        }
                    }
                }
            }
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
        ///     Try get session.
        /// </summary>
        /// <param name="clientSocket">socket</param>
        /// <param name="session">session</param>
        /// <returns>if get session, return true</returns>
        private bool TryGet(Socket clientSocket, out PsSession? session)
        {
            int mod = GetMod(clientSocket);
            return _sessions[mod].TryGetValue(clientSocket, out session);
        }
        
        /// <summary>
        ///     Generate session.
        /// </summary>
        /// <param name="clientSocket">socket</param>
        /// <returns>session</returns>
        internal PsSession Generate(Socket clientSocket)
        {
            int mod = GetMod(clientSocket);
            if (_sessions[mod].ContainsKey(clientSocket))
            {
                return _sessions[mod][clientSocket];
            }

            PsSession? session;
            lock (_sessionLocks[mod])
            {
                if (!TryGet(clientSocket, out session))
                {
                    var tmpSession = new PsSession(clientSocket);
                    session = _sessions[mod].GetOrAdd(clientSocket, tmpSession);
                    session.CloseQueue = CloseQueue;
                    if (tmpSession == session)
                    {
                        Interlocked.Increment(ref _sessionCount);
                        PsLogger.Debug(() => $"Generate session: {session}");
                    }
                }
            }

            return session!;
        }

        /// <summary>
        ///     Get session.
        /// </summary>
        /// <param name="clientSocket">socket</param>
        /// <returns>session or null</returns>
        internal PsSession? Get(Socket clientSocket)
        {
            int mod = GetMod(clientSocket);
            lock (_sessionLocks[mod])
            {
                if (!_sessions[mod].ContainsKey(clientSocket)) return null;
                return _sessions[mod][clientSocket];
            }
        }

        /// <summary>
        ///     Remove session.
        /// </summary>
        /// <param name="clientSocket">socket</param>
        /// <returns>removed session or null</returns>
        internal PsSession? By(Socket clientSocket)
        {
            int mod = GetMod(clientSocket);
            PsSession? session;
            lock (_sessionLocks[mod])
            {
                if (_sessions[mod].Remove(clientSocket, out session))
                {
                    Interlocked.Decrement(ref _sessionCount);
                    PsLogger.Debug(() => $"By session: {session}");
                }
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
}