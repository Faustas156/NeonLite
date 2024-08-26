using HarmonyLib;
using System.Reflection;


namespace NeonLite.Modules.Misc
{
    [HarmonyPatch(typeof(IntroCards))]
    internal class SkipIntro : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "noIntro", "Skip Intro", "Never hear the fabled \"We're called neons.\" speech when you start up your game.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        // static readonly MethodInfo original = AccessTools.Method(typeof(IntroCards), "Start");
        static void Activate(bool activate)
        {
            /*
            if (activate)
                NeonLite.Harmony.Patch(original, prefix: Helpers.HM(SkipIntroCards));
            else
                NeonLite.Harmony.Unpatch(original, Helpers.MI(SkipIntroCards));//*/

            active = activate;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        static void SkipIntroCards(ref int ___m_state) => ___m_state = active ? 2 : ___m_state;
    }
}
