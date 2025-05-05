using Xunit;
using Xunit.Abstractions;

namespace PurpleSofa.Tests;

public class TestClientV4V6Async
{
    public TestClientV4V6Async(ITestOutputHelper helper)
    {
        PsDate.AddSeconds = 60 * 60 * 9;
        // PsLogger.Verbose = true;
        PsLogger.Writer = new StreamWriter(new FileStream("TestV4V6.log", FileMode.Append));
    }

    [Fact]
    public void TestClientCloseV4V6()
    {
        var server = new PsServer(new AsyncCallbackServer())
        {
            SocketAddressFamily = PsSocketAddressFamily.Ipv6
        };
        server.Start();

        var tasks = new List<Task>();
        for (var i = 0; i < 2; i++)
            tasks.Add(Task.Run(async () =>
            {
                var client = new PsClient(new AsyncCallbackClient(), "127.0.0.1", 8710)
                {
                    ReadBufferSize = 1024
                };
                client.Connect();
                await Task.Delay(5);
                client.Disconnect();
            }));

        Task.WaitAll(tasks.ToArray());

        Thread.Sleep(5000);

        server.Shutdown();
        PsLogger.Close();
    }

    [Fact]
    public void TestServerCloseV4V6()
    {
        var server = new PsServer(new AsyncCallbackServer())
        {
            SocketAddressFamily = PsSocketAddressFamily.Ipv6
        };
        server.Start();

        var tasks = new List<Task>();
        for (var i = 0; i < 2; i++)
            tasks.Add(Task.Run(() =>
            {
                var client = new PsClient(new AsyncCallbackClient(), "127.0.0.1", 8710);
                client.Connect();
            }));

        server.Shutdown();

        Thread.Sleep(5000);
        PsLogger.Close();
    }
}