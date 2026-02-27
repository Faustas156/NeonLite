using System.Reflection;

namespace NeonLite.Modules.UI
{
    [Module]
    internal static class ExtraSlider
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static void Activate(bool _) => Patching.AddPatch(typeof(SliderUIPrefab), "Initialise", Initialize, Patching.PatchTarget.Prefix);

        static void Initialize(ref string localisationKey, ref OptionsMenuPanelInformation.OptionEntry optionEntry)
        {
            //if (localisationKey == "Interface/OPTIONS_01_FOV")
            //    optionEntry.SliderMaximum = 160;
            if (localisationKey == "Interface/OPTIONS_02_MOUSESEN")
                optionEntry.StepSize = 0.01f;
        }

    }
}
