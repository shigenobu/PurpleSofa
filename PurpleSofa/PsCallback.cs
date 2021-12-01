namespace PurpleSofa
{
    public abstract class PsCallback
    {
        public virtual void OnOpen(PsSession session)
        {
        }

        public abstract void OnMessage(PsSession session, byte[] message);

        public virtual void OnClose(PsSession session, PsCloseReason closeReason)
        {
        }
    }
}