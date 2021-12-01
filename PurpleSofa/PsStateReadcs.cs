using System.Net.Sockets;

namespace PurpleSofa
{
    public class PsStateRead : PsState
    {
        public byte[]? Buffer { get; set; }
        
        public PsCloseReason CloseReason { get; set; }
        
        public override string ToString()
        {
            return $"Socket: {Socket.PxSocketRemoteEndPoint()}, CloseReason: {CloseReason}";
        }
    }
}