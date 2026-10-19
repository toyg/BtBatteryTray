// Copyright (c) 2026 Giacomo Lacava
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BTChargeInTray
{
    internal sealed class DeviceConfig
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "🔋";
        public bool Enabled { get; set; } = true;
    }

    internal sealed class AppConfig
    {
        public int PollIntervalSeconds { get; set; } = 60;
        public int LowBatteryThreshold { get; set; } = 20;
        public int WarnThreshold { get; set; } = 60;
        public List<DeviceConfig> Devices { get; set; } = new();

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static string Directory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BtBatteryTray");

        public static string FilePath => Path.Combine(Directory, "settings.json");

        public static AppConfig CreateDefault() => new()
        {
            Devices =
            {
                new() { Name = "Eyelash Sofle", Icon = "⌨️" },
                new() { Name = "MIXX StreamQ C1 Hands-Free AG", Icon = "🎧" },
                new() { Name = "BT5.2 Mouse", Icon = "🖱️", Enabled = false },
                new() { Name = "JVC HA-S30BT Hands-Free AG", Icon = "🎧", Enabled = false },
            }
        };

        /// <summary>Loads the config, creating the default file on first run. Falls back to defaults on error.</summary>
        public static AppConfig Load(out string? error)
        {
            error = null;
            try
            {
                if (!File.Exists(FilePath))
                {
                    var def = CreateDefault();
                    def.Save();
                    return def;
                }
                var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(FilePath, Encoding.UTF8), Options)
                          ?? CreateDefault();
                cfg.PollIntervalSeconds = Math.Max(5, cfg.PollIntervalSeconds);
                return cfg;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return CreateDefault();
            }
        }

        public void Save()
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Options), new UTF8Encoding(false));
        }
    }
}
