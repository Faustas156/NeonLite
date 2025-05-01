using System.Runtime.InteropServices;
using System;
using UnityEngine.Rendering;
using MelonLoader;

namespace NeonLite.Modules
{
    internal class MLVersionWarn : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;
         
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr MessageBox(int hWnd, string text, string caption, uint type);

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "mlverWarn", "Last ML Version", null, "", true);

            var ver = typeof(MelonMod).Assembly.GetName().Version;
            var verstr = $"{ver.Major}.{ver.Minor}.{ver.Build}";

            if (setting.Value != verstr)
            {
                if (verstr != "0.6.1")
                    MessageBox(0, "You're using a version of MelonLoader that's not v0.6.1.\nIt is recommended to switch your MelonLoader version to v0.6.1 to avoid mod incompatibilities and bugs.\n\nThis message will not be shown again.", "NeonLite", 0x30);
                setting.Value = verstr;
                MelonPreferences.Save();
            }
        }
    }
}
