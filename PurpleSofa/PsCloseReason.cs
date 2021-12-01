namespace PurpleSofa
{
    public enum PsCloseReason
    {
        None,

        PeerClose,

        SelfClose,

        Failed,
        
        Timeout,

        Shutdown
    }
}