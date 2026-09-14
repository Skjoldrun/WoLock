using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;

namespace WoLock.Core.Network;

/// <summary>
/// Smart-guess helpers: derive the broadcast address from the active NIC and
/// compute it from a local address plus subnet mask.
/// </summary>
public static class BroadcastResolver
{
    /// <summary>
    /// Computes the broadcast address for an IPv4 address and subnet mask.
    /// broadcast = (address AND mask) OR (NOT mask).
    /// </summary>
    /// <param name="address">The IPv4 host address.</param>
    /// <param name="subnetMask">The IPv4 subnet mask.</param>
    /// <returns>The broadcast address.</returns>
    /// <exception cref="ArgumentException">The address or mask is not IPv4.</exception>
    public static IPAddress ComputeBroadcast(IPAddress address, IPAddress subnetMask)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ArgumentException("Only IPv4 addresses are supported.", nameof(address));
        }

        if (subnetMask.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ArgumentException("Only IPv4 subnet masks are supported.", nameof(subnetMask));
        }

        byte[] a = address.GetAddressBytes();
        byte[] m = subnetMask.GetAddressBytes();
        byte[] broadcast = new byte[4];
        for (int i = 0; i < broadcast.Length; i++)
        {
            broadcast[i] = (byte)(a[i] | ~m[i]);
        }

        return new IPAddress(broadcast);
    }

    /// <summary>
    /// Derives the IPv4 subnet mask from a prefix length (0..32).
    /// </summary>
    public static IPAddress MaskFromPrefix(int prefix)
    {
        if (prefix < 0 || prefix > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(prefix), prefix, "Prefix length must be between 0 and 32.");
        }

        // Shift by 32 wraps to 0 in C#, so handle the 0 and 32 extremes
        // explicitly.
        uint mask = prefix switch
        {
            0 => 0u,
            32 => 0xFFFFFFFFu,
            _ => 0xFFFFFFFFu << (32 - prefix),
        };
        return new IPAddress(new[]
        {
            (byte)(mask >> 24),
            (byte)(mask >> 16),
            (byte)(mask >> 8),
            (byte)mask,
        });
    }

    /// <summary>
    /// Enumerates network interfaces, supporting both the stock
    /// <c>NetworkInterface.GetInterfaces()</c> API and builds that expose
    /// <c>NetworkInterface.GetAllNetworkInterfaces()</c>.
    /// </summary>
    private static IEnumerable<NetworkInterface> EnumerateInterfaces()
    {
        Type type = typeof(NetworkInterface);
        MethodInfo? method = type.GetMethod(
            "GetInterfaces",
            BindingFlags.Public | BindingFlags.Static)
            ?? type.GetMethod(
                "GetAllNetworkInterfaces",
                BindingFlags.Public | BindingFlags.Static);

        if (method is null)
        {
            throw new NotSupportedException(
                "No NetworkInterface enumeration method (GetInterfaces/GetAllNetworkInterfaces) is available on this runtime.");
        }

        return (IEnumerable<NetworkInterface>)method.Invoke(null, null)!;
    }

    /// <summary>
    /// Returns all active Ethernet interfaces.
    /// </summary>
    public static IList<NetworkInterface> GetActiveEthernetInterfaces() =>
        EnumerateInterfaces()
            .Where(ni =>
                ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                && ni.OperationalStatus == OperationalStatus.Up)
            .ToList();

    /// <summary>
    /// Resolves the local IPv4 address and subnet mask of the primary active
    /// Ethernet interface (preferring the first non-loopback interface).
    /// </summary>
    /// <exception cref="InvalidOperationException">No suitable interface was found.</exception>
    public static (IPAddress LocalAddress, IPAddress SubnetMask) ResolvePrimaryNic()
    {
        NetworkInterface? ni = GetActiveEthernetInterfaces().FirstOrDefault(n =>
            n.GetIPProperties().UnicastAddresses.Any(u => u.Address.AddressFamily == AddressFamily.InterNetwork));

        if (ni is null)
        {
            throw new InvalidOperationException("No active Ethernet interface with an IPv4 address was found.");
        }

        UnicastIPAddressInformation unicast = ni.GetIPProperties()
            .UnicastAddresses
            .First(u => u.Address.AddressFamily == AddressFamily.InterNetwork);

        IPAddress mask = MaskFromPrefix(unicast.PrefixLength);
        return (unicast.Address, mask);
    }
}

