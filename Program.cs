// Copyright (c) 2026 Giacomo Lacava
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace BTChargeInTray
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            using var mutex = new Mutex(true, @"Local\BtBatteryTray", out bool isFirst);
            if (!isFirst) return;

            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApp());
        }
    }
}
