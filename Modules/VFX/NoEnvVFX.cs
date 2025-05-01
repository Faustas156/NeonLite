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
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(EnvironmentFX), "StartEventSchedule", StopFX, Patching.PatchTarget.Prefix);

            active = activate;
        }

        static bool StopFX() => false;
    }

}
