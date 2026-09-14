using System.Net;
using WoLock.Core.Config;
using WoLock.Core.Network;

namespace WoLock.Core;

/// <summary>
/// Builds <see cref="WakeRequest"/> objects from <see cref="DeviceConfig"/>
/// entries, filling missing values with smart-guess defaults.
/// </summary>
public interface IWakeRequestFactory
{
    /// <summary>
    /// Creates a wake request for the given device configuration.
    /// </summary>
    /// <param name="config">The device configuration.</param>
    /// <returns>The built wake request.</returns>
    WakeRequest Create(DeviceConfig config);
}

/// <summary>Default <see cref="IWakeRequestFactory"/> implementation.</summary>
public class WakeRequestFactory : IWakeRequestFactory
{
    /// <summary>Default UDP port when none is configured.</summary>
    public const int DefaultPort = 9;

    /// <summary>Fallback UDP port when none is configured.</summary>
    public const int FallbackPort = 7;

    /// <inheritdoc />
    public WakeRequest Create(DeviceConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        MacAddress mac = MacAddress.Parse(config.Mac ?? string.Empty);
        bool broadcast = config.Broadcast ?? true;

        List<int> ports = config.Port is { } configuredPort
            ? new() { configuredPort }
            : new() { DefaultPort, FallbackPort };

        IEnumerable<IPAddress> addresses = ResolveAddresses(config);

        List<IPEndPoint> targets = new();
        foreach (IPAddress address in addresses)
        {
            foreach (int port in ports)
            {
                targets.Add(new IPEndPoint(address, port));
            }
        }

        return new WakeRequest
        {
            Mac = mac,
            Targets = targets,
            Broadcast = broadcast,
            NicName = config.Nic,
        };
    }

    private static IEnumerable<IPAddress> ResolveAddresses(DeviceConfig config)
    {
        IEnumerable<IPAddress> addresses;
        if (!string.IsNullOrWhiteSpace(config.TargetIp))
        {
            addresses = new[] { IPAddress.Parse(config.TargetIp!) };
        }
        else
        {
            // Smart-guess: derive broadcast + local unicast from the active NIC.
            (IPAddress localAddress, IPAddress subnet) = BroadcastResolver.ResolvePrimaryNic();
            IPAddress broadcastAddress = BroadcastResolver.ComputeBroadcast(localAddress, subnet);
            addresses = new List<IPAddress> { broadcastAddress, localAddress };
        }

        List<IPAddress> result = addresses.ToList();
        if (config.AdditionalIps is { } extras)
        {
            foreach (string extra in extras)
            {
                result.Add(IPAddress.Parse(extra));
            }
        }

        return result;
    }
}
