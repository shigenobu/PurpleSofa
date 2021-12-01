using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PurpleSofa
{
    public class PsSessionManager
    {
        private int _devide;

        private readonly PsShutdown _shutdown;
        
        private readonly List<object> _sessionLocks;

        private readonly List<ConcurrentDictionary<Socket, PsSession>> _sessions;

        private long _sessionCount;

        public PsQueue<PsStateRead> CloseQueue { get; }

        private CancellationTokenSource? _timeoutTokenSource;
        
        private Task? _timeoutTask;

        private int _taskNo;

        internal PsSessionManager(int devide, PsShutdown shutdown)
        {
            _devide = devide;
            _shutdown = shutdown;

            _sessionLocks = new List<object>(devide);
            for (int i = 0; i < devide; i++)
            {
                _sessionLocks.Add(new object());
            }
            
            _sessions = new List<ConcurrentDictionary<Socket, PsSession>>(devide);
            for (int i = 0; i < devide; i++)
            {
                _sessions.Add(new ConcurrentDictionary<Socket, PsSession>());
            }

            CloseQueue = new PsQueue<PsStateRead>();
        }

        internal void StartTimeoutTask()
        {
            int delay = 1000 / _devide;
            _timeoutTokenSource = new CancellationTokenSource();
            _timeoutTask = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    // check cancel
                    if (_timeoutTokenSource.Token.IsCancellationRequested)
                    {
                        PsLogger.Debug(() => $"Cancel timeout task: {_timeoutTokenSource}");
                        return;
                    }
                    
                    // if running shutdown, current connections are force to close
                    if (_shutdown.InShutdown())
                    {
                        PsLogger.Info($"In shutdown, manager count left sessions: {GetSessionCount()}");
                        for (int i = 0; i < _devide; i++)
                        {
                            
                        }
                        
                        return;
                    }
                
                    // delay
                    await Task.Delay(delay);
                
                    // increment task no
                    _taskNo++;
                    if (_taskNo >= _devide) _taskNo = 0;

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
            },  _timeoutTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        internal void ShutdownTimeoutTask()
        {
            if (_timeoutTask == null) return;
            if (_timeoutTokenSource == null) return;
            
            if (_timeoutTask.IsCanceled)
            {
                return;
            }
            
            // cancel
            _timeoutTokenSource.Cancel();
            
            // shutdown all sessions
            for (int i = 0; i < _devide; i++)
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
        
        private int GetMod(Socket s)
        {
            return Math.Abs(s.GetHashCode() % _devide);
        }

        private bool TryGet(Socket clientSocket, out PsSession? session)
        {
            int mod = GetMod(clientSocket);
            return _sessions[mod].TryGetValue(clientSocket, out session);
        }
        
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

        internal PsSession? Get(Socket clientSocket)
        {
            int mod = GetMod(clientSocket);
            lock (_sessionLocks[mod])
            {
                if (!_sessions[mod].ContainsKey(clientSocket)) return null;
                return _sessions[mod][clientSocket];
            }
        }

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

        public long GetSessionCount()
        {
            return Interlocked.Read(ref _sessionCount);
        }
    }
}