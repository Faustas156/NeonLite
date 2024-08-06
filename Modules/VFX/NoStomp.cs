using HarmonyLib;
using System.Reflection;
using UnityEngine.Rendering;

namespace NeonLite.Modules.VFX
{
    // ORIGINAL CODE BY PUPPYPOWERTOOLS AUTHOR HECATE/PANDORAS FOX
    internal class NoStomp : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static bool hit = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noStomp", "Disable stomp splashbang", "Disable the white flash that appears when you land a stomp.", false);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(ScannerEffect), "OnStomp");
        static void Activate(bool activate)
        {
            if (activate)
                NeonLite.Harmony.Patch(original, prefix: Helpers.HM(StopFade));
            else
                NeonLite.Harmony.Unpatch(original, Helpers.MI(StopFade));

            active = activate;
        }

        static bool StopFade() => false;
    }
}
