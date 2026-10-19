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
