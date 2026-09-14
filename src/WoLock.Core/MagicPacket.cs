namespace WoLock.Core;

/// <summary>
/// Builds the Wake-on-LAN "magic packet": six <c>0xFF</c> bytes followed by the
/// target MAC address repeated sixteen times (6 + 16 * 6 = 102 bytes).
/// </summary>
public static class MagicPacket
{
    /// <summary>The fixed size of a magic packet in bytes.</summary>
    public const int Size = 102;

    /// <summary>
    /// Builds the magic packet for the given MAC address.
    /// </summary>
    /// <param name="mac">The target MAC address.</param>
    /// <returns>A new 102-byte packet.</returns>
    public static byte[] Create(MacAddress mac)
    {
        ArgumentNullException.ThrowIfNull(mac);

        byte[] payload = new byte[Size];
        for (int i = 0; i < 6; i++)
        {
            payload[i] = 0xFF;
        }

        ReadOnlySpan<byte> target = mac.Bytes;
        for (int roll = 0; roll < 16; roll++)
        {
            target.CopyTo(payload.AsSpan(6 + roll * 6));
        }

        return payload;
    }
}
