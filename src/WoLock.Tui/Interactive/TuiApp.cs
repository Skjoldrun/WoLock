using System.Net;
using Terminal.Gui;
using WoLock.Core;

namespace WoLock.Tui.Interactive;

/// <summary>
/// The interactive Terminal.Gui front-end: a device list, a wake action, and a
/// status display, styled with the OneDark Pro theme.
/// </summary>
public static class TuiApp
{
    /// <summary>
    /// Runs the interactive Terminal.Gui UI for the given service.
    /// </summary>
    /// <param name="service">The wake service providing devices.</param>
    /// <returns>A process exit code.</returns>
    public static int Run(WakeService service)
    {
        Application.Init();

        List<string> names = service.Devices.Select(d => d.Name).ToList();

        Window root = new Window
        {
            Title = $"WoLock — {service.ActiveProfileName}",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = OneDarkTheme.Window,
        };

        // Set on the existing border so the window title stays in the title bar.
        root.Border.BorderStyle = BorderStyle.Rounded;
        root.Border.BorderBrush = OneDarkTheme.Colors.Blue;

        Label subtitle = new Label
        {
            Text = $"{service.Devices.Count} device(s)",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            ColorScheme = OneDarkTheme.Title,
        };

        ListView listView = new ListView(names)
        {
            X = 0,
            Y = 3,
            Width = Dim.Percent(58),
            Height = Dim.Fill(6),
            ColorScheme = OneDarkTheme.List,
        };

        (Label mac, Label targets) = BuildDetails();
        root.Add(listView, mac, targets);

        Button wakeButton = new Button("_Wake")
        {
            X = 0,
            Y = Pos.AnchorEnd(3),
            Height = 1,
            ColorScheme = OneDarkTheme.Button,
        };

        Label status = new Label
        {
            Text = "Ready.",
            X = Pos.Percent(62),
            Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(),
            ColorScheme = OneDarkTheme.Window,
        };

        Label footer = new Label
        {
            Text = "  <Enter> wake   <q> quit  ",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            ColorScheme = OneDarkTheme.Window,
        };

        root.Add(subtitle, wakeButton, status, footer);

        void RefreshStatus(string message)
        {
            status.Text = message;
            Application.Refresh();
        }

        void OnSelectionChanged()
        {
            int index = listView.SelectedItem;
            if (index < 0 || index >= service.Devices.Count)
            {
                return;
            }

            Device device = service.Devices[index];
            mac.Text = $"MAC  {device.Request.Mac}";
            targets.Text = $"Targets  {FormatTargets(device.Request.Targets)}";
        }

        void DoWake()
        {
            int selectedId = listView.SelectedItem;
            if (selectedId < 0 || selectedId >= names.Count)
            {
                RefreshStatus("No device selected.");
                return;
            }

            string name = names[selectedId];
            RefreshStatus($"Waking {name}...");
            _ = Task.Run(() =>
            {
                WakeResult result = service.WakeAsync(
                    name, checkPing: true, timeoutMs: WakeService.DefaultPingTimeoutMs, CancellationToken.None).GetAwaiter().GetResult();
                RefreshResult(result);
            });
        }

        void RefreshResult(WakeResult result)
        {
            string message = result.Status switch
            {
                WakeStatus.Responding => $"{result.DeviceName}: device responded.",
                WakeStatus.PacketSent => $"{result.DeviceName}: packet sent.",
                WakeStatus.NoResponse => $"{result.DeviceName}: no response.",
                WakeStatus.Failed => $"{result.DeviceName}: failed. {result.Error}",
                _ => result.DeviceName,
            };
            status.ColorScheme = OneDarkTheme.StatusColor(result.Status);
            RefreshStatus(message);
        }

        wakeButton.Clicked += DoWake;
        listView.SelectedItemChanged += _ => OnSelectionChanged();
        root.KeyDown += args =>
        {
            Key key = (Key)args.KeyEvent.KeyValue;
            if (key == Key.Q || key == Key.q)
            {
                Application.RequestStop(root);
            }
            else if (key == Key.Enter)
            {
                DoWake();
            }
        };

        OnSelectionChanged();
        Application.Run(root, _ => true);
        Application.Shutdown();
        return 0;
    }

    private static (Label mac, Label targets) BuildDetails()
    {
        Label mac = new Label
        {
            Text = "MAC  —",
            X = Pos.Percent(62),
            Y = 3,
            Width = Dim.Fill(),
            ColorScheme = OneDarkTheme.Detail,
        };

        Label targets = new Label
        {
            Text = "Targets  —",
            X = Pos.Percent(62),
            Y = 6,
            Width = Dim.Fill(),
            ColorScheme = OneDarkTheme.Window,
        };

        return (mac, targets);
    }

    private static string FormatTargets(IList<IPEndPoint> targets) =>
        targets.Count == 0
            ? "—"
            : string.Join(", ", targets.Select(t => $"{t.Address}:{t.Port}"));
}
