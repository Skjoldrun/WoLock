using WoLock.Core;
using WoLock.Core.Config;

namespace WoLock.Tui;

/// <summary>The outcome of a wake attempt.</summary>
public enum WakeStatus
{
    /// <summary>The magic packet was sent successfully.</summary>
    PacketSent,

    /// <summary>The device responded to the ping after the packet was sent.</summary>
    Responding,

    /// <summary>The packet was sent but the device did not respond in time.</summary>
    NoResponse,

    /// <summary>Sending the packet failed.</summary>
    Failed,
}

/// <summary>A resolved, wakeable device: its config plus the built request.</summary>
public sealed class Device
{
    /// <summary>The display name used to select the device.</summary>
    public required string Name { get; init; }

    /// <summary>The device configuration.</summary>
    public required DeviceConfig Config { get; init; }

    /// <summary>The built wake request.</summary>
    public required WakeRequest Request { get; init; }
}

/// <summary>The result of a wake attempt.</summary>
public sealed class WakeResult
{
    /// <summary>The outcome.</summary>
    public required WakeStatus Status { get; init; }

    /// <summary>The device name.</summary>
    public required string DeviceName { get; init; }

    /// <summary>An error message when <see cref="WakeStatus.Failed"/>.</summary>
    public string? Error { get; init; }

    /// <summary>The ping timeout in milliseconds, when a ping was performed.</summary>
    public int TimeoutMs { get; init; }
}
