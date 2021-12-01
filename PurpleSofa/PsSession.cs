using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PurpleSofa
{
    public class PsSession
    {
        private const int DefaultTimeoutMilliSeconds = 1500;

        private long _lifeTimestampMilliseconds;

        private string _sid;

        private Socket _clientSocket;

        public EndPoint? LocalEndPoint { get; }
        
        public EndPoint? RemoteEndPoint { get; }

        public int IdleMilliSeconds { get; set; } = 60000;
        
        internal PsQueue<PsStateRead>? CloseQueue { get; set; }
        
        internal bool CloseHandlerCalled { get; set; } 
        internal bool ShutdownHandlerCalled { get; set; } 
        internal bool SelfClosed { get; set; }

        private bool _newest = true;

        private Dictionary<string, object>? _values;
        
        public PsSession(Socket clientSocket)
        {
            _sid = PsUtils.RandomString(16);
            _clientSocket = clientSocket;
            LocalEndPoint = _clientSocket.PxSocketLocalEndPoint();
            RemoteEndPoint = _clientSocket.PxSocketRemoteEndPoint();
        }

        internal bool IsTimeout()
        {
            return !_newest && PsDate.NowTimestampMilliSeconds() > _lifeTimestampMilliseconds;
        }
        
        internal void UpdateTimeout()
        {
            _newest = false;
            _lifeTimestampMilliseconds = PsDate.NowTimestampMilliSeconds() + IdleMilliSeconds;
        }
        
        public bool IsOpen()
        {
            return !CloseHandlerCalled
                && !ShutdownHandlerCalled
                && !SelfClosed;
        }

        public void Send(byte[] message, int timeout = DefaultTimeoutMilliSeconds)
        {
            try
            {
                _clientSocket.SendTimeout = timeout;
                _clientSocket.Send(new ArraySegment<byte>(message), SocketFlags.None);
            }
            catch (Exception e)
            {
                PsLogger.Error(e);
                
                // force close
                Close();
            }
        }

        public void Close()
        {
            if (IsOpen()) return;
            
            // self closed is set to true
            SelfClosed = true;
            
            // direct into queue
            CloseQueue?.Add(new PsStateRead()
            {
                Socket = _clientSocket,
                CloseReason = PsCloseReason.SelfClose
            });
        }
        
        public void SetValue(string name, object value)
        {
            _values ??= new Dictionary<string, object>();
            _values.Add(name, value);
        }

        public T? GetValue<T>(string name)
        {
            if (_values == null) return default;
            if (!_values.ContainsKey(name)) return default;
            return (T?)_values[name];
        }

        public void ClearValue(string name)
        {
            _values?.Remove(name);
        }

        public override string ToString()
        {
            return _sid;
        }
    }
}