using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PurpleSofa;

/// <summary>
///     Network.
/// </summary>
public static class PsNetwork
{
    /// <summary>
    ///     Network interfaces.
    /// </summary>
    private static readonly IEnumerable<NetworkInterface> NetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
        .Where(i =>
            i is {OperationalStatus: OperationalStatus.Up, NetworkInterfaceType: NetworkInterfaceType.Ethernet} &&
            i.NetworkInterfaceType != NetworkInterfaceType.Loopback);

    /// <summary>
    ///     Get local ipv4 addresses.
    /// </summary>
    /// <returns>ipv4 addresses</returns>
    public static IEnumerable<IPAddress> GetLocalIpv4Addresses()
    {
        List<IPAddress> addresses = new();
        foreach (var networkInterface in NetworkInterfaces)
        {
            var address = networkInterface.GetIPProperties().UnicastAddresses
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address)
                .FirstOrDefault();
            if (address != null)
                addresses.Add(address);
        }

        return addresses;
    }

    /// <summary>
    ///     Get local ipv6 addresses.
    /// </summary>
    /// <returns>ipv6 addresses</returns>
    public static IEnumerable<IPAddress> GetLocalIpv6Addresses()
    {
        List<IPAddress> addresses = new();
        foreach (var networkInterface in NetworkInterfaces)
        {
            var address = networkInterface.GetIPProperties().UnicastAddresses
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)
                .Select(a => a.Address)
                .FirstOrDefault();
            if (address != null)
                addresses.Add(address);
        }

        return addresses;
    }
}