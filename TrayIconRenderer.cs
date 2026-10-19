using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace BTChargeInTray
{
    internal static class TrayIconRenderer
    {
        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// The tray icon is too small for a full percentage, so it shows a single character:
        /// the tens digit (87% → "8", 60% → "6", 5% → "0"), "F" for a full 100%, "–" when offline.
        /// Exact values live in the tooltip and popup.
        /// </summary>
        internal static string Glyph(int? percent) => percent switch
        {
            null => "–",
            >= 100 => "F",
            var p => (Math.Max(0, p.Value) / 10).ToString(),
        };

        /// <summary>Draws a colored tile with a one-character level glyph (see <see cref="Glyph"/>). Caller owns the returned Icon.</summary>
        public static Icon Render(int? percent, Color tile)
        {
            int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
            using var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                using (var path = RoundedRect(new RectangleF(0, 0, size, size), size / 5f))
                using (var brush = new SolidBrush(tile))
                    g.FillPath(brush, path);

                using var font = new Font("Segoe UI", size * 0.75f, FontStyle.Bold, GraphicsUnit.Pixel);
                using var fmt = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap,
                };
                g.DrawString(Glyph(percent), font, Brushes.White, new RectangleF(0, 0, size, size + 1), fmt);
            }

            IntPtr h = bmp.GetHicon();
            try { return (Icon)Icon.FromHandle(h).Clone(); }
            finally { DestroyIcon(h); } // avoid leaking a GDI handle on every update
        }

        private static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            float d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
