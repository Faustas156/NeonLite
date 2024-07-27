using HarmonyLib;

namespace NeonLite
{
    [HarmonyPatch]
    internal class ExtFovSlider
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SliderUIPrefab), "Initialise")]
        internal static void Initialize(ref string localisationKey, ref OptionsMenuPanelInformation.OptionEntry optionEntry)
        {
            if (localisationKey == "Interface/OPTIONS_01_FOV")
                optionEntry.SliderMaximum = 160;
        }
    }
}
