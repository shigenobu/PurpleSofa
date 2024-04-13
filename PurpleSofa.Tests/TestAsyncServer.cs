using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PurpleSofa.Tests
{
    public class TestAsyncServer
    {
        public TestAsyncServer(ITestOutputHelper helper)
        {
            PsDate.AddSeconds = 60 * 60 * 9;
            PsLogger.Writer = new StreamWriter(new FileStream("PurpleSofa.log", FileMode.Append));
            PsLogger.Verbose = true;
        }

        [Fact]
        public void TestAsync()
        {
            var server = new PsServer(new AsyncCallback(){CallbackMode = PsCallbackMode.Async})
            {
                SocketAddressFamily = PsSocketAddressFamily.Ipv6,
                Host = "0.0.0.0",
                Port = 8710,
                ReadBufferSize = 1024,
                ReceiveBufferSize = 1024 * 1024 * 16,
                Backlog = 128,
                Divide = 1
            };
            server.Start();
            Task.Run(async () =>
            {
                while (true)
                {
                    PsLogger.Debug($"session count:{server.GetSessionCount()}");
                    await Task.Delay(5000); 
                }
            });
            // server.WaitFor();
            Thread.Sleep(10000);
            server.Shutdown();
        }
    }

    public class AsyncCallback : PsCallback
    {
        private const string Key = "inc";
        
        public override async Task OnOpenAsync(PsSession session)
        {
            PsLogger.Debug($"OnOpen {session}");
            session.SetValue(Key, 0);
            session.ChangeIdleMilliSeconds(5000);

            int inc = session.GetValue<int>(Key);
            await session.SendAsync($"On Open inc: {inc}\n".PxToBytes());
        }

        public override async Task OnMessageAsync(PsSession session, byte[] message)
        {
            PsLogger.Debug($"OnMessage {session} {message.PxToString()}");
            int inc = session.GetValue<int>(Key);
            inc++;
            session.SetValue(Key, inc);
            await session.SendAsync($"On Message inc: {inc}\n".PxToBytes());
            if (inc > 3) session.Close();
        }

        public override Task OnCloseAsync(PsSession session, PsCloseReason closeReason)
        {
            session.ClearValue(Key);
            int inc = session.GetValue<int>(Key);
            PsLogger.Debug($"OnClose {session} {closeReason}, inc:{inc}");
            return Task.CompletedTask;
        }
    }
}