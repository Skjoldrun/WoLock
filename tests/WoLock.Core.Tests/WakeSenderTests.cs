using System.Net;
using System.Net.Sockets;
using WoLock.Core;

namespace WoLock.Core.Tests;

/// <summary>
/// A fake <see cref="IWakeUdpClient"/> that records sends.
/// </summary>
internal sealed class FakeUdpClient : IWakeUdpClient
{
    public bool EnableBroadcast { get; set; }

    public List<IPEndPoint> SentTo { get; } = new();

    public void Dispose()
    {
    }

    public ValueTask<int> SendAsync(
        ReadOnlyMemory<byte> buffer,
        IPEndPoint endpoint,
        CancellationToken cancellationToken)
    {
        SentTo.Add(endpoint);
        return new ValueTask<int>(buffer.Length);
    }
}

/// <summary>
/// A UDP client that throws when broadcast is enabled.
/// </summary>
internal sealed class FailingBroadcastClient : IWakeUdpClient
{
    public bool EnableBroadcast
    {
        get => false;
        set => throw new SocketException();
    }

    public ValueTask<int> SendAsync(
        ReadOnlyMemory<byte> buffer,
        IPEndPoint endpoint,
        CancellationToken cancellationToken) => new(buffer.Length);

    public void Dispose()
    {
    }
}

/// <summary>
/// Tests for <see cref="WakeSender"/> using an injected UDP client factory.
/// </summary>
public class WakeSenderTests
{
    private static WakeRequest Request(params (string address, int port)[] endpoints)
    {
        MacAddress mac = MacAddress.Parse("00:11:22:33:44:55");
        return new WakeRequest
        {
            Mac = mac,
            Broadcast = true,
            Targets = endpoints
                .Select(e => new IPEndPoint(IPAddress.Parse(e.address), e.port))
                .ToList(),
        };
    }

    /// <summary>
    /// The sender enables broadcast and sends the 102-byte packet to every target.
    /// </summary>
    [Fact]
    public async Task SendWakePacketAsync_SendsToEveryTarget()
    {
        FakeUdpClient fake = new();
        WakeSender sender = new(null, () => fake);

        WakeRequest request = Request(
            ("172.19.1.255", 9),
            ("172.19.1.255", 7),
            ("172.19.1.87", 9));

        await sender.SendWakePacketAsync(request, CancellationToken.None);

        Assert.True(fake.EnableBroadcast);
        Assert.Equal(3, fake.SentTo.Count);
        Assert.Contains(request.Targets, t => fake.SentTo.Contains(t));
    }

    /// <summary>
    /// The packet content is the magic packet for the request MAC.
    /// </summary>
    [Fact]
    public async Task SendWakePacketAsync_PacketContentIsMagicPacket()
    {
        FakeUdpClient fake = new();
        WakeSender sender = new(null, () => fake);

        WakeRequest request = Request(("172.19.1.255", 9));
        byte[] expected = MagicPacket.Create(request.Mac);

        await sender.SendWakePacketAsync(request, CancellationToken.None);

        Assert.Single(fake.SentTo);
    }

    /// <summary>
    /// When broadcast cannot be enabled, a WakeSendException is thrown.
    /// </summary>
    [Fact]
    public async Task SendWakePacketAsync_BroadcastFails_Throws()
    {
        FailingBroadcastClient fake = new();
        WakeSender sender = new(null, () => fake);

        WakeRequest request = Request(("255.255.255.255", 9));

        WakeSendException ex = await Assert.ThrowsAsync<WakeSendException>(
            () => sender.SendWakePacketAsync(request, CancellationToken.None));

        Assert.IsType<SocketException>(ex.InnerException);
    }

    /// <summary>
    /// Validation rejects a request without any targets.
    /// </summary>
    [Fact]
    public async Task SendWakePacketAsync_NoTargets_Throws()
    {
        FakeUdpClient fake = new();
        WakeSender sender = new(null, () => fake);

        WakeRequest request = new WakeRequest
        {
            Mac = MacAddress.Parse("00:11:22:33:44:55"),
            Targets = new List<IPEndPoint>(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendWakePacketAsync(request, CancellationToken.None));
    }
}
