namespace PurpleSofa
{
    /// <summary>
    ///     State read.
    /// </summary>
    internal class PsStateRead : PsState
    {
        /// <summary>
        ///     Buffer by read.
        /// </summary>
        internal byte[]? Buffer { get; set; }
        
        /// <summary>
        ///     Close reason.
        /// </summary>
        internal PsCloseReason CloseReason { get; set; }
        
        /// <summary>
        ///     To String.
        /// </summary>
        /// <returns>socket remote endpoint, close reason</returns>
        public override string ToString()
        {
            return $"Socket: {Socket.PxSocketRemoteEndPoint()}, CloseReason: {CloseReason}";
        }
    }
}