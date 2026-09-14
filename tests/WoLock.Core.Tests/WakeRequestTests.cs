using System.Net;
using WoLock.Core;

namespace WoLock.Core.Tests;

/// <summary>
/// Tests for <see cref="WakeRequest"/> defaults and validation.
/// </summary>
public class WakeRequestTests
{
    /// <summary>
    /// Broadcast defaults to true and no targets are set initially.
    /// </summary>
    [Fact]
    public void Defaults_BroadcastTrueEmptyTargets()
    {
        MacAddress mac = MacAddress.Parse("00:11:22:33:44:55");
        WakeRequest request = new WakeRequest { Mac = mac };

        Assert.True(request.Broadcast);
        Assert.Empty(request.Targets);
    }

    /// <summary>
    /// Validation passes for a request with a MAC and at least one target.
    /// </summary>
    [Fact]
    public void Validate_ValidRequest_NotThrows()
    {
        MacAddress mac = MacAddress.Parse("00:11:22:33:44:55");
        WakeRequest request = new WakeRequest
        {
            Mac = mac,
            Targets = new List<IPEndPoint> { new IPEndPoint(IPAddress.Loopback, 9) },
        };

        request.Validate();
    }

    /// <summary>
    /// Validation throws when there are no targets.
    /// </summary>
    [Fact]
    public void Validate_NoTargets_Throws()
    {
        MacAddress mac = MacAddress.Parse("00:11:22:33:44:55");
        WakeRequest request = new WakeRequest { Mac = mac };

        Assert.Throws<InvalidOperationException>(request.Validate);
    }
}
