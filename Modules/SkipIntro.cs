using HarmonyLib;
using MelonLoader;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class SkipIntro : Module
    {
        private static MelonPreferences_Entry<bool> skipintro_enabler;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(IntroCards), "Start")]
        private static void SkipIntroCards(ref int ___m_state)
        {
            //Must be assigned here because intro is starting too early
            skipintro_enabler = NeonLite.neonLite_config.CreateEntry("Disable Intro", true, description: "Never hear the fabled \"We're called neons.\" speech when you start up your game. (REQUIRES RESTART)");

            if (skipintro_enabler.Value)
                ___m_state = 2;
        }
    }
}
