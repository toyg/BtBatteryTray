// Copyright (c) 2026 Giacomo Lacava
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace BTChargeInTray
{
    internal sealed record DeviceStatus(string Name, string Icon, int? Percent)
    {
        public bool IsOnline => Percent.HasValue;
    }

    internal static class Palette
    {
        // Brighter shades for text on the dark popup, darker shades for tray icon tiles.
        public static Color Text(int? pct, AppConfig cfg) => pct switch
        {
            null => Color.FromArgb(150, 150, 150),
            _ when pct < cfg.LowBatteryThreshold => Color.FromArgb(255, 90, 90),
            _ when pct <= cfg.WarnThreshold => Color.FromArgb(255, 175, 40),
            _ => Color.FromArgb(90, 220, 110),
        };

        public static Color Tile(int? pct, AppConfig cfg) => pct switch
        {
            null => Color.FromArgb(110, 110, 110),
            _ when pct < cfg.LowBatteryThreshold => Color.FromArgb(200, 40, 40),
            _ when pct <= cfg.WarnThreshold => Color.FromArgb(205, 110, 0),
            _ => Color.FromArgb(30, 140, 60),
        };
    }
}
