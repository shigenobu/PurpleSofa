using System.Net.Sockets;

namespace PurpleSofa
{
    /// <summary>
    ///     State.
    /// </summary>
    public class PsState
    {
        /// <summary>
        ///     Socket.
        /// </summary>
        public Socket Socket { get; init; } = null!;
    }
}