using Terminal.Gui;
using WoLock.Core;

namespace WoLock.Tui.Interactive;

/// <summary>
/// The interactive Terminal.Gui front-end: a device list, a wake action, and
/// a status display.
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
            Title = "WoLock — Profile: " + service.ActiveProfileName,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        ListView listView = new ListView(names)
        {
            X = 0,
            Y = 0,
            Width = 40,
            Height = Dim.Fill(3),
        };

        Button wakeButton = new Button("Wake")
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Height = 1,
        };

        Label statusLabel = new Label
        {
            Text = "Select a device and press Wake (or Enter). Press q to quit.",
            X = 42,
            Y = 0,
            Width = Dim.Fill(),
            Height = 3,
        };

        root.Add(listView, wakeButton, statusLabel);

        void RefreshStatus(string message)
        {
            statusLabel.Text = message;
            Application.Refresh();
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
            RefreshStatus(message);
        }

        wakeButton.Clicked += DoWake;
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

        Application.Run(root, _ => true);
        Application.Shutdown();
        return 0;
    }
}
