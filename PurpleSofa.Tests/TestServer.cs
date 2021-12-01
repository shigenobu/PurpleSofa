using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace PurpleSofa.Tests
{
    public class TestServer
    {
        public TestServer(ITestOutputHelper helper)
        {
            // PsLogger.Writer = new StreamWriter(new FileStream("PurpleSofa.log", FileMode.Append));
            PsLogger.Verbose = true;
        }

        [Fact]
        public void TestSimple()
        {
            var server = new PsServer(new Callback());
            server.Start();
        }
    }

    public class Callback : PsCallback
    {
        public override void OnOpen(PsSession session)
        {
            PsLogger.Debug($"OnOpen {session}");
        }

        public override void OnMessage(PsSession session, byte[] message)
        {
            PsLogger.Debug($"OnMessage {session} {message.PxToString()}");
        }

        public override void OnClose(PsSession session, PsCloseReason closeReason)
        {
            PsLogger.Debug($"OnClose {session} {closeReason}");
        }
    }
}