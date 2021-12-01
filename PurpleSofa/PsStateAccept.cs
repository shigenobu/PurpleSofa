using System.Net.Sockets;

namespace PurpleSofa
{
    public class PsStateAccept : PsState
    {
        public override string ToString()
        {
            return $"Socket: {Socket.PxSocketRemoteEndPoint()}";
        }
    }
}