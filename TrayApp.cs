using System.Diagnostics;

namespace BTChargeInTray
{
    internal sealed class TrayApp : ApplicationContext
    {
        private const int OfflineGraceMisses = 2;   // consecutive missed polls before a device is shown offline
        private const int NotifyRearmMargin = 5;    // points above threshold before a device can notify again

        private readonly NotifyIcon _notify;
        private readonly PopupForm _popup = new();
        private readonly System.Windows.Forms.Timer _pollTimer = new();
        private readonly System.Windows.Forms.Timer _reloadDebounce = new() { Interval = 500 };
        private readonly FileSystemWatcher? _watcher;
        private readonly ToolStripMenuItem _autostartItem;

        private AppConfig _config;
        private IReadOnlyList<DeviceStatus> _statuses = Array.Empty<DeviceStatus>();
        private Icon? _currentIcon;
        private bool _polling;

        private readonly Dictionary<string, int> _lastKnown = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _misses = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _notified = new(StringComparer.OrdinalIgnoreCase);

        public TrayApp()
        {
            _config = AppConfig.Load(out var err);

            _autostartItem = new ToolStripMenuItem("Start with Windows") { Checked = Autostart.IsEnabled(), CheckOnClick = false };
            _autostartItem.Click += (_, _) =>
            {
                try { Autostart.Set(!_autostartItem.Checked); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "BtBatteryTray"); }
                _autostartItem.Checked = Autostart.IsEnabled();
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Refresh now", null, async (_, _) => await PollAsync());
            menu.Items.Add(_autostartItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Open config file", null, (_, _) => OpenConfig());
            menu.Items.Add("Reload config", null, (_, _) => ReloadConfig());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => ExitThread());

            _notify = new NotifyIcon { ContextMenuStrip = menu, Text = "BtBatteryTray", Visible = true };
            _notify.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtons.Left) _popup.Toggle(Cursor.Position);
            };
            SetIcon(TrayIconRenderer.Render(null, Palette.Tile(null, _config)));

            _pollTimer.Interval = _config.PollIntervalSeconds * 1000;
            _pollTimer.Tick += async (_, _) => await PollAsync();
            _pollTimer.Start();

            _reloadDebounce.Tick += (_, _) => { _reloadDebounce.Stop(); ReloadConfig(); };
            try
            {
                System.IO.Directory.CreateDirectory(AppConfig.Directory);
                _watcher = new FileSystemWatcher(AppConfig.Directory, Path.GetFileName(AppConfig.FilePath))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true,
                };
                var ctx = SynchronizationContext.Current;
                FileSystemEventHandler h = (_, _) => ctx?.Post(_ => { _reloadDebounce.Stop(); _reloadDebounce.Start(); }, null);
                _watcher.Changed += h;
                _watcher.Created += h;
                _watcher.Renamed += (_, _) => ctx?.Post(_ => { _reloadDebounce.Stop(); _reloadDebounce.Start(); }, null);
            }
            catch { /* hot reload is optional */ }

            if (err != null)
                _notify.ShowBalloonTip(5000, "BtBatteryTray", $"Config error, using defaults: {err}", ToolTipIcon.Warning);

            _ = PollAsync();
        }

        private void OpenConfig()
        {
            try { Process.Start(new ProcessStartInfo(AppConfig.FilePath) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "BtBatteryTray"); }
        }

        private void ReloadConfig()
        {
            _config = AppConfig.Load(out var err);
            if (err != null)
                _notify.ShowBalloonTip(5000, "BtBatteryTray", $"Config error, using defaults: {err}", ToolTipIcon.Warning);
            _pollTimer.Interval = _config.PollIntervalSeconds * 1000;
            _ = PollAsync();
        }

        private async Task PollAsync()
        {
            if (_polling) return;
            _polling = true;
            try
            {
                var cfg = _config;
                var readings = await Task.Run(BatteryReader.ReadAll);
                _statuses = BuildStatuses(cfg, readings);
                Apply(cfg);
            }
            catch (Exception ex)
            {
                _notify.Text = Truncate($"Error: {ex.Message}", 127);
            }
            finally { _polling = false; }
        }

        private List<DeviceStatus> BuildStatuses(AppConfig cfg, Dictionary<string, int> readings)
        {
            var list = new List<DeviceStatus>();
            foreach (var d in cfg.Devices.Where(d => d.Enabled && !string.IsNullOrWhiteSpace(d.Name)))
            {
                int? pct = null;
                if (readings.TryGetValue(d.Name, out int v))
                {
                    pct = v;
                    _lastKnown[d.Name] = v;
                    _misses[d.Name] = 0;
                }
                else
                {
                    int m = _misses.GetValueOrDefault(d.Name) + 1;
                    _misses[d.Name] = m;
                    // brief grace period so a single missed read doesn't flap to offline
                    if (m < OfflineGraceMisses && _lastKnown.TryGetValue(d.Name, out int last)) pct = last;
                }
                list.Add(new DeviceStatus(d.Name, d.Icon, pct));
            }
            return list;
        }

        private void Apply(AppConfig cfg)
        {
            // Tray icon: lowest online battery, grey dash when everything is offline.
            int? lowest = _statuses.Where(s => s.IsOnline).Select(s => s.Percent).DefaultIfEmpty(null).Min();
            SetIcon(TrayIconRenderer.Render(lowest, Palette.Tile(lowest, cfg)));

            var lines = _statuses.Select(s => s.IsOnline ? $"{s.Icon} {s.Percent}% {s.Name}" : $"{s.Icon} offline {s.Name}");
            _notify.Text = Truncate(_statuses.Count == 0 ? "No devices enabled" : string.Join("\n", lines), 127);

            _popup.SetStatuses(_statuses, cfg);
            CheckNotifications(cfg);
        }

        private void CheckNotifications(AppConfig cfg)
        {
            foreach (var s in _statuses)
            {
                if (!s.IsOnline) continue;
                if (s.Percent < cfg.LowBatteryThreshold)
                {
                    if (_notified.Add(s.Name))
                        _notify.ShowBalloonTip(8000, "Low battery", $"{s.Icon} {s.Name}: {s.Percent}%", ToolTipIcon.Warning);
                }
                else if (s.Percent >= cfg.LowBatteryThreshold + NotifyRearmMargin)
                {
                    _notified.Remove(s.Name);
                }
            }
        }

        private void SetIcon(Icon icon)
        {
            var old = _currentIcon;
            _notify.Icon = icon;
            _currentIcon = icon;
            old?.Dispose();
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

        protected override void ExitThreadCore()
        {
            _pollTimer.Stop();
            _watcher?.Dispose();
            _notify.Visible = false;
            _notify.Dispose();
            _popup.Dispose();
            _currentIcon?.Dispose();
            base.ExitThreadCore();
        }
    }
}
