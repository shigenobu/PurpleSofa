using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;

namespace PurpleSofa
{
    public static class PsLogger
    {
        public static TextWriter? Writer { get; set; } = new StreamWriter(Console.OpenStandardOutput());

        public static bool StopLogger { get; set; }
        
        public static bool Verbose { get; set; }

        public static Action<object?>? Transfer;

        public static void Error(object? message)
        {
            Out("ERROR", message);
        }
        
        public static void Info(object? message)
        {
            Out("INFO", message);
        }
        
        public static void Debug(object? message)
        {
            if (!Verbose) return;
            Out("DEBUG", message);
        }
        
        private static void Out(string name, object? message)
        {
            if (StopLogger && message is not Exception _) return;

            var context = default(PsLoggerContext);
            context.Recorded = PsDate.Now();
            context.ThreadId = $"{Thread.CurrentThread.ManagedThreadId:D10}";
            context.Name = name;
            if (message == null)
                context.Message = "<NULL>";
            else if (message.ToString()!.PxToBytes().Length == 0)
                context.Message = "<EMPTY>";
            else if (message is IEnumerable tmp)
                context.Message = string.Join("\n", tmp);
            else
                context.Message = message.ToString()!;
            
            StringBuilder builder = new();
            builder.Append($"[{context.Recorded}]");
            builder.Append($"[{context.ThreadId}]");
            builder.Append($"[{context.Name}]");
            builder.Append($"{context.Message}\n");
            var log = builder.ToString();
            Transfer?.Invoke(log);

            if (Writer != null)
            {
                Writer.Write(log);
                Writer.Flush();    
            }
        }
    }

    internal struct PsLoggerContext
    {
        internal string Recorded { get; set; }

        internal string ThreadId { get; set; }
        internal string Name { get; set; }

        internal string Message { get; set; }
    }
}