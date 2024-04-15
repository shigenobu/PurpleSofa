using System.Net;
using System.Net.Sockets;

namespace PurpleSofa;

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
    ///     Socket.
    /// </summary>
    private readonly Socket _clientSocket;

    /// <summary>
    ///     Connection id.
    /// </summary>
    private readonly Guid _connectionId;

    /// <summary>
    ///     Idle milli seconds.
    /// </summary>
    private int _idleMilliSeconds = 60000;

    /// <summary>
    ///     Life timestamp milli seconds.
    /// </summary>
    private long _lifeTimestampMilliseconds;

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
    /// <param name="connectionId">connection id</param>
    public PsSession(Socket clientSocket, Guid connectionId)
    {
        _connectionId = connectionId;
        _clientSocket = clientSocket;
        LocalEndPoint = _clientSocket.PxSocketLocalEndPoint();
        RemoteEndPoint = _clientSocket.PxSocketRemoteEndPoint();
    }

    /// <summary>
    ///     Local endpoint.
    /// </summary>
    public EndPoint? LocalEndPoint { get; }

    /// <summary>
    ///     Remote endpoint.
    /// </summary>
    public EndPoint? RemoteEndPoint { get; }

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
    internal bool SelfClosed { get; private set; }

    /// <summary>
    ///     Lock.
    /// </summary>
    internal PsLock Lock { get; } = new();

    /// <summary>
    ///     Get connection id.
    /// </summary>
    /// <returns>connection id</returns>
    public Guid GetConnectionId()
    {
        return _connectionId;
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
    public bool IsOpen()
    {
        return !CloseHandlerCalled
               && !ShutdownHandlerCalled
               && !SelfClosed;
    }

    /// <summary>
    ///     Send string.
    /// </summary>
    /// <param name="message">message</param>
    /// <param name="timeout">timeout</param>
    /// <exception cref="PsSendException">send error</exception>
    public void Send(string message, int timeout = DefaultTimeoutMilliSeconds)
    {
        Send(message.PxToBytes(), timeout);
    }

    /// <summary>
    ///     Send string.
    /// </summary>
    /// <param name="message">message</param>
    /// <param name="timeout">timeout</param>
    /// <exception cref="PsSendException">send error</exception>
    public async Task SendAsync(string message, int timeout = DefaultTimeoutMilliSeconds)
    {
        await SendAsync(message.PxToBytes(), timeout);
    }

    /// <summary>
    ///     Send bytes.
    /// </summary>
    /// <param name="message">message</param>
    /// <param name="timeout">timeout</param>
    /// <exception cref="PsSendException">send error</exception>
    public void Send(byte[] message, int timeout = DefaultTimeoutMilliSeconds)
    {
        SendAsync(message, timeout).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     Async send bytes.
    /// </summary>
    /// <param name="message">message</param>
    /// <param name="timeout">timeout</param>
    /// <exception cref="PsSendException">send error</exception>
    public async Task SendAsync(byte[] message, int timeout = DefaultTimeoutMilliSeconds)
    {
        try
        {
            if (!IsOpen()) return;

            _clientSocket.SendTimeout = timeout;
            var t = _clientSocket.SendAsync(new ArraySegment<byte>(message), SocketFlags.None);
            if (await Task.WhenAny(t, Task.Delay(timeout)) != t)
                throw new PsSendException($"Error send to {this} ({message.Length})");
            PsLogger.Debug(() => $"Send to {this} ({message.Length})");
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
        CloseQueue?.Add(new PsStateRead
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
        return (T?) _values[name];
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
        return $"ConnectionId:{_connectionId}, Local:{LocalEndPoint}, Remote:{RemoteEndPoint}";
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
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="msg">msg</param>
    internal PsSendException(string msg) : base(msg)
    {
    }
}