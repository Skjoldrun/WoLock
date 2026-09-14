using System.Net;
using WoLock.Core.Network;

namespace WoLock.Core.Tests;

/// <summary>
/// Tests for the pure subnet math in <see cref="BroadcastResolver"/>.
/// </summary>
public class BroadcastResolverTests
{
    /// <summary>
    /// Computes the broadcast address for a host address and subnet mask.
    /// </summary>
    [Fact]
    public void ComputeBroadcast_172_19_1_87_24_ReturnsBroadcast()
    {
        IPAddress address = IPAddress.Parse("172.19.1.87");
        IPAddress mask = IPAddress.Parse("255.255.255.0");

        IPAddress broadcast = BroadcastResolver.ComputeBroadcast(address, mask);

        Assert.Equal("172.19.1.255", broadcast.ToString());
    }

    /// <summary>
    /// A /16 mask yields the correct broadcast address.
    /// </summary>
    [Fact]
    public void ComputeBroadcast_16BitMask_ReturnsBroadcast()
    {
        IPAddress address = IPAddress.Parse("10.0.5.20");
        IPAddress mask = IPAddress.Parse("255.255.0.0");

        IPAddress broadcast = BroadcastResolver.ComputeBroadcast(address, mask);

        Assert.Equal("10.0.255.255", broadcast.ToString());
    }

    /// <summary>
    /// Derives the subnet mask from a prefix length.
    /// </summary>
    [Theory]
    [InlineData(8, "255.0.0.0")]
    [InlineData(16, "255.255.0.0")]
    [InlineData(24, "255.255.255.0")]
    [InlineData(32, "255.255.255.255")]
    [InlineData(0, "0.0.0.0")]
    public void MaskFromPrefix_ReturnsExpectedMask(int prefix, string expected)
    {
        IPAddress mask = BroadcastResolver.MaskFromPrefix(prefix);

        Assert.Equal(expected, mask.ToString());
    }

    /// <summary>
    /// A /31 prefix is a valid mask.
    /// </summary>
    [Fact]
    public void ComputeBroadcast_31Prefix_Works()
    {
        IPAddress address = IPAddress.Parse("192.168.1.10");
        IPAddress mask = BroadcastResolver.MaskFromPrefix(31);

        IPAddress broadcast = BroadcastResolver.ComputeBroadcast(address, mask);

        Assert.Equal("192.168.1.11", broadcast.ToString());
    }

    /// <summary>
    /// Rejects a non-IPv4 address.
    /// </summary>
    [Fact]
    public void ComputeBroadcast_NonIpv4_Throws()
    {
        IPAddress address = IPAddress.Parse("::1");
        IPAddress mask = IPAddress.Parse("255.255.255.0");

        Assert.Throws<ArgumentException>(() => BroadcastResolver.ComputeBroadcast(address, mask));
    }

    /// <summary>
    /// Rejects a prefix length outside the 0..32 range.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(33)]
    public void MaskFromPrefix_OutOfRange_Throws(int prefix)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BroadcastResolver.MaskFromPrefix(prefix));
    }
}
