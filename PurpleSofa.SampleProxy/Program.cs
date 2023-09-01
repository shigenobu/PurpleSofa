using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Unicode;
using PurpleSofa;

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
        
        _backends.Add(new Backend{Host = "127.0.0.1", Port = 8000});
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
        
        var cs = _multiClient.Connect(backend.Host, backend.Port);
        session.SetValue("csse", cs.Socket.LocalEndPoint);
        
        Console.WriteLine($"e server OnOpen:{cs.Socket.LocalEndPoint}");
        var space = _holder.AllocateSpace(cs.Socket.LocalEndPoint);
        space.FrontSession = session;
    }

    public override void OnMessage(PsSession session, byte[] message)
    {
        Console.WriteLine("OnMessage front -> proxy");

        var csse = session.GetValue<EndPoint>("csse");
        var space = _holder.GetSpace(csse);
        space.ToBack(message);
    }

    public override void OnClose(PsSession session, PsCloseReason closeReason)
    {
        var csse = session.GetValue<EndPoint>("csse");
        Console.WriteLine($"e server OnClose:{csse}, Reason:{closeReason}");
        _holder.ReleaseSpace(csse);
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
        
        Console.WriteLine($"e client OnOpen:{session.LocalEndPoint}");
        var space = _holder.AllocateSpace(session.LocalEndPoint);
        space.BackSession = session;
    }

    public override void OnMessage(PsSession session, byte[] message)
    {
        Console.WriteLine("OnMessage back -> proxy");

        var space = _holder.AllocateSpace(session.LocalEndPoint);
        space.ToFront(message);
    }

    public override void OnClose(PsSession session, PsCloseReason closeReason)
    {
        Console.WriteLine($"e client OnClose:{session.LocalEndPoint}, Reason:{closeReason}");
        _holder.ReleaseSpace(session.LocalEndPoint);
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
    internal PsSession? FrontSession { get; set; }
    internal PsSession? BackSession { get; set; }

    public void ToBack(byte[] message)
    {
        var i = 0;
        while (BackSession == null)
        {
            i++;
            if (i > 100) throw new Exception("timeout ToBack");
            Thread.Sleep(1);
        }
        
        BackSession?.Send(message);
        Console.WriteLine("Send proxy -> back");
        Console.WriteLine(Encoding.UTF8.GetString(message));
    }

    public void ToFront(byte[] message)
    {
        var i = 0;
        while (FrontSession == null)
        {
            i++;
            if (i > 100) throw new Exception("timeout ToFront");
            Thread.Sleep(1);
        }
        
        FrontSession?.Send(message);
        Console.WriteLine("Send proxy -> front");
        Console.WriteLine(Encoding.UTF8.GetString(message));
    }
}

internal class FrontBackHolder
{
    private readonly ConcurrentDictionary<EndPoint, Space> _endpointSpaceMap = new();
    
    public Space AllocateSpace(EndPoint backLocalEndpoint)
    {
        return _endpointSpaceMap.GetOrAdd(backLocalEndpoint, new Space());
    }

    public Space? GetSpace(EndPoint backLocalEndpoint)
    {
        if (_endpointSpaceMap.TryGetValue(backLocalEndpoint, out var space))
        {
            return space;
        }
        return null;
    }
    
    public void ReleaseSpace(EndPoint backLocalEndpoint)
    {
        if (_endpointSpaceMap.TryRemove(backLocalEndpoint, out var space))
        {
            Console.WriteLine("ReleaseSpace");
            space.BackSession?.Close();
            space.FrontSession?.Close();
        } 
    }
}