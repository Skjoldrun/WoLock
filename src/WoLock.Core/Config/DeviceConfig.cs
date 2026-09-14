using System.Text.Json.Serialization;

namespace WoLock.Core.Config;

/// <summary>
/// Configuration for a single wakeable device. Every field is optional except
/// <see cref="Mac"/>; missing values are filled in by smart-guess.
/// </summary>
public sealed class DeviceConfig
{
    /// <summary>A human-readable name used to select the device.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>The target MAC address (required).</summary>
    [JsonPropertyName("mac")]
    public string? Mac { get; init; }

    /// <summary>
    /// Override for the target/broadcast IP. When omitted, the broadcast
    /// address is derived from the active NIC (smart-guess).
    /// </summary>
    [JsonPropertyName("targetIp")]
    public string? TargetIp { get; init; }

    /// <summary>
    /// UDP port. When omitted, ports 9 and 7 are both used (fallback 7).
    /// </summary>
    [JsonPropertyName("port")]
    public int? Port { get; init; }

    /// <summary>Override for the network interface name to send through.</summary>
    [JsonPropertyName("nic")]
    public string? Nic { get; init; }

    /// <summary>Override for the broadcast flag. Defaults to <c>true</c>.</summary>
    [JsonPropertyName("broadcast")]
    public bool? Broadcast { get; init; }

    /// <summary>
    /// Additional unicast IPs to send to as well. When omitted, the local
    /// NIC unicast address is added alongside the broadcast address.
    /// </summary>
    [JsonPropertyName("additionalIps")]
    public IList<string>? AdditionalIps { get; init; }
}

/// <summary>A named group of devices that can be selected as a profile.</summary>
public sealed class ProfileConfig
{
    /// <summary>The profile name used to select it.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The devices contained in this profile.</summary>
    [JsonPropertyName("devices")]
    public IList<DeviceConfig> Devices { get; init; } = new List<DeviceConfig>();
}

/// <summary>Root configuration loaded from <c>appsettings.json</c>.</summary>
public sealed class WoLockConfig
{
    /// <summary>The available profiles.</summary>
    [JsonPropertyName("profiles")]
    public IList<ProfileConfig> Profiles { get; init; } = new List<ProfileConfig>();

    /// <summary>
    /// The name of the active profile. When null or unknown, the detected
    /// network profile is used.
    /// </summary>
    [JsonPropertyName("activeProfile")]
    public string? ActiveProfile { get; init; }
}
