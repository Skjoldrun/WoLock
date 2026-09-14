using System.Net;
using WoLock.Core;
using WoLock.Core.Config;

namespace WoLock.Core.Tests;

/// <summary>
/// Tests for <see cref="WakeRequestFactory"/>. Uses <c>targetIp</c> so no
/// network interface is required.
/// </summary>
public class WakeRequestFactoryTests
{
    private readonly WakeRequestFactory _factory = new();

    /// <summary>
    /// Without a configured port, both port 9 and the fallback port 7 are used.
    /// </summary>
    [Fact]
    public void Create_NoPort_UsesPorts9And7()
    {
        DeviceConfig config = new DeviceConfig
        {
            Mac = "00:11:22:33:44:55",
            TargetIp = "172.19.1.255",
        };

        WakeRequest request = _factory.Create(config);

        Assert.Equal(
            new[] { 7, 9 },
            request.Targets.Select(t => t.Port).OrderBy(p => p));
    }

    /// <summary>
    /// A configured port overrides the default ports.
    /// </summary>
    [Fact]
    public void Create_WithPort_UsesOnlyThatPort()
    {
        DeviceConfig config = new DeviceConfig
        {
            Mac = "00:11:22:33:44:55",
            TargetIp = "172.19.1.255",
            Port = 5000,
        };

        WakeRequest request = _factory.Create(config);

        Assert.Single(request.Targets);
        Assert.Equal(5000, request.Targets[0].Port);
    }

    /// <summary>
    /// Broadcast defaults to true.
    /// </summary>
    [Fact]
    public void Create_DefaultBroadcast_IsTrue()
    {
        DeviceConfig config = new DeviceConfig
        {
            Mac = "00:11:22:33:44:55",
            TargetIp = "172.19.1.255",
        };

        Assert.True(_factory.Create(config).Broadcast);
    }

    /// <summary>
    /// A configured broadcast value overrides the default.
    /// </summary>
    [Fact]
    public void Create_WithBroadcastFalse_OverridesDefault()
    {
        DeviceConfig config = new DeviceConfig
        {
            Mac = "00:11:22:33:44:55",
            TargetIp = "172.19.1.255",
            Broadcast = false,
        };

        Assert.False(_factory.Create(config).Broadcast);
    }

    /// <summary>
    /// The configured NIC is passed through to the request.
    /// </summary>
    [Fact]
    public void Create_WithNic_PassesThroughNicName()
    {
        DeviceConfig config = new DeviceConfig
        {
            Mac = "00:11:22:33:44:55",
            TargetIp = "172.19.1.255",
            Nic = "eth0",
        };

        Assert.Equal("eth0", _factory.Create(config).NicName);
    }

    /// <summary>
    /// Additional IPs are added alongside the configured target IP.
    /// </summary>
    [Fact]
    public void Create_WithAdditionalIps_AddsAllAddresses()
    {
        DeviceConfig config = new DeviceConfig
        {
            Mac = "00:11:22:33:44:55",
            TargetIp = "172.19.1.255",
            AdditionalIps = new[] { "172.19.1.87", "172.19.1.86" },
        };

        WakeRequest request = _factory.Create(config);

        // Each address is sent on both default ports (9 and 7), so compare the
        // distinct set of addresses.
        Assert.Equal(
            new[] { "172.19.1.255", "172.19.1.87", "172.19.1.86" },
            request.Targets.Select(t => t.Address.ToString()).Distinct().ToArray());
    }

    /// <summary>
    /// An invalid MAC throws a FormatException.
    /// </summary>
    [Fact]
    public void Create_InvalidMac_Throws()
    {
        DeviceConfig config = new DeviceConfig { Mac = "not-a-mac", TargetIp = "172.19.1.255" };

        Assert.Throws<FormatException>(() => _factory.Create(config));
    }

    /// <summary>
    /// A null config throws.
    /// </summary>
    [Fact]
    public void Create_NullConfig_Throws()
    {
        DeviceConfig? config = null;
        Assert.Throws<ArgumentNullException>(() => _factory.Create(config!));
    }
}
