using System.Collections.Concurrent;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Text.Unicode;
using PurpleSofa;

int workMin;  
int ioMin;  
ThreadPool.GetMinThreads(out workMin, out ioMin);
Console.WriteLine("MinThreads work={0}, i/o={1}", workMin, ioMin); 
ThreadPool.SetMinThreads(workMin * 8, ioMin * 8);   
ThreadPool.GetMinThreads(out workMin, out ioMin);
Console.WriteLine("MinThreads work={0}, i/o={1}", workMin, ioMin); 

PsDate.AddSeconds = 60 * 60 * 9;
PsLogger.Verbose = true;
// PsLogger.StopLogger = true;

var holder = new FrontBackHolder();
var multiClient = new PsMultiClient(new MultiClientCallback(holder));
multiClient.InitBundle();

var serverTask = Task.Run(async () =>
{
    var server = new PsServer(new SeverCallback(holder, multiClient))
    {
        Host = "0.0.0.0",
        Port = 8710
    };
    server.Start();
    server.WaitFor();
    await Task.Delay(1000);
});

serverTask.Wait();
PsLogger.Close();

internal class SeverCallback : PsCallback
{
    private readonly FrontBackHolder _holder;
    
    private readonly PsMultiClient _multiClient;

    private readonly List<Backend> _backends = new List<Backend>();

    private int _forwardNo;
    
    public SeverCallback(FrontBackHolder holder, PsMultiClient multiClient)
    {
        _holder = holder;
        _multiClient = multiClient;
        
        _backends.Add(new Backend{Host = "127.0.0.1", Port = 33306});
        // _backends.Add(new Backend{Host = "127.0.0.1", Port = 8081});
        // _backends.Add(new Backend{Host = "127.0.0.1", Port = 8082});
        // _backends.Add(new Backend{Host = "127.0.0.1", Port = 8083});
    }

    public override void OnOpen(PsSession session)
    {
        Console.WriteLine("OnOpen front -> proxy");
        
        var idx = Interlocked.Increment(ref _forwardNo) % _backends.Count;
        var backend = _backends[idx];
        Console.WriteLine($"backend:{backend}");
        session.SetValue("backend", backend);

        var connectionId = Guid.NewGuid();
        Console.WriteLine($"connectionId:{connectionId}");
        session.SetValue("connectionId", connectionId);
        
        var clientConnection = _multiClient.Connect(backend.Host, backend.Port, connectionId);
        session.SetValue("clientConnection", clientConnection);
        
        var space = _holder.AllocateSpace(connectionId);
        space.FrontSession = session;
    }

    public override void OnMessage(PsSession session, byte[] message)
    {
        Console.WriteLine("OnMessage front -> proxy");

        var connectionId = session.GetValue<Guid>("connectionId");
        var clientConnection = session.GetValue<PsMultiClientConnection>("clientConnection");
        if (!clientConnection.Socket.Connected)
        {
            var backend = session.GetValue<Backend>("backend");
            clientConnection = _multiClient.Connect(backend.Host, backend.Port, connectionId);
            session.SetValue("clientConnection", clientConnection);
        }
        
        var space = _holder.GetSpace(connectionId);
        space.SendToBack(message);
    }

    public override void OnClose(PsSession session, PsCloseReason closeReason)
    {
        var connectionId = session.GetValue<Guid>("connectionId");
        Console.WriteLine($"e server OnClose:{connectionId}, Reason:{closeReason}");
        _holder.ReleaseSpace(connectionId, session);
    }
}

internal class MultiClientCallback : PsCallback
{
    private FrontBackHolder _holder;
    
    public MultiClientCallback(FrontBackHolder holder)
    {
        _holder = holder;
    }

    public override void OnOpen(PsSession session)
    {
        Console.WriteLine("OnOpen proxy -> back");
        
        Console.WriteLine($"e client OnOpen:{session.GetConnectionId()}");
        var space = _holder.AllocateSpace(session.GetConnectionId());
        space.BackSession = session;
    }

    public override void OnMessage(PsSession session, byte[] message)
    {
        Console.WriteLine("OnMessage back -> proxy");

        var space = _holder.AllocateSpace(session.GetConnectionId());
        space.SendToFront(message);
    }

    public override void OnClose(PsSession session, PsCloseReason closeReason)
    {
        Console.WriteLine($"e client OnClose:{session.GetConnectionId()}, Reason:{closeReason}");
        _holder.ReleaseSpace(session.GetConnectionId(), session);
    }
}

internal class Backend
{
    public string Host { get; init; } = null!;
    public int Port { get; init; }

    public override string ToString()
    {
        return $"{Host}:{Port}";
    }
}

internal class Space
{
    private Queue<byte[]> _toFrontBufferList = new ();
    private Queue<byte[]> _toBackBufferList = new ();

    private PsSession? _frontSession;
    private PsSession? _backSession;

    public PsSession? FrontSession
    {
        get => _frontSession;
        set
        {
            _frontSession = value;
            FlushToFront();
        }
    }
    
    public PsSession? BackSession
    {
        get => _backSession;
        set
        {
            _backSession = value;
            FlushToBack();
        }
    }
    
    public void SendToBack(byte[] message)
    {
        if (_backSession == null)
        {
            lock (_toBackBufferList)
            {
                _toBackBufferList.Enqueue(message);    
            }
            return;
        }
        
        FlushToBack();
        try
        {
            _backSession?.Send(message);
            Console.WriteLine($"Send proxy -> back ({message.Length})");
            // Console.WriteLine(Encoding.UTF8.GetString(message));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public void SendToFront(byte[] message)
    {
        if (_frontSession == null)
        {
            lock (_toFrontBufferList)
            {
                _toFrontBufferList.Enqueue(message);    
            }
            return;
        }
        
        FlushToFront();
        try
        {
            _frontSession?.Send(message);
            Console.WriteLine($"Send proxy -> front ({message.Length})");
            // Console.WriteLine(Encoding.UTF8.GetString(message));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public void FlushToFront()
    {
        if (_frontSession == null) return;

        lock (_toFrontBufferList)
        {
            foreach (var msg in _toFrontBufferList)
            {
                try
                {
                    _frontSession.Send(msg);
                    Console.WriteLine($"Flush proxy -> front ({msg.Length})");
                    // Console.WriteLine(Encoding.UTF8.GetString(msg));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            _toFrontBufferList.Clear(); 
        }
    }

    public void FlushToBack()
    {
        if (_backSession == null) return;

        lock (_toBackBufferList)
        {
            foreach (var msg in _toBackBufferList)
            {
                try
                {
                    _backSession.Send(msg);
                    Console.WriteLine($"Flush proxy -> back ({msg.Length})");
                    // Console.WriteLine(Encoding.UTF8.GetString(msg));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            _toBackBufferList.Clear();
        }
    }
}

internal class FrontBackHolder
{
    private readonly ConcurrentDictionary<Guid, Space> _endpointSpaceMap = new();
    
    public Space AllocateSpace(Guid connectionId)
    {
        return _endpointSpaceMap.GetOrAdd(connectionId, new Space());
    }

    public Space? GetSpace(Guid connectionId)
    {
        if (_endpointSpaceMap.TryGetValue(connectionId, out var space))
        {
            return space;
        }
        return null;
    }
    
    public void ReleaseSpace(Guid connectionId, PsSession closedSession)
    {
        lock (connectionId.ToString())
        {
            var space = GetSpace(connectionId);
            if (space == null) return;
            
            if (closedSession == space.BackSession)
            {
                space.BackSession = null;
                space.FlushToFront();
                // space.FrontSession?.Close();
            }
            if (closedSession == space.FrontSession)
            {
                space.FrontSession = null;
                space.FlushToBack();
                // space.BackSession?.Close();
            }
            
            if ((space.FrontSession == null || !space.FrontSession.IsOpen())
                && (space.BackSession == null || !space.BackSession.IsOpen()))
            {
                Console.WriteLine("ReleaseSpace");
                _endpointSpaceMap.TryRemove(connectionId, out _);
            }
        }
    }
}