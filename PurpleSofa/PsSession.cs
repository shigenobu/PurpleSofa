using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace PurpleSofa
{
    /// <summary>
    ///     Session.
    /// </summary>
    public class PsSession
    {
        /// <summary>
        ///     Default timeout milli seconds.
        /// </summary>
        private const int DefaultTimeoutMilliSeconds = 1500;

        /// <summary>
        ///     Life timestamp milli seconds.
        /// </summary>
        private long _lifeTimestampMilliseconds;

        /// <summary>
        ///     Session id.
        /// </summary>
        private readonly string _sid;

        /// <summary>
        ///     Socket.
        /// </summary>
        private readonly Socket _clientSocket;

        /// <summary>
        ///     Local endpoint.
        /// </summary>
        public EndPoint? LocalEndPoint { get; }
        
        /// <summary>
        ///     Remote endpoint.
        /// </summary>
        public EndPoint? RemoteEndPoint { get; }

        /// <summary>
        ///     Idle milli seconds.
        /// </summary>
        private int _idleMilliSeconds = 60000;
        
        /// <summary>
        ///     Close queue.
        /// </summary>
        internal PsQueue<PsStateRead>? CloseQueue { get; set; }
        
        /// <summary>
        ///     Close handler called.
        /// </summary>
        internal bool CloseHandlerCalled { get; set; }
        
        /// <summary>
        ///     Shutdown handler called.
        /// </summary>
        internal bool ShutdownHandlerCalled { get; set; }
        
        /// <summary>
        ///     Self closed.
        /// </summary>
        internal bool SelfClosed { get; set; }

        /// <summary>
        ///     Newest.
        /// </summary>
        private bool _newest = true;

        /// <summary>
        ///     Session values.
        /// </summary>
        private Dictionary<string, object>? _values;
        
        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="clientSocket">socket</param>
        public PsSession(Socket clientSocket)
        {
            _sid = PsUtils.RandomString(16);
            _clientSocket = clientSocket;
            LocalEndPoint = _clientSocket.PxSocketLocalEndPoint();
            RemoteEndPoint = _clientSocket.PxSocketRemoteEndPoint();
        }

        /// <summary>
        ///     Change idle milli seconds.
        /// </summary>
        /// <param name="idleMilliSeconds">idle milli seconds</param>
        public void ChangeIdleMilliSeconds(int idleMilliSeconds)
        {
            _idleMilliSeconds = idleMilliSeconds;
            UpdateTimeout();
        }
        
        /// <summary>
        ///     Is timeout.
        /// </summary>
        /// <returns>if timeout, return true</returns>
        internal bool IsTimeout()
        {
            return !_newest && PsDate.NowTimestampMilliSeconds() > _lifeTimestampMilliseconds;
        }
        
        /// <summary>
        ///     Update timeout.
        /// </summary>
        internal void UpdateTimeout()
        {
            _newest = false;
            _lifeTimestampMilliseconds = PsDate.NowTimestampMilliSeconds() + _idleMilliSeconds;
        }
        
        /// <summary>
        ///     Is open.
        /// </summary>
        /// <returns>if several flags are false all, return true</returns>
        private bool IsOpen()
        {
            return !CloseHandlerCalled
                && !ShutdownHandlerCalled
                && !SelfClosed;
        }

        /// <summary>
        ///     Send.
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="timeout">timeout</param>
        /// <exception cref="PsSendException">send error</exception>
        public void Send(byte[] message, int timeout = DefaultTimeoutMilliSeconds)
        {
            if (!IsOpen()) return;
            try
            {
                lock (this)
                {
                    _clientSocket.SendTimeout = timeout;
                    _clientSocket.Send(new ArraySegment<byte>(message), SocketFlags.None);    
                }
            }
            catch (Exception e)
            {
                PsLogger.Debug(() => e);
                
                // force close
                Close();
                throw new PsSendException(e);
            }
        }

        /// <summary>
        ///     Close.
        /// </summary>
        public void Close()
        {
            if (!IsOpen()) return;
            
            // self closed is set to true
            SelfClosed = true;
            
            // direct into queue
            CloseQueue?.Add(new PsStateRead()
            {
                Socket = _clientSocket,
                CloseReason = PsCloseReason.SelfClose
            });
        }
        
        /// <summary>
        ///     Set value.
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="value">value</param>
        public void SetValue(string name, object value)
        {
            _values ??= new Dictionary<string, object>();
            _values[name] = value;
        }

        /// <summary>
        ///     Get value.
        /// </summary>
        /// <param name="name">name</param>
        /// <typeparam name="T">type</typeparam>
        /// <returns>value or null</returns>
        public T? GetValue<T>(string name)
        {
            if (_values == null) return default;
            if (!_values.ContainsKey(name)) return default;
            return (T?)_values[name];
        }

        /// <summary>
        ///     Clear value.
        /// </summary>
        /// <param name="name">name</param>
        public void ClearValue(string name)
        {
            _values?.Remove(name);
        }

        /// <summary>
        ///     To string.
        /// </summary>
        /// <returns>session id</returns>
        public override string ToString()
        {
            return _sid;
        }
    }

    /// <summary>
    ///     Send exception.
    /// </summary>
    public class PsSendException : Exception
    {
        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="e">exception</param>
        internal PsSendException(Exception e) : base(e.ToString())
        {}
    }
}