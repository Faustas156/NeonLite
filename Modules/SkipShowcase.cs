using HarmonyLib;
using MelonLoader;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class SkipShowcase : Module
    {
        private static MelonPreferences_Entry<bool> _setting_SkipShowcase;

        public SkipShowcase() =>
            _setting_SkipShowcase = NeonLite.Config_NeonLite.CreateEntry("Insight Screen Remover", false, description: "No longer displays the \"Insight Crystal Dust (Empty)\" screen after finishing a sidequest level.");



        [HarmonyPrefix]
        [HarmonyPatch(typeof(MainMenu), "SetItemShowcaseCard")]
        private static bool PreSetItemShowcaseCard(ref Action callback)
        {
            if (!_setting_SkipShowcase.Value)
                return true;

            callback?.Invoke();
            return false;
        }
    }
}
