using System.Text.Json;
using WoLock.Core.Config;

namespace WoLock.Core.Tests;

/// <summary>
/// Tests for <see cref="ConfigLoader"/>.
/// </summary>
public class ConfigLoaderTests
{
    /// <summary>
    /// Deserializes profiles, devices and the active profile from JSON.
    /// </summary>
    [Fact]
    public void Deserialize_ParsesProfilesDevicesAndActiveProfile()
    {
        string json = """
        {
            // a comment
            "profiles": [
                {
                    "name": "Office",
                    "devices": [
                        { "name": "MUNIN", "mac": "84:47:09:88:78:56", "targetIp": "172.19.1.255", "port": 9 }
                    ]
                }
            ],
            "activeProfile": "Office"
        }
        """;

        WoLockConfig config = ConfigLoader.Deserialize(json);

        Assert.Single(config.Profiles);
        ProfileConfig profile = config.Profiles[0];
        Assert.Equal("Office", profile.Name);
        Assert.Single(profile.Devices);

        DeviceConfig device = profile.Devices[0];
        Assert.Equal("MUNIN", device.Name);
        Assert.Equal("84:47:09:88:78:56", device.Mac);
        Assert.Equal("172.19.1.255", device.TargetIp);
        Assert.Equal(9, device.Port);

        Assert.Equal("Office", config.ActiveProfile);
    }

    /// <summary>
    /// Missing values default sensibly (empty device list, no active profile).
    /// </summary>
    [Fact]
    public void Deserialize_EmblemConfig_UsesDefaults()
    {
        WoLockConfig config = ConfigLoader.Deserialize("{}");

        Assert.Empty(config.Profiles);
        Assert.Null(config.ActiveProfile);
    }

    /// <summary>
    /// Loading a non-existent file throws a FileNotFoundException.
    /// </summary>
    [Fact]
    public void Load_MissingFile_Throws()
    {
        string path = Path.Combine(Path.GetTempPath(), "wolock-missing-abcdef.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        Assert.Throws<FileNotFoundException>(() => ConfigLoader.Load(path));
    }

    /// <summary>
    /// Invalid JSON throws a JsonException.
    /// </summary>
    [Fact]
    public void Deserialize_InvalidJson_Throws()
    {
        Assert.Throws<JsonException>(() => ConfigLoader.Deserialize("{ not json }"));
    }
}
