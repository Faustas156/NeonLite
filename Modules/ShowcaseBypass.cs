using HarmonyLib;
using MelonLoader;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class ShowcaseBypass : Module
    {
        public static MelonPreferences_Entry<bool> ShowcaseBypass_enable;

        public ShowcaseBypass() =>
            ShowcaseBypass_enable = NeonLite.neonLite_config.CreateEntry("Insight Screen Remover", false, description: "No longer displays the \"Insight Crystal Dust (Empty)\" screen after finishing a sidequest level.");



        [HarmonyPrefix]
        [HarmonyPatch(typeof(MainMenu), "SetItemShowcaseCard")]
        private static bool PreSetItemShowcaseCard(ref Action callback)
        {
            if (!ShowcaseBypass_enable.Value)
                return true;

            callback?.Invoke();
            return false;
        }
    }
}
