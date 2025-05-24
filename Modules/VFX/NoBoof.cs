using HarmonyLib;
using System.Reflection;

#pragma warning disable CS0414

namespace NeonLite.Modules.Misc.VFX
{
    internal class NoBoof : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noBoof", "Disable Book of Life overlay", "Disable the red diamond border from Book of Life.", false);
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(PlayerUI), "SetTelefragOverlay", StopTelefrag, Patching.PatchTarget.Prefix);
            active = activate;
        }

        static void StopTelefrag(ref bool on) => on = false;
    }
}
