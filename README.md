# PurpleSofa - C# .NET async tcp server & client

[![nuget](https://badgen.net/nuget/v/PurpleSofa/latest)](https://www.nuget.org/packages/PurpleSofa/)
[![.NET CI](https://github.com/shigenobu/PurpleSofa/actions/workflows/ci.yaml/badge.svg?branch=develop)](https://github.com/shigenobu/PurpleSofa/actions/workflows/ci.yaml)
[![codecov](https://codecov.io/gh/shigenobu/PurpleSofa/branch/develop/graph/badge.svg?token=RNH9EOC8JF)](https://codecov.io/gh/shigenobu/PurpleSofa)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

## feature

PurpleSofa is __'Asynchronous Programming Model (APM)'__ socket wrapper library,  
with __'Task-based Asynchronous Pattern (TAP)'__ at callback methods.  
Otherwise, __APM__ and __TAP__ mixed.

* Callback for
    * 'OnOpenAsync' (accepted or connected)
    * 'OnMessageAsync' (received)
    * 'OnCloseAsync' (received none).
* Can store user value in session.
* Check timeout at regular intervals by last receive time. It's useful to detect 'half close'.
* 'OnCloseAsync' execution is taken via queue in order to avoid simultaneously many 'close'.

## how to use

### callback

    public class Callback : PsCallback
    {
        private const string Key = "inc";

        public override async Task OnOpenAsync(PsSession session)
        {
            Console.WriteLine($"OnOpen {session}");
            session.SetValue(Key, 0);
            session.ChangeIdleMilliSeconds(5000);

            var inc = session.GetValue<int>(Key);
            await session.SendAsync($"inc:{inc}");
        }

        public override async Task OnMessageAsync(PsSession session, byte[] message)
        {
            Console.WriteLine($"OnMessage {session} {Encoding.UTF8.GetString(message)}");
            var inc = session.GetValue<int>(Key);
            inc++;
            session.SetValue(Key, inc);
            await session.SendAsync($"inc: {inc}");
            if (inc > 3) session.Close();
        }

        public override Task OnCloseAsync(PsSession session, PsCloseReason closeReason)
        {
            session.ClearValue(Key);
            var inc = session.GetValue<int>(Key);
            Console.WriteLine($"OnClose {session} {closeReason}, inc:{inc}");
            return Task.CompletedTask;
        }
    }

### for server (ip v4)

    public static void Main(string[] args)
    {
        var server = new PsServer(new Callback());
        server.Start();
        server.WaitFor();
        // --- another thread
        // server.Shutdown();
    }

### for client (ip v4)

    public static void Main(string[] args)
    {
        var client = new PsClient(new Callback(), "127.0.0.1", 8710);
        client.Connect();
        // ...
        client.Disconnect();
    }

### for server (ip v6)

    public static void Main(string[] args)
    {
        var server = new PsServer(new Callback())
        {
            SocketAddressFamily = PsSocketAddressFamily.Ipv6
        };
        server.Start();
        server.WaitFor();
        // --- another thread
        // server.Shutdown();
    }

* Ipv4 socket is treated as ipv6 socket.
* If host address `0.0.0.0`, changed to `::`.

### for client (ip v6)

    public static void Main(string[] args)
    {
        var client = new PsClient(new Callback(), PsSocketAddressFamily.Ipv6, "::1", 8710);
        // Below is no problem
        // var client = new PsClient(new Callback(), "127.0.0.1", 8710);
        client.Connect();
        // ...
        client.Disconnect();
    }

* Ipv4 socket is treated as ipv6 socket.
* If server is listening on ipv6, client is able to connect to server like v4.

---

### for multi client (ip v4, v6)

    var multiClient = new PsMultiClient(new Callback());
    multiClient.InitBundle();
    for (int i = 0; i < 3; i++)
    {
        // assume that server listening on 8710, 8711, 8712
        var clientConnection = multiClient.Connect("127.0.0.1", 8710 + i);
        // ip v6
        // var clientConnection = multiClient.Connect(PsSocketAddressFamily.Ipv6, "::1", 8710 + i);
        :
        multiClient.Disconnect(clientConnection);
    }
    multiClient.DestroyBundle();
