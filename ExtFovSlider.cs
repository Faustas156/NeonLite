using HarmonyLib;

namespace NeonLite
{
    [HarmonyPatch]
    internal class ExtFovSlider
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SliderUIPrefab), "Initialise")]
        internal static void Initialize(ref OptionsMenuPanelInformation.OptionEntry optionEntry) => optionEntry.SliderMaximum = 160;
    }
}
