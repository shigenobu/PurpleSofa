using System.Net.Sockets;

namespace PurpleSofa;

/// <summary>
///     Address family.
/// </summary>
public enum PsSocketAddressFamily
{
    /// <summary>
    ///     Ipv4.
    /// </summary>
    Ipv4,

    /// <summary>
    ///     Ipv6.
    ///     If selected, v4 socket is treated as v6 socket.
    /// </summary>
    Ipv6
}

/// <summary>
///     Address family resolver.
/// </summary>
internal static class PsSocketAddressFamilyResolver
{
    /// <summary>
    ///     Resolve for 'AddressFamily' .
    /// </summary>
    /// <param name="socketAddressFamily">address family</param>
    /// <returns>AddressFamily</returns>
    internal static AddressFamily Resolve(PsSocketAddressFamily socketAddressFamily)
    {
        return socketAddressFamily switch
        {
            PsSocketAddressFamily.Ipv6 => AddressFamily.InterNetworkV6,
            _ => AddressFamily.InterNetwork
        };
    }
}