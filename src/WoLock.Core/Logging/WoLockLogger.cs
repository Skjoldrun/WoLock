using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace WoLock.Core.Logging;

/// <summary>
/// Central Serilog configuration for WoLock apps. Defaults to DEBUG/INFO,
/// console output, and an optional file sink (non-persistent by default).
/// </summary>
public static class WoLockLogger
{
    /// <summary>
    /// Builds and installs the global Serilog logger.
    /// </summary>
    /// <param name="enableFile">
    /// When <c>true</c>, also writes logs to a file in <paramref name="logDirectory"/>.
    /// </param>
    /// <param name="logDirectory">
    /// Directory for the file sink. Required when <paramref name="enableFile"/> is <c>true</c>.
    /// </param>
    /// <param name="minimumLevel">The minimum log level. Defaults to <see cref="LogEventLevel.Debug"/>.</param>
    /// <returns>The configured <see cref="LoggerConfiguration"/> for further customization.</returns>
    public static LoggerConfiguration Configure(
        bool enableFile = false,
        string? logDirectory = null,
        LogEventLevel minimumLevel = LogEventLevel.Debug)
    {
        LoggerConfiguration configuration = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}");

        if (enableFile)
        {
            ArgumentNullException.ThrowIfNull(logDirectory);
            configuration = configuration.WriteTo.File(
                Path.Combine(logDirectory, "wolock-{Date}.log"),
                rollingInterval: RollingInterval.Day);
        }

        Log.Logger = configuration.CreateLogger();
        return configuration;
    }
}
