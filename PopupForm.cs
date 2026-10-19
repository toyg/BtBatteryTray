// Copyright (c) 2026 Giacomo Lacava
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace BTChargeInTray
{
    /// <summary>Small borderless flyout listing every monitored device.</summary>
    internal sealed class PopupForm : Form
    {
        private readonly FlowLayoutPanel _panel = new()
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Dock = DockStyle.Fill,
        };

        private DateTime _lastHidden = DateTime.MinValue;

        public PopupForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.FromArgb(32, 32, 32);
            ForeColor = Color.White;
            Controls.Add(_panel);
            Deactivate += (_, _) => { if (Visible) Hide(); };
            VisibleChanged += (_, _) => { if (!Visible) _lastHidden = DateTime.UtcNow; };
        }

        // Tool-window style so it never shows in Alt+Tab.
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        protected override bool ShowWithoutActivation => false;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(80, 80, 80));
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        public void SetStatuses(IReadOnlyList<DeviceStatus> statuses, AppConfig cfg)
        {
            _panel.SuspendLayout();
            foreach (Control c in _panel.Controls) c.Dispose();
            _panel.Controls.Clear();

            if (statuses.Count == 0)
            {
                _panel.Controls.Add(MakeLabel("No devices enabled", Color.Gray));
            }
            foreach (var s in statuses)
            {
                string text = s.IsOnline ? $"{s.Icon} {s.Percent}%  {s.Name}" : $"{s.Icon} offline  {s.Name}";
                _panel.Controls.Add(MakeLabel(text, Palette.Text(s.Percent, cfg)));
            }
            _panel.ResumeLayout(true);
        }

        private static Label MakeLabel(string text, Color color) => new()
        {
            AutoSize = true,
            Text = text,
            ForeColor = color,
            Font = new Font("Segoe UI Emoji", 11),
            Margin = new Padding(0, 2, 0, 2),
        };

        /// <summary>Toggles visibility; ignores the click that merely dismissed the popup via Deactivate.</summary>
        public void Toggle(Point anchor)
        {
            if (Visible) { Hide(); return; }
            if ((DateTime.UtcNow - _lastHidden).TotalMilliseconds < 300) return;

            PerformLayout();
            var wa = Screen.FromPoint(anchor).WorkingArea;
            int x = Math.Clamp(anchor.X - Width / 2, wa.Left + 8, wa.Right - Width - 8);
            int y = anchor.Y > wa.Top + wa.Height / 2
                ? wa.Bottom - Height - 8
                : wa.Top + 8;
            Location = new Point(x, y);
            Show();
            Activate();
        }
    }
}
