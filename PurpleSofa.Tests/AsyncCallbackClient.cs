namespace PurpleSofa.Tests;

public class AsyncCallbackClient : PsCallback
{
    private const string Key = "inc";

    public override Task OnOpenAsync(PsSession session)
    {
        session.ChangeIdleMilliSeconds(1000);
        session.SetValue(Key, 0);
        // session.Send($"Hello {session.LocalEndPoint}.".PxToBytes());
        return Task.CompletedTask;
    }

    public override async Task OnMessageAsync(PsSession session, byte[] message)
    {
        PsLogger.Info($"Receive from server: '{message.PxToString()}' ({session}).");

        var inc = session.GetValue<int>(Key);
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