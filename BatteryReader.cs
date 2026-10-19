// Copyright (c) 2026 Giacomo Lacava
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.InteropServices;

namespace BTChargeInTray
{
    /// <summary>
    /// Reads DEVPKEY_Bluetooth_Battery for present devnodes, matched by friendly name.
    /// </summary>
    internal static class BatteryReader
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DEVPROPKEY
        {
            public Guid fmtid;
            public uint pid;
            public DEVPROPKEY(Guid g, uint p) { fmtid = g; pid = p; }
        }

        private const uint CR_SUCCESS = 0;
        private const uint CR_BUFFER_SMALL = 0x1A;
        private const uint CM_GETIDLIST_FILTER_PRESENT = 0x100;

        private static readonly DEVPROPKEY BatteryKey =
            new(new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"), 2);
        private static readonly DEVPROPKEY FriendlyNameKey =
            new(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);
        private static readonly DEVPROPKEY NameKey =
            new(new Guid("B725F130-47EF-101A-A5F1-02608C9EEBAC"), 10);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        private static extern uint CM_Get_Device_ID_List_SizeW(out uint len, string? filter, uint flags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        private static extern uint CM_Get_Device_ID_ListW(string? filter, char[] buffer, uint len, uint flags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        private static extern uint CM_Locate_DevNodeW(out uint devInst, string deviceId, uint flags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        private static extern uint CM_Get_DevNode_PropertyW(
            uint devInst, ref DEVPROPKEY key, out uint propType, byte[]? buffer, ref uint size, uint flags);

        private static byte[]? GetProp(uint inst, DEVPROPKEY key)
        {
            uint size = 0;
            uint cr = CM_Get_DevNode_PropertyW(inst, ref key, out _, null, ref size, 0);
            if (cr != CR_BUFFER_SMALL || size == 0) return null;
            var buf = new byte[size];
            cr = CM_Get_DevNode_PropertyW(inst, ref key, out _, buf, ref size, 0);
            return cr == CR_SUCCESS ? buf : null;
        }

        private static string? GetString(uint inst, DEVPROPKEY key)
        {
            var b = GetProp(inst, key);
            return b == null ? null : System.Text.Encoding.Unicode.GetString(b).TrimEnd('\0');
        }

        private static IEnumerable<string> PresentDeviceIds()
        {
            if (CM_Get_Device_ID_List_SizeW(out uint len, null, CM_GETIDLIST_FILTER_PRESENT) != CR_SUCCESS || len == 0)
                return Array.Empty<string>();
            var buf = new char[len];
            if (CM_Get_Device_ID_ListW(null, buf, len, CM_GETIDLIST_FILTER_PRESENT) != CR_SUCCESS)
                return Array.Empty<string>();
            return new string(buf).Split('\0', StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Returns every present device that reports a battery level, keyed by friendly name.
        /// If several devnodes share a name, the first one with a battery value wins.
        /// </summary>
        public static Dictionary<string, int> ReadAll()
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in PresentDeviceIds())
            {
                try
                {
                    if (CM_Locate_DevNodeW(out uint inst, id, 0) != CR_SUCCESS) continue;
                    var bat = GetProp(inst, BatteryKey);
                    if (bat == null || bat.Length == 0) continue;
                    var name = GetString(inst, FriendlyNameKey) ?? GetString(inst, NameKey);
                    if (string.IsNullOrEmpty(name)) continue;
                    result.TryAdd(name, bat[0]);
                }
                catch { /* skip devnodes we can't read */ }
            }
            return result;
        }
    }
}
