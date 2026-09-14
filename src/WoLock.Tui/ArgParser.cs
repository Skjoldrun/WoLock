namespace WoLock.Tui;

/// <summary>The top-level command selected on the command line.</summary>
public enum Command
{
    /// <summary>Start the interactive Terminal.Gui UI.</summary>
    Interactive,

    /// <summary>List all devices.</summary>
    List,

    /// <summary>Wake a single device.</summary>
    Wake,
}

/// <summary>Thrown when command-line arguments cannot be parsed.</summary>
public sealed class ArgParseException : Exception
{
    /// <summary>
    /// Creates an <see cref="ArgParseException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ArgParseException(string message) : base(message)
    {
    }
}

/// <summary>
/// Minimal, dependency-free command-line parser for the supported commands.
/// </summary>
public static class ArgParser
{
    /// <summary>
    /// Parses command-line arguments into a command and its options.
    /// </summary>
    /// <param name="args">The raw arguments.</param>
    /// <param name="configPath">The configuration file path.</param>
    /// <param name="checkPing">Whether to ping after sending the packet.</param>
    /// <param name="timeoutMs">The ping timeout in milliseconds.</param>
    /// <param name="showHelp">Whether help was requested.</param>
    /// <returns>The selected command and optional device name.</returns>
    public static (Command Command, string? Device) Parse(
        string[] args,
        out string configPath,
        out bool checkPing,
        out int timeoutMs,
        out bool showHelp)
    {
        configPath = "appsettings.json";
        checkPing = false;
        timeoutMs = WakeService.DefaultPingTimeoutMs;
        showHelp = false;
        Command command = Command.Interactive;
        string? device = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "-h":
                case "--help":
                    showHelp = true;
                    return (Command.Interactive, null);

                case "--config":
                    if (++i >= args.Length)
                    {
                        throw new ArgParseException("--config requires a path argument.");
                    }

                    configPath = args[i];
                    break;

                case "--ping":
                    checkPing = true;
                    break;

                case "--timeout":
                    if (++i >= args.Length
                        || !int.TryParse(args[i], out int timeout)
                        || timeout <= 0)
                    {
                        throw new ArgParseException("--timeout requires a positive number of milliseconds.");
                    }

                    timeoutMs = timeout;
                    break;

                case "list":
                    command = Command.List;
                    break;

                case "wake":
                    command = Command.Wake;
                    if (++i >= args.Length)
                    {
                        throw new ArgParseException("wake requires a device name.");
                    }

                    device = args[i];
                    break;

                case "interactive":
                    command = Command.Interactive;
                    break;

                default:
                    if (arg.StartsWith("--"))
                    {
                        throw new ArgParseException($"Unknown option '{arg}'.");
                    }

                    throw new ArgParseException($"Unexpected argument '{arg}'.");
            }
        }

        return (command, device);
    }
}
