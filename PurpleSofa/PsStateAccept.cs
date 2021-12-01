using System.Net.Sockets;

namespace PurpleSofa
{
    /// <summary>
    ///     State accept.
    /// </summary>
    public class PsStateAccept : PsState
    {
        /// <summary>
        ///     To String.
        /// </summary>
        /// <returns>socket remote endpoint</returns>
        public override string ToString()
        {
            return $"Socket: {Socket.PxSocketRemoteEndPoint()}";
        }
    }
}