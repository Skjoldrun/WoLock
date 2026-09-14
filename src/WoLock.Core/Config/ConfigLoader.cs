using System.Text.Json;

namespace WoLock.Core.Config;

/// <summary>
/// Loads <see cref="WoLockConfig"/> from an appsettings.json file.
/// </summary>
public static class ConfigLoader
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Loads the configuration from the given file path.
    /// </summary>
    /// <param name="path">Path to the JSON configuration file.</param>
    /// <returns>The parsed configuration.</returns>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="JsonException">The file is not valid JSON.</exception>
    public static WoLockConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Configuration file not found: {path}");
        }

        string json = File.ReadAllText(path);
        return Deserialize(json);
    }

    /// <summary>
    /// Loads the configuration from the given JSON string.
    /// </summary>
    public static WoLockConfig Deserialize(string json) =>
        JsonSerializer.Deserialize<WoLockConfig>(json, _options)
        ?? throw new JsonException("Configuration deserialized to null.");

    /// <summary>
    /// Returns the default configuration file name.
    /// </summary>
    public static string DefaultFileName => "appsettings.json";
}
