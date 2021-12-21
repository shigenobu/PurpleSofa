namespace PurpleSofa
{
    /// <summary>
    ///     State connect.
    /// </summary>
    internal class PsStateConnect : PsState
    {
        /// <summary>
        ///     To String.
        /// </summary>
        /// <returns>socket local endpoint</returns>
        public override string ToString()
        {
            return $"Socket: {Socket.PxSocketLocalEndPoint()}";
        }
    }
}