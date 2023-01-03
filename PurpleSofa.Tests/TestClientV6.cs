using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PurpleSofa.Tests
{
    public class TestClientV6
    {
        public TestClientV6(ITestOutputHelper helper)
        {
            PsDate.AddSeconds = 60 * 60 * 9;
            // PsLogger.Verbose = true;
            PsLogger.Writer = new StreamWriter(new FileStream("TestV6.log", FileMode.Append));
        }

        [Fact]
        public void TestClientCloseV6()
        {
            var server = new PsServer(new CallbackServerV6())
            {
                SocketAddressFamily = PsSocketAddressFamily.Ipv6
            };
            server.Start();

            var tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                tasks.Add(Task.Run(async () =>
                    {
                        var client = new PsClient(new CallbackClient(), PsSocketAddressFamily.Ipv6, "::1", 8710)
                        {
                            ReadBufferSize = 1024
                        };
                        client.Connect();
                        await Task.Delay(5);
                        client.Disconnect();
                    }));
            }

            Task.WaitAll(tasks.ToArray());
            
            Thread.Sleep(5000);
            
            server.Shutdown();
            PsLogger.Close();
        }
        
        [Fact]
        public void TestServerCloseV6()
        {
            var server = new PsServer(new CallbackServerV6())
            {
                SocketAddressFamily = PsSocketAddressFamily.Ipv6
            };
            server.Start();

            var tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var client = new PsClient(new CallbackClient(), PsSocketAddressFamily.Ipv6, "::1", 8710);
                    client.Connect();
                }));
            }

            server.Shutdown();
            
            Thread.Sleep(5000);
            PsLogger.Close();
        }
    }
    
    public class CallbackServerV6 : PsCallback
    {
        private const string Key = "inc";
        
        public override void OnOpen(PsSession session)
        {
            session.SetValue(Key, 0);
            session.Send($"Hello {session.RemoteEndPoint}.".PxToBytes());
        }

        public override void OnMessage(PsSession session, byte[] message)
        {
            PsLogger.Info($"Receive from client: '{message.PxToString()}' ({session}).");
            
            int inc = session.GetValue<int>(Key);
            inc++;
            session.SetValue(Key, inc);
            
            var reply = $"{inc}";
            session.Send(reply.PxToBytes());
        }

        public override void OnClose(PsSession session, PsCloseReason closeReason)
        {
            PsLogger.Info($"Goodby {session.RemoteEndPoint} for {closeReason} at server.");
        }
    }
    
    public class CallbackClientV6 : PsCallback
    {
        private const string Key = "inc";
        
        public override void OnOpen(PsSession session)
        {
            session.SetValue(Key, 0);
            session.Send($"Hello {session.LocalEndPoint}.".PxToBytes());
        }

        public override void OnMessage(PsSession session, byte[] message)
        {
            PsLogger.Info($"Receive from server: '{message.PxToString()}' ({session}).");
            
            int inc = session.GetValue<int>(Key);
            inc++;
            session.SetValue(Key, inc);
            
            if (inc > 5) return;
            var reply = $"{inc}";
            session.Send(reply.PxToBytes());
            
        }

        public override void OnClose(PsSession session, PsCloseReason closeReason)
        {
            PsLogger.Info($"Goodby {session.LocalEndPoint} for {closeReason} at client.");
        }
    }
}