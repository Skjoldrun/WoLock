using System.Net;

namespace WoLock.Core;

/// <summary>
/// Describes a single wake operation: which MAC to wake, to which
/// IP/port combinations to send, and how to send it.
/// </summary>
public sealed class WakeRequest
{
    /// <summary>The target MAC address (required).</summary>
    public required MacAddress Mac { get; init; }

    /// <summary>The destination endpoints. Supports multiple IPs and ports.</summary>
    public IList<IPEndPoint> Targets { get; init; } = new List<IPEndPoint>();

    /// <summary>Whether to enable UDP broadcast. Defaults to <c>true</c>.</summary>
    public bool Broadcast { get; init; } = true;

    /// <summary>Optional network interface name to send through.</summary>
    public string? NicName { get; init; }

    /// <summary>
    /// Validates the request, ensuring a MAC and at least one target are present.
    /// </summary>
    /// <exception cref="InvalidOperationException">The request is incomplete.</exception>
    public void Validate()
    {
        if (Mac is null)
        {
            throw new InvalidOperationException("A wake request requires a MAC address.");
        }

        if (Targets is null || Targets.Count == 0)
        {
            throw new InvalidOperationException("A wake request requires at least one target endpoint.");
        }
    }
}
