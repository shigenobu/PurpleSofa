namespace PurpleSofa
{
    /// <summary>
    ///     Callback.
    /// </summary>
    public abstract class PsCallback
    {
        /// <summary>
        ///     Open handler.
        /// </summary>
        /// <param name="session">session</param>
        public virtual void OnOpen(PsSession session)
        {
        }

        /// <summary>
        ///     Message handler.
        /// </summary>
        /// <param name="session">session</param>
        /// <param name="message">message</param>
        public abstract void OnMessage(PsSession session, byte[] message);

        /// <summary>
        ///     Close handler.
        /// </summary>
        /// <param name="session">session</param>
        /// <param name="closeReason">close reason</param>
        public virtual void OnClose(PsSession session, PsCloseReason closeReason)
        {
        }
    }
}