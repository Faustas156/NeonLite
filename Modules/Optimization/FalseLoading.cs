using HarmonyLib;
using System.Reflection;

namespace NeonLite.Modules.Optimization
{
    internal class FalseLoading : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "fakeLoading", "Loading optimizations", "Forces the game to not wait on loading screens.\nDisable if you have weird menu issues.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo ogload = AccessTools.Method(typeof(MenuScreenLoading), "LoadScene");

        static void Activate(bool activate)
        {
            if (activate)
                Patching.AddPatch(ogload, RemoveFrontload, Patching.PatchTarget.Prefix);
            else
                Patching.RemovePatch(ogload, RemoveFrontload);

            active = activate;
        }

        static void RemoveFrontload(ref bool enforceMinimumTime, ref bool frontloadWait) => enforceMinimumTime = frontloadWait = false;
    }
}
