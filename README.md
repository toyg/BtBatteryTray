# BtBatteryTray

A small Windows system tray utility that shows the battery level of your Bluetooth devices
(keyboard, headset, mouse, ...). It is a C# port of an earlier PowerShell script.

## What it does

- **One tray icon** showing the *lowest* battery among your online devices, as a colored tile
  with a single character:

  | Level | Icon shows |
  |---|---|
  | 0-9% | `0` |
  | 10-99% | `1` to `9` |
  | 100% | `F` |
  | all devices offline | `–` (grey) |

  The tile is red below the low threshold (default 20%), orange up to the warn threshold
  (default 60%), green above. Hover for exact values.
- **Click the icon** for a popup listing every monitored device with its exact percentage.
- **Low-battery notification** when a device drops below the threshold (once per crossing; it
  re-arms after the device recovers 5 points above it).
- **Offline state** for devices that are not connected, with a short grace period so a single
  missed read does not flap.
- **Right-click menu:** Refresh now, Start with Windows, Open config file, Reload config, Exit.

Battery levels are read directly from the Windows device property
`DEVPKEY_Bluetooth_Battery` (`{104EA319-6EE2-4701-BD47-8DDBF425BBE5}`, pid 2) via `cfgmgr32`,
matching devices by their Windows friendly name. No Bluetooth stack calls or extra packages.

## Requirements

- Windows 10/11
- .NET 10 Desktop Runtime (the build is framework-dependent)
- .NET 10 SDK to build

## Build and run

```
dotnet publish -c Release -r win-x64 --self-contained false -o publish
publish\BtBatteryTray.exe
```

Keep the `.dll` and `.json` files next to the exe. Only one instance runs at a time.

## Configuration

On first run `%AppData%\BtBatteryTray\settings.json` is created. Edit it (tray menu: *Open config
file*); changes are picked up automatically.

```json
{
  "pollIntervalSeconds": 60,
  "lowBatteryThreshold": 20,
  "warnThreshold": 60,
  "devices": [
    { "name": "Eyelash Sofle", "icon": "⌨️", "enabled": true },
    { "name": "MIXX StreamQ C1 Hands-Free AG", "icon": "🎧", "enabled": true },
    { "name": "BT5.2 Mouse", "icon": "🖱️", "enabled": false }
  ]
}
```

- `name` must match the device's friendly name in Windows (Device Manager) exactly. Note that
  headsets often expose a separate "... Hands-Free AG" entry that carries the battery value.
- `pollIntervalSeconds` is clamped to a minimum of 5.
- `icon` is shown next to the device in the popup and tooltip.

## Project layout

| File | Purpose |
|---|---|
| `Program.cs` | Entry point, single-instance mutex |
| `TrayApp.cs` | Tray icon, menu, polling, notifications |
| `BatteryReader.cs` | `cfgmgr32` P/Invoke battery reads |
| `TrayIconRenderer.cs` | Draws the tray icon |
| `PopupForm.cs` | Device list flyout opened from the tray icon |
| `Config.cs` | JSON settings load/save |
| `DeviceStatus.cs` | Status record and color palette |
| `Autostart.cs` | "Start with Windows" registry entry |

## License

MIT. See [LICENSE](LICENSE).
