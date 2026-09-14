using WoLock.Core;

namespace WoLock.Core.Tests;

/// <summary>
/// Tests for <see cref="MacAddress"/> parsing, formatting and equality.
/// </summary>
public class MacAddressTests
{
    /// <summary>
    /// Parses the common colon-separated form and exposes the six bytes.
    /// </summary>
    [Fact]
    public void Parse_ColonSeparatedForm_ReturnsBytes()
    {
        MacAddress mac = MacAddress.Parse("84:47:09:88:78:56");

        Assert.Equal(
            new byte[] { 0x84, 0x47, 0x09, 0x88, 0x78, 0x56 },
            mac.Bytes.ToArray());
    }

    /// <summary>
    /// Parses the dash-separated form identically to the colon form.
    /// </summary>
    [Fact]
    public void Parse_DashSeparatedForm_ReturnsSameBytes()
    {
        MacAddress colon = MacAddress.Parse("84:47:09:88:78:56");
        MacAddress dash = MacAddress.Parse("84-47-09-88-78-56");

        Assert.Equal(colon, dash);
    }

    /// <summary>
    /// Accepts lowercase hex characters.
    /// </summary>
    [Fact]
    public void Parse_LowercaseHex_IsAccepted()
    {
        MacAddress mac = MacAddress.Parse("84:47:09:88:78:56");
        MacAddress lower = MacAddress.Parse("84:47:09:88:78:56".ToLowerInvariant());

        Assert.Equal(mac, lower);
    }

    /// <summary>
    /// Formats the address back to uppercase colon-separated form.
    /// </summary>
    [Fact]
    public void ToString_FormatsUppercaseColonSeparated()
    {
        MacAddress mac = MacAddress.Parse("84:47:09:88:78:56");

        Assert.Equal("84:47:09:88:78:56", mac.ToString());
    }

    /// <summary>
    /// Two addresses with the same bytes are equal.
    /// </summary>
    [Fact]
    public void Equals_SameBytes_AreEqual()
    {
        MacAddress a = MacAddress.Parse("00:11:22:33:44:55");
        MacAddress b = MacAddress.Parse("00-11-22-33-44-55");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Addresses with different bytes are not equal.
    /// </summary>
    [Fact]
    public void Equals_DifferentBytes_AreNotEqual()
    {
        MacAddress a = MacAddress.Parse("00:11:22:33:44:55");
        MacAddress b = MacAddress.Parse("00:11:22:33:44:56");

        Assert.NotEqual(a, b);
    }

    /// <summary>
    /// Rejects null or whitespace input.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_Throws(string value)
    {
        Assert.Throws<FormatException>(() => MacAddress.Parse(value));
    }

    /// <summary>
    /// Rejects a value with the wrong number of hex characters.
    /// </summary>
    [Theory]
    [InlineData("84:47:09:88:78")]
    [InlineData("84:47:09:88:78:56:00")]
    [InlineData("8447098878")]
    public void Parse_WrongLength_Throws(string value)
    {
        Assert.Throws<FormatException>(() => MacAddress.Parse(value));
    }

    /// <summary>
    /// Rejects non-hex octets.
    /// </summary>
    [Fact]
    public void Parse_NonHexOctet_Throws()
    {
        Assert.Throws<FormatException>(() => MacAddress.Parse("84:47:09:88:78:ZZ"));
    }
}
