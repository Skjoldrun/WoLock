using Serilog;
using WoLock.Core;
using WoLock.Core.Logging;
using WoLock.Tui.Interactive;

namespace WoLock.Tui;

/// <summary>
/// Entry point for the WoLock TUI: parses command-line arguments, configures
/// logging, and dispatches to the list, wake, or interactive commands.
/// </summary>
public static class Program
{
    /// <summary>The application entry point.</summary>
    public static async Task<int> Main(string[] args)
    {
        Command command = Command.Interactive;
        string? device = null;
        string configPath = ResolveConfigPath();
        bool checkPing = false;
        int timeoutMs = WakeService.DefaultPingTimeoutMs;

        try
        {
            (command, device) = ArgParser.Parse(args, out configPath, out checkPing, out timeoutMs, out bool showHelp);
            if (showHelp)
            {
                PrintUsage();
                return 0;
            }

            WoLockLogger.Configure(enableFile: false);

            IWakeSender sender = new WakeSender();
            IPingChecker ping = new PingChecker();
            IWakeRequestFactory factory = new WakeRequestFactory();

            WakeService service = WakeService.Create(factory, sender, ping, Log.Logger, configPath);

            switch (command)
            {
                case Command.List:
                    PrintList(service);
                    return 0;

                case Command.Wake:
                    return await WakeOne(service, device!, checkPing, timeoutMs, CancellationToken.None)
                        .ConfigureAwait(false);

                case Command.Interactive:
                    return TuiApp.Run(service);

                default:
                    PrintUsage();
                    return 1;
            }
        }
        catch (ArgParseException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            PrintUsage();
            return 2;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Unexpected error.");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> WakeOne(
        WakeService service, string device, bool checkPing, int timeoutMs, CancellationToken ct)
    {
        WakeResult result = await service.WakeAsync(device, checkPing, timeoutMs, ct).ConfigureAwait(false);
        PrintResult(result);
        return result.Status == WakeStatus.Responding || result.Status == WakeStatus.PacketSent ? 0 : 1;
    }

    private static void PrintList(WakeService service)
    {
        Console.WriteLine($"Profile: {service.ActiveProfileName}");
        Console.WriteLine();
        foreach (Device device in service.Devices)
        {
            Console.WriteLine($"  {device.Name,-12} MAC {device.Request.Mac}");
        }
    }

    private static void PrintResult(WakeResult result)
    {
        string message = result.Status switch
        {
            WakeStatus.PacketSent => $"{result.DeviceName}: packet sent.",
            WakeStatus.Responding => $"{result.DeviceName}: device responded.",
            WakeStatus.NoResponse => $"{result.DeviceName}: no response.",
            WakeStatus.Failed => $"{result.DeviceName}: failed. {result.Error}",
            _ => result.DeviceName,
        };

        bool success = result.Status is WakeStatus.Responding or WakeStatus.PacketSent;
        Console.WriteLine(success ? message : $"{message} [exit {(int)result.Status}]");
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            WoLock - Wake-on-LAN

            Usage:
              WoLock.Tui [command] [options]

            Commands:
              list                  List all devices in the active profile.
              wake <device>         Wake a device by name.
              interactive           Start the interactive TUI (default).

            Options:
              --config <path>       Path to appsettings.json.
              --ping                Ping the device to confirm it responded.
              --timeout <ms>        Ping timeout in ms (default 4000).
              -h, --help            Show this help.
            """);
    }

    private static string ResolveConfigPath()
    {
        if (Environment.GetEnvironmentVariable("WOLOCK_CONFIG") is { } env)
        {
            return env;
        }

        string candidate = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        return File.Exists(candidate) ? candidate : "appsettings.json";
    }
}
