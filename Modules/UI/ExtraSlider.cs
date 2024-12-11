using HarmonyLib;
using System.Reflection;

namespace NeonLite.Modules.UI
{
    internal class ExtraSlider : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static void Setup() { }

        static readonly MethodInfo original = AccessTools.Method(typeof(SliderUIPrefab), "Initialise");
        static void Activate(bool activate) => Patching.AddPatch(original, Initialize, Patching.PatchTarget.Prefix);

        static void Initialize(ref string localisationKey, ref OptionsMenuPanelInformation.OptionEntry optionEntry)
        {
            //if (localisationKey == "Interface/OPTIONS_01_FOV")
            //    optionEntry.SliderMaximum = 160;
            if (localisationKey == "Interface/OPTIONS_02_MOUSESEN")
                optionEntry.StepSize = 0.01f;
        }

    }
}
