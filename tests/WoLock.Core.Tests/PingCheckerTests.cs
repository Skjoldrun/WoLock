using System.Net;
using WoLock.Core;

namespace WoLock.Core.Tests;

/// <summary>
/// Tests for <see cref="PingChecker"/> that do not require real ICMP traffic.
/// </summary>
public class PingCheckerTests
{
    /// <summary>
    /// IPv6 addresses are rejected before any ICMP is attempted.
    /// </summary>
    [Fact]
    public async Task PingAsync_NonIpv4_Throws()
    {
        PingChecker ping = new();

        await Assert.ThrowsAsync<ArgumentException>(
            () => ping.PingAsync(IPAddress.Parse("::1"), 1000, CancellationToken.None));
    }
}
