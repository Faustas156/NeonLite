using HarmonyLib;
using System.Reflection;


namespace NeonLite.Modules.UI
{
    internal class BegoneApocalypse : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI", "noGreen", "Begone Apocalypse", "Get rid of the Apocalyptic view and replace it with the blue skies.", true);
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(MenuScreenMapAesthetics), "Start");
        static void Activate(bool activate)
        {
            if (activate)
                Patching.AddPatch(original, RemoveApocalypse, Patching.PatchTarget.Postfix);
            else
                Patching.RemovePatch(original, RemoveApocalypse);

            active = activate;
        }

        static void RemoveApocalypse(ref MenuScreenMapAesthetics __instance) => __instance.SetApocalypse(false);
    }
}
