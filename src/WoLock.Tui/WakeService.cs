using System.Net;
using Serilog;
using WoLock.Core;
using WoLock.Core.Config;
using WoLock.Core.Logging;

namespace WoLock.Tui;

/// <summary>
/// Orchestrates loading configuration, resolving devices, and waking them.
/// </summary>
public sealed class WakeService
{
    /// <summary>Ping timeout used when no explicit timeout is given.</summary>
    public const int DefaultPingTimeoutMs = 4000;

    private readonly IWakeSender _sender;
    private readonly IPingChecker _ping;
    private readonly ILogger _logger;

    /// <summary>The resolved devices.</summary>
    public IList<Device> Devices { get; }

    /// <summary>The name of the profile the devices came from.</summary>
    public string ActiveProfileName { get; }

    /// <summary>
    /// Creates a <see cref="WakeService"/> with the given devices and dependencies.
    /// </summary>
    /// <param name="devices">The resolved devices.</param>
    /// <param name="sender">The wake sender.</param>
    /// <param name="ping">The ping checker.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="activeProfileName">The active profile name.</param>
    public WakeService(
        IList<Device> devices,
        IWakeSender sender,
        IPingChecker ping,
        ILogger logger,
        string activeProfileName)
    {
        Devices = devices;
        _sender = sender;
        _ping = ping;
        _logger = logger;
        ActiveProfileName = activeProfileName;
    }

    /// <summary>
    /// Builds a <see cref="WakeService"/> from a configuration file. When the
    /// file defines no profiles, a built-in default device (the MUNIN server)
    /// is used so the tool works out of the box.
    /// </summary>
    public static WakeService Create(
        IWakeRequestFactory factory,
        IWakeSender sender,
        IPingChecker ping,
        ILogger logger,
        string configPath)
    {
        WoLockConfig config;
        string activeProfileName;

        if (File.Exists(configPath))
        {
            config = ConfigLoader.Load(configPath);
            ProfileConfig? profile = config.Profiles
                .FirstOrDefault(p =>
                    string.Equals(p.Name, config.ActiveProfile, StringComparison.OrdinalIgnoreCase));
            if (profile is null)
            {
                profile = config.Profiles.FirstOrDefault()
                    ?? throw new InvalidOperationException(
                        $"Configuration '{configPath}' defines no profiles.");
            }

            activeProfileName = profile.Name;
        }
        else
        {
            config = new WoLockConfig();
            activeProfileName = "(default)";
        }

        IList<DeviceConfig> deviceConfigs = config.Profiles.SelectMany(p => p.Devices).ToList();
        if (deviceConfigs.Count == 0)
        {
            deviceConfigs = DefaultDevices.Build();
        }

        IList<Device> devices = deviceConfigs
            .Select((d, index) => new Device
            {
                Name = string.IsNullOrWhiteSpace(d.Name) ? $"Device{index + 1}" : d.Name!,
                Config = d,
                Request = factory.Create(d),
            })
            .ToList();

        return new WakeService(devices, sender, ping, logger, activeProfileName);
    }

    /// <summary>
    /// Finds a device by name (case-insensitive).
    /// </summary>
    /// <exception cref="InvalidOperationException">No device matches.</exception>
    public Device GetDevice(string name) =>
        Devices.FirstOrDefault(d =>
                string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Unknown device '{name}'. Run 'list' for the available devices.");

    /// <summary>
    /// Sends a wake packet for the given device and, optionally, pings the
    /// targets to confirm the device responded.
    /// </summary>
    public async Task<WakeResult> WakeAsync(
        string name, bool checkPing, int timeoutMs = DefaultPingTimeoutMs, CancellationToken cancellationToken = default)
    {
        Device device = GetDevice(name);
        try
        {
            await _sender.SendWakePacketAsync(device.Request, cancellationToken).ConfigureAwait(false);
            _logger.Information(
                "Wake packet sent to {Device} ({Mac}).", device.Name, device.Request.Mac);

            if (!checkPing)
            {
                return new WakeResult { Status = WakeStatus.PacketSent, DeviceName = device.Name };
            }

            foreach (IPEndPoint endpoint in device.Request.Targets)
            {
                if (endpoint.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork
                    || IsLoopback(endpoint.Address))
                {
                    continue;
                }

                bool responded;
                try
                {
                    responded = await _ping.PingAsync(endpoint.Address, timeoutMs, cancellationToken).ConfigureAwait(false);
                }
                catch (NotSupportedException)
                {
                    // ICMP is unavailable on this runtime; the packet was still sent.
                    _logger.Warning("Ping is not available on this runtime; reporting packet sent without confirmation.");
                    return new WakeResult { Status = WakeStatus.PacketSent, DeviceName = device.Name };
                }

                if (responded)
                {
                    _logger.Information("{Device} responded within {Timeout} ms.", device.Name, timeoutMs);
                    return new WakeResult
                    {
                        Status = WakeStatus.Responding,
                        DeviceName = device.Name,
                        TimeoutMs = timeoutMs,
                    };
                }
            }

            return new WakeResult
            {
                Status = WakeStatus.NoResponse,
                DeviceName = device.Name,
                TimeoutMs = timeoutMs,
            };
        }
        catch (WakeSendException ex)
        {
            _logger.Error(ex, "Failed to wake {Device}.", device.Name);
            return new WakeResult
            {
                Status = WakeStatus.Failed,
                DeviceName = device.Name,
                Error = ex.Message,
            };
        }
    }

    private static bool IsLoopback(System.Net.IPAddress address)
    {
        // IPv4 loopback is 127.0.0.0/8. Avoids depending on the platform's
        // IPAddress.IsLoopback (property vs. method differs across runtimes).
        byte[] bytes = address.GetAddressBytes();
        return bytes.Length == 4 && bytes[0] == 127;
    }
}

/// <summary>Built-in default devices used when no configuration is present.</summary>
public static class DefaultDevices
{
    /// <summary>
    /// Returns the default MUNIN server device from the project's starting point.
    /// </summary>
    public static IList<DeviceConfig> Build() => new List<DeviceConfig>
    {
        new DeviceConfig
        {
            Name = "MUNIN",
            Mac = "84:47:09:88:78:56",
            TargetIp = "172.19.1.255",
            AdditionalIps = new[] { "172.19.1.87", "172.19.1.86" },
        },
    };
}
