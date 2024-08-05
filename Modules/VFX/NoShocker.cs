using HarmonyLib;
using System.Reflection;

#pragma warning disable CS0414

namespace NeonLite.Modules.Misc.VFX
{
    // ORIGINAL CODE BY PUPPYPOWERTOOLS AUTHOR HECATE/PANDORAS FOX
    internal class NoShocker : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noShocker", "Disable shocker overlay", "Disable the white flash from shockers (not the explosion!)", false);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(UIScreenFader), "FadeScreen");
        static void Activate(bool activate)
        {
            if (activate)
                NeonLite.Harmony.Patch(original, prefix: Helpers.HM(StopFade));
            else
                NeonLite.Harmony.Unpatch(original, Helpers.MI(StopFade));

            active = activate;
        }

        static bool StopFade(UIScreenFader.FadeType ft, float time) => !(ft == UIScreenFader.FadeType.FadeIn && time == 0.333f);
    }
}
