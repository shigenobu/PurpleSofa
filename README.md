# PurpleSofa - C# .NET async tcp server & client

[![nuget](https://badgen.net/nuget/v/PurpleSofa/latest)](https://www.nuget.org/packages/PurpleSofa/)
[![.NET CI](https://github.com/shigenobu/PurpleSofa/actions/workflows/ci.yaml/badge.svg?branch=develop)](https://github.com/shigenobu/PurpleSofa/actions/workflows/ci.yaml)
[![codecov](https://codecov.io/gh/shigenobu/PurpleSofa/branch/develop/graph/badge.svg?token=RNH9EOC8JF)](https://codecov.io/gh/shigenobu/PurpleSofa)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

## feature

* Callback for 'OnOpen'(accepted or connected), 'OnMessage'(received), 'OnClose'(received none).
* Can store user value in session.
* Check timeout at regular intervals by last receive time. It's useful to detect 'half close'.
* 'OnClose' execution is taken via queue in order to avoid simultaneously many 'close'.

## how to use

### callback

    public class Callback : PsCallback
    {
        private const string Key = "inc";
        
        public override void OnOpen(PsSession session)
        {
            Console.WriteLine($"OnOpen {session}");
            session.SetValue(Key, 0);
            session.ChangeIdleMilliSeconds(5000);

            int inc = session.GetValue<int>(Key);
            session.Send($"inc: {inc}");
        }

        public override void OnMessage(PsSession session, byte[] message)
        {
            Console.WriteLine($"OnMessage {session} {Encoding.UTF8.GetString(message)}");
            int inc = session.GetValue<int>(Key);
            inc++;
            session.SetValue(Key, inc);
            session.Send($"inc: {inc}");
            if (inc > 3) session.Close();
        }

        public override void OnClose(PsSession session, PsCloseReason closeReason)
        {
            session.ClearValue(Key);
            int inc = session.GetValue<int>(Key);
            Console.WriteLine($"OnClose {session} {closeReason}, inc:{inc}");
        }
    }

### for server

    public static void Main(string[] args)
    {
        var server = new PsServer(new Callback());
        server.Start();
        server.WaitFor();
        // --- another thread
        // server.Shutdown();
    }

### for client

    public static void Main(string[] args)
    {
        var client = new PsClient(new Callback(), "127.0.0.1", 8710);
        client.Connect();
        // ...
        client.Disconnect();
    }
