using HarmonyLib;
using MelonLoader;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class BegoneApocalypse : Module
    {
        private static MelonPreferences_Entry<bool> _setting_Apocalypse_display;

        public BegoneApocalypse() =>
            _setting_Apocalypse_display = NeonLite.Config_NeonLite.CreateEntry("Begone Apocalypse", true, description: "Get rid of the Apocalyptic view and replace it with the blue skies.");

        [HarmonyPostfix()]
        [HarmonyPatch(typeof(MenuScreenMapAesthetics), "Start")]
        private static void RemoveApocalypse(MenuScreenMapAesthetics __instance) =>
            __instance.SetApocalypse(!_setting_Apocalypse_display.Value);
    }
}
