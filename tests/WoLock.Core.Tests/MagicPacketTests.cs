using WoLock.Core;

namespace WoLock.Core.Tests;

/// <summary>
/// Tests for <see cref="MagicPacket"/> layout.
/// </summary>
public class MagicPacketTests
{
    /// <summary>
    /// The magic packet is exactly 102 bytes: 6 sync bytes + 16 × MAC.
    /// </summary>
    [Fact]
    public void Size_Is102Bytes()
    {
        Assert.Equal(102, MagicPacket.Size);
    }

    /// <summary>
    /// The first six bytes are 0xFF.
    /// </summary>
    [Fact]
    public void StartsWithSixSyncBytes()
    {
        MacAddress mac = MacAddress.Parse("00:11:22:33:44:55");
        byte[] packet = MagicPacket.Create(mac);

        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(0xFF, packet[i]);
        }
    }

    /// <summary>
    /// The MAC address is repeated sixteen times starting at byte 6.
    /// </summary>
    [Fact]
    public void MacRepeatedSixteenTimes()
    {
        MacAddress mac = MacAddress.Parse("00:11:22:33:44:55");
        byte[] packet = MagicPacket.Create(mac);

        byte[] expected = mac.Bytes.ToArray();
        for (int roll = 0; roll < 16; roll++)
        {
            byte[] slice = packet.AsSpan(6 + roll * 6, 6).ToArray();
            Assert.Equal(expected, slice);
        }
    }

    /// <summary>
    /// The full payload equals sync bytes followed by the MAC sixteen times.
    /// </summary>
    [Fact]
    public void FullPayloadMatchesExpectedLayout()
    {
        MacAddress mac = MacAddress.Parse("84:47:09:88:78:56");
        byte[] packet = MagicPacket.Create(mac);
        byte[] expected = new byte[102];

        for (int i = 0; i < 6; i++)
        {
            expected[i] = 0xFF;
        }

        for (int roll = 0; roll < 16; roll++)
        {
            mac.Bytes.CopyTo(expected.AsSpan(6 + roll * 6));
        }

        Assert.Equal(expected, packet);
    }

    /// <summary>
    /// Passing null throws.
    /// </summary>
    [Fact]
    public void Create_NullMac_Throws()
    {
        MacAddress? mac = null;
        Assert.Throws<ArgumentNullException>(() => MagicPacket.Create(mac!));
    }
}
