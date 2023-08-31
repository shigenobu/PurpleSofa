using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PurpleSofa.Tests;

public class TestMultiClient
{
    public TestMultiClient(ITestOutputHelper helper)
    {
        PsDate.AddSeconds = 60 * 60 * 9;
        // PsLogger.Verbose = true;
        PsLogger.Writer = new StreamWriter(new FileStream("TestMulti.log", FileMode.Append));
    }
    
    [Fact]
    public void TestMultiClientClose()
    {
        var server = new PsServer(new CallbackServer());
        server.Start();

        var multiClient = new PsMultiClient(new CallbackClient())
        {
            ReadBufferSize = 1024,
            Divide = 5
        };
        multiClient.InitBundle();
        
        var tasks = new List<Task>();
        for (int i = 0; i < 2; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var clientSocket = multiClient.Connect("127.0.0.1", 8710);
                await Task.Delay(5);
                multiClient.Disconnect(clientSocket);
            }));
        }

        Task.WaitAll(tasks.ToArray());
            
        Thread.Sleep(5000);
            
        multiClient.DestroyBundle();
        server.Shutdown();
        PsLogger.Close();
    }
}