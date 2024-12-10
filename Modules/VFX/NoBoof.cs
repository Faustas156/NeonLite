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
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(PlayerUI), "SetTelefragOverlay");
        static void Activate(bool activate)
        {
            if (activate)
                Patching.AddPatch(original, StopTelefrag, Patching.PatchTarget.Prefix);
            else
                Patching.RemovePatch(original, StopTelefrag);

            active = activate;
        }

        static void StopTelefrag(ref bool on) => on = false;
    }
}
