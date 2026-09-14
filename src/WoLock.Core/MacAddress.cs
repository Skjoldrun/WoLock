using System.Globalization;

namespace WoLock.Core;

/// <summary>
/// Represents a 48-bit (6-byte) MAC address with parsing and validation.
/// Accepts the common <c>XX:XX:XX:XX:XX:XX</c> and <c>XX-XX-XX-XX-XX-XX</c> forms.
/// </summary>
public sealed class MacAddress : IEquatable<MacAddress>
{
    private static readonly NumberFormatInfo _hexFormat = NumberFormatInfo.InvariantInfo;

    private readonly byte[] _bytes;

    private MacAddress(byte[] bytes) => _bytes = bytes;

    /// <summary>Gets the six bytes of this MAC address in order.</summary>
    public ReadOnlySpan<byte> Bytes => _bytes;

    /// <summary>
    /// Parses and validates a MAC address.
    /// </summary>
    /// <param name="value">The MAC address string.</param>
    /// <returns>The parsed <see cref="MacAddress"/>.</returns>
    /// <exception cref="FormatException">The value is not a valid MAC address.</exception>
    public static MacAddress Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException("MAC address must not be null or whitespace.");
        }

        string cleaned = value.Replace("-", "").Replace(":", "");
        if (cleaned.Length != 12)
        {
            throw new FormatException($"Invalid MAC address '{value}': expected 6 octets (12 hex characters).");
        }

        byte[] bytes = new byte[6];
        for (int i = 0; i < bytes.Length; i++)
        {
            string octet = cleaned.Substring(i * 2, 2);
            if (!byte.TryParse(octet, NumberStyles.HexNumber, _hexFormat, out byte parsed))
            {
                throw new FormatException($"Invalid MAC address '{value}': '{octet}' is not a valid hex octet.");
            }

            bytes[i] = parsed;
        }

        return new MacAddress(bytes);
    }

    /// <inheritdoc />
    public bool Equals(MacAddress? other)
    {
        if (other is null)
        {
            return false;
        }

        return _bytes.AsSpan().SequenceEqual(other._bytes);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as MacAddress);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_bytes[0], _bytes[1], _bytes[2], _bytes[3], _bytes[4], _bytes[5]);

    /// <inheritdoc />
    public override string ToString() =>
        string.Join(":", _bytes.Select(b => b.ToString("X2", _hexFormat)));
}
