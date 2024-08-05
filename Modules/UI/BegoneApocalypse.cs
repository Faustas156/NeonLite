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
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(MenuScreenMapAesthetics), "Start");
        static void Activate(bool activate)
        {
            if (activate)
                NeonLite.Harmony.Patch(original, postfix: Helpers.HM(RemoveApocalypse));
            else
                NeonLite.Harmony.Unpatch(original, Helpers.MI(RemoveApocalypse));

            active = activate;
        }

        static void RemoveApocalypse(ref MenuScreenMapAesthetics __instance) => __instance.SetApocalypse(false);
    }
}
