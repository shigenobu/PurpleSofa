using System;
using System.Threading;

namespace PurpleSofa
{
    public class PsShutdown
    {
        private readonly PsShutdownExecutor _executor;

        private volatile bool _inShutdown;
        
        public PsShutdown(PsShutdownExecutor executor)
        {
            _executor = executor;
        }

        public bool InShutdown()
        {
            return _inShutdown;
        }

        public void Shutdown(object? sender, EventArgs args)
        {
            // start shutdown
            PsLogger.Info("Start shutdown.");
            _inShutdown = true;
            
            // sleep
            try
            {
                Thread.Sleep(1000);
            }
            catch (Exception e)
            {
                PsLogger.Error(e);
            }
            
            // execute
            _executor.Execute();
            
            // end shutdown
            PsLogger.Info("End shutdown.");
        }
    }
}