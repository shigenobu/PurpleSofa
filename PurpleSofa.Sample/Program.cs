using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PurpleSofa.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            PsDate.AddSeconds = 60 * 60 * 9;
            // PsLogger.Verbose = true;
            PsLogger.StopLogger = true;
            
            var serverTask = StartServer();
            var clientTasks = StartClient();

            Task.WaitAll(clientTasks.ToArray());
            serverTask.Wait();

            PsLogger.Close();
        }

        private static Task StartServer()
        {
            return Task.Run(async () =>
            {
                var server = new PsServer(new Callback())
                {
                    Host = "0.0.0.0",
                    Port = 8710
                };
                server.Start();
                // server.WaitFor();
                await Task.Delay(1000);
            });
        }

        private static List<Task> StartClient()
        {
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var client = new PsClient(new Callback(), "127.0.0.1", 8710)
                    {
                        ReadBufferSize = 1024
                    };
                    client.Connect();
                    await Task.Delay(100);
                    client.Disconnect();
                }));
            }

            return tasks;
        }
        
        public class Callback : PsCallback
        {
            private const string Key = "inc";
            
            public override void OnOpen(PsSession session)
            {
                Console.WriteLine($"OnOpen {session}");
                session.SetValue(Key, 0);
                session.ChangeIdleMilliSeconds(5000);

                int inc = session.GetValue<int>(Key);
                session.Send($"inc:{inc}");
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
    }
}

