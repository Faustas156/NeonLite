using HarmonyLib;

namespace NeonLite
{
    [HarmonyPatch]
    internal class ExtSlider
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SliderUIPrefab), "Initialise")]
        internal static void Initialize(ref string localisationKey, ref OptionsMenuPanelInformation.OptionEntry optionEntry)
        {
            if (localisationKey == "Interface/OPTIONS_01_FOV")
                optionEntry.SliderMaximum = 160;
            if (localisationKey == "Interface/OPTIONS_02_MOUSESEN")
                optionEntry.StepSize = 0.01f;
        }
    }
}
