using HarmonyLib;
using System.Reflection;

namespace NeonLite.Modules.VFX
{
    internal class NoEnvVFX : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noEnvVFX", "Disable additional environment FX", "Disables stuff like chapter 11 lightning, and possibly other stuff.", false);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(EnvironmentFX), "StartEventSchedule");
        static void Activate(bool activate)
        {
            if (activate)
                Patching.AddPatch(original, StopFX, Patching.PatchTarget.Prefix);
            else
                Patching.RemovePatch(original, StopFX);

            active = activate;
        }

        static bool StopFX() => false;
    }

}
