using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PurpleSofa.Tests
{
    public class TestClientAsync
    {
        public TestClientAsync(ITestOutputHelper helper)
        {
            PsDate.AddSeconds = 60 * 60 * 9;
            PsLogger.Verbose = true;
            PsLogger.Writer = new StreamWriter(new FileStream("Test.log", FileMode.Append));
            // PsLogger.Transfer = new PsLoggerTransfer
            // {
            //     Transfer = msg => helper.WriteLine(msg.ToString()),
            //     Raw = false
            // };
        }

        [Fact]
        public void TestAsyncClientClose()
        {
            var server = new PsServer(new AsyncCallbackServer(){UseAsyncCallback = true});
            server.Start();

            var tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                tasks.Add(Task.Run(async () =>
                    {
                        var client = new PsClient(new AsyncCallbackClient(){UseAsyncCallback = true}, "127.0.0.1", 8710)
                        {
                            ReadBufferSize = 1024
                        };
                        client.Connect();
                        await Task.Delay(10000);
                        client.Disconnect();
                    }));
            }

            Task.WaitAll(tasks.ToArray());
            
            Thread.Sleep(5000);
            
            server.Shutdown();
            PsLogger.Close();
        }
        
        [Fact]
        public void TestAsyncServerClose()
        {
            var server = new PsServer(new AsyncCallbackServer(){UseAsyncCallback = true});
            server.Start();

            var tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var client = new PsClient(new AsyncCallbackClient(){UseAsyncCallback = true}, "127.0.0.1", 8710);
                    client.Connect();
                }));
            }

            server.Shutdown();
            
            Thread.Sleep(5000);
            PsLogger.Close();
        }
    }
    
    public class AsyncCallbackServer : PsCallback
    {
        private const string Key = "inc";

        private readonly PsLock _lock = new();

        private List<PsSession> _sessions = new List<PsSession>();
        
        public override async Task OnOpenAsync(PsSession session)
        {
            using (await _lock.LockAsync())
            {
                _sessions.Add(session);
                
                session.SetValue(Key, 0);
                foreach (var s in _sessions)
                {
                    // s.Send($"Hello {s.RemoteEndPoint}.".PxToBytes());    
                    await s.SendAsync($"Hello num:{_sessions.Count}.".PxToBytes());    
                }
            }
        }

        public override async Task OnMessageAsync(PsSession session, byte[] message)
        {
            using (await _lock.LockAsync())
            {
                // PsLogger.Info($"Receive from client: '{message.PxToString()}' ({session}) before:{PsDate.Now()}.");
                // Do not use 'await'.
                // await Task.Delay(2000);
                // PsLogger.Info($"Receive from client: '{message.PxToString()}' ({session}) after:{PsDate.Now()}.");
            
                int inc = session.GetValue<int>(Key);
                inc++;
                session.SetValue(Key, inc);
            
                var reply = $"s{inc}";
                // PsLogger.Info($"Receive from client: '{message.PxToString()}' ({session}) last:{PsDate.Now()}.");
                foreach (var s in _sessions)
                {
                    // s.Send(reply.PxToBytes());    
                    await s.SendAsync(message);    
                }
            }
        }

        public override async Task OnCloseAsync(PsSession session, PsCloseReason closeReason)
        {
            using (await _lock.LockAsync())
            {
                _sessions.Remove(session);
                foreach (var s in _sessions)
                {
                    var reply = $"Goodby {session.RemoteEndPoint} for {closeReason} at server.";
                    await s.SendAsync(reply.PxToBytes());    
                }
            }
        }
    }
    
    public class AsyncCallbackClient : PsCallback
    {
        private const string Key = "inc";
        
        public override Task OnOpenAsync(PsSession session)
        {
            session.SetValue(Key, 0);
            // session.Send($"Hello {session.LocalEndPoint}.".PxToBytes());
            return Task.CompletedTask;
        }

        public override async Task OnMessageAsync(PsSession session, byte[] message)
        {
            PsLogger.Info($"Receive from server: '{message.PxToString()}' ({session}).");
            
            int inc = session.GetValue<int>(Key);
            inc++;
            session.SetValue(Key, inc);
            
            if (inc > 5) return;
            var reply = $"c{inc}";
            await session.SendAsync(reply.PxToBytes());
        }

        public override Task OnCloseAsync(PsSession session, PsCloseReason closeReason)
        {
            PsLogger.Info($"Goodby {session.LocalEndPoint} for {closeReason} at client.");
            return Task.CompletedTask;
        }
    }
}