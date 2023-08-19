using HarmonyLib;
using MelonLoader;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class BegoneApocalypse : Module
    {
        private static MelonPreferences_Entry<bool> Apocalypse_display;

        public BegoneApocalypse() =>
            Apocalypse_display = NeonLite.neonLite_config.CreateEntry("Begone Apocalypse", true, description: "Get rid of the Apocalyptic view and replace it with the blue skies.");

        [HarmonyPostfix()]
        [HarmonyPatch(typeof(MenuScreenMapAesthetics), "Start")]
        private static void RemoveApocalypse(MenuScreenMapAesthetics __instance) =>
            __instance.SetApocalypse(!Apocalypse_display.Value);
    }
}
