using System.Collections.Concurrent;
using System.Text;
using PurpleSofa;

// int workMin;  
// int ioMin;  
// ThreadPool.GetMinThreads(out workMin, out ioMin);
// Console.WriteLine("MinThreads work={0}, i/o={1}", workMin, ioMin); 
// ThreadPool.SetMinThreads(workMin * 8, ioMin * 8);   
// ThreadPool.GetMinThreads(out workMin, out ioMin);
// Console.WriteLine("MinThreads work={0}, i/o={1}", workMin, ioMin); 

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
    private readonly List<Backend> _backends = new();
    private readonly FrontBackHolder _holder;

    private readonly PsMultiClient _multiClient;

    private int _forwardNo;

    public SeverCallback(FrontBackHolder holder, PsMultiClient multiClient)
    {
        _holder = holder;
        _multiClient = multiClient;

        _backends.Add(new Backend {Host = "127.0.0.1", Port = 5000});
        // _backends.Add(new Backend{Host = "127.0.0.1", Port = 8081});
        // _backends.Add(new Backend{Host = "127.0.0.1", Port = 8082});
        // _backends.Add(new Backend{Host = "127.0.0.1", Port = 8083});
    }

    public override async Task OnOpenAsync(PsSession session)
    {
        Console.WriteLine("OnOpen front -> proxy");

        var idx = Interlocked.Increment(ref _forwardNo) % _backends.Count;
        var backend = _backends[idx];
        Console.WriteLine($"backend:{backend}");
        session.SetValue("backend", backend);

        var connectionId = Guid.NewGuid();
        Console.WriteLine($"connectionId:{connectionId}");
        session.SetValue("connectionId", connectionId);

        _multiClient.Connect(backend.Host, backend.Port, connectionId);
        var space = _holder.AllocateSpace(connectionId);
        await space.SetFrontSessionAsync(session);
    }

    public override async Task OnMessageAsync(PsSession session, byte[] message)
    {
        Console.WriteLine("OnMessage front -> proxy");

        var connectionId = session.GetValue<Guid>("connectionId");
        var space = _holder.GetSpace(connectionId);
        await space?.SendToBackAsync(message)!;
    }

    public override async Task OnCloseAsync(PsSession session, PsCloseReason closeReason)
    {
        var connectionId = session.GetValue<Guid>("connectionId");
        Console.WriteLine($"e server OnClose:{connectionId}, Reason:{closeReason}");
        await _holder.ReleaseSpaceAsync(connectionId, session);
    }
}

internal class MultiClientCallback(FrontBackHolder holder) : PsCallback
{
    public override async Task OnOpenAsync(PsSession session)
    {
        Console.WriteLine("OnOpen proxy -> back");

        Console.WriteLine($"e client OnOpen:{session.GetConnectionId()}");
        var space = holder.AllocateSpace(session.GetConnectionId());
        await space.SetBackSessionAsync(session);
    }

    public override async Task OnMessageAsync(PsSession session, byte[] message)
    {
        Console.WriteLine("OnMessage back -> proxy");

        var space = holder.AllocateSpace(session.GetConnectionId());
        await space.SendToFrontAsync(message);
    }

    public override async Task OnCloseAsync(PsSession session, PsCloseReason closeReason)
    {
        Console.WriteLine($"e client OnClose:{session.GetConnectionId()}, Reason:{closeReason}");
        await holder.ReleaseSpaceAsync(session.GetConnectionId(), session);
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

internal class Locker
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IDisposable> LockAsync()
    {
        await _semaphore.WaitAsync();
        return new LockerHandler(_semaphore);
    }

    private sealed class LockerHandler(SemaphoreSlim semaphore) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            semaphore.Release();
            _disposed = true;
        }
    }
}

internal class Space
{
    private readonly Locker _backLocker = new();
    private readonly Locker _frontLocker = new();
    private readonly Queue<byte[]> _toBackBufferList = new();

    private readonly Queue<byte[]> _toFrontBufferList = new();

    public Locker FrontBackLocker => new();

    public PsSession? FrontSession { get; private set; }

    public PsSession? BackSession { get; private set; }

    public async Task SetFrontSessionAsync(PsSession? frontSession)
    {
        FrontSession = frontSession;
        await FlushToFrontAsync();
    }

    public async Task SetBackSessionAsync(PsSession? backSession)
    {
        BackSession = backSession;
        await FlushToBackAsync();
    }

    public async Task SendToBackAsync(byte[] message)
    {
        if (BackSession == null)
        {
            using (await _backLocker.LockAsync())
            {
                _toBackBufferList.Enqueue(message);
            }

            return;
        }

        await FlushToBackAsync();
        try
        {
            await BackSession?.SendAsync(message)!;
            Console.WriteLine($"Send proxy -> back ({message.Length})");
            Console.WriteLine(Encoding.UTF8.GetString(message));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task SendToFrontAsync(byte[] message)
    {
        if (FrontSession == null)
        {
            using (await _frontLocker.LockAsync())
            {
                _toFrontBufferList.Enqueue(message);
            }

            return;
        }

        await FlushToFrontAsync();
        try
        {
            await FrontSession?.SendAsync(message)!;
            Console.WriteLine($"Send proxy -> front ({message.Length})");
            Console.WriteLine(Encoding.UTF8.GetString(message));
            // Console.WriteLine(Encoding.UTF8.GetString(message));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task FlushToFrontAsync()
    {
        if (FrontSession == null) return;

        using (await _frontLocker.LockAsync())
        {
            foreach (var msg in _toFrontBufferList)
                try
                {
                    await FrontSession.SendAsync(msg);
                    Console.WriteLine($"Flush proxy -> front ({msg.Length})");
                    // Console.WriteLine(Encoding.UTF8.GetString(msg));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            _toFrontBufferList.Clear();
        }
    }

    public async Task FlushToBackAsync()
    {
        if (BackSession == null) return;

        using (await _backLocker.LockAsync())
        {
            foreach (var msg in _toBackBufferList)
                try
                {
                    await BackSession.SendAsync(msg);
                    Console.WriteLine($"Flush proxy -> back ({msg.Length})");
                    // Console.WriteLine(Encoding.UTF8.GetString(msg));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
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
        return _endpointSpaceMap.GetValueOrDefault(connectionId);
    }

    public async Task ReleaseSpaceAsync(Guid connectionId, PsSession closedSession)
    {
        var space = GetSpace(connectionId);
        if (space == null) return;

        using (await space.FrontBackLocker.LockAsync())
        {
            if (closedSession == space.BackSession)
            {
                await space.SetBackSessionAsync(null);
                await space.FlushToFrontAsync();
                // space.FrontSession?.Close();
            }

            if (closedSession == space.FrontSession)
            {
                await space.SetFrontSessionAsync(null);
                await space.FlushToBackAsync();
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