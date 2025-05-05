namespace PurpleSofa.Tests;

public class AsyncCallbackServer : PsCallback
{
    private const string Key = "inc";

    private readonly PsLock _lock = new();

    private readonly List<PsSession> _sessions = new();

    public override async Task OnOpenAsync(PsSession session)
    {
        session.ChangeIdleMilliSeconds(1000);
        using (await _lock.LockAsync())
        {
            _sessions.Add(session);

            session.SetValue(Key, 0);
            foreach (var s in _sessions)
                // s.Send($"Hello {s.RemoteEndPoint}.".PxToBytes());    
                await s.SendAsync($"Hello num:{_sessions.Count}.".PxToBytes());
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

            var inc = session.GetValue<int>(Key);
            inc++;
            session.SetValue(Key, inc);

            var reply = $"s{inc}";
            // PsLogger.Info($"Receive from client: '{message.PxToString()}' ({session}) last:{PsDate.Now()}.");
            foreach (var s in _sessions)
                // s.Send(reply.PxToBytes());    
                await s.SendAsync(message);
        }
    }

    public override async Task OnCloseAsync(PsSession session, PsCloseReason closeReason)
    {
        PsLogger.Info($"Goodby {session.RemoteEndPoint} for {closeReason} at server.");
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