using HarmonyLib;


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
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        // static readonly MethodInfo original = AccessTools.Method(typeof(IntroCards), "Start");
        static void Activate(bool activate)
        {
            /*
            if (activate)
                Patching.AddPatch(original, SkipIntroCards, Patching.PatchTarget.Prefix);
            else
                Patching.RemovePatch(original, SkipIntroCards);//*/

            active = activate;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        static void SkipIntroCards(ref int ___m_state) => ___m_state = active ? 2 : ___m_state;
    }
}
