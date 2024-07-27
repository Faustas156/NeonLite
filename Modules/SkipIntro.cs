using HarmonyLib;
using MelonLoader;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class SkipIntro : Module
    {
        private static MelonPreferences_Entry<bool> _setting_SkipIntro;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(IntroCards), "Start")]
        private static void SkipIntroCards(ref int ___m_state)
        {
            //Must be assigned here because intro is starting too early
            _setting_SkipIntro = NeonLite.Config_NeonLite.CreateEntry("Disable Intro", true, description: "Never hear the fabled \"We're called neons.\" speech when you start up your game. (REQUIRES RESTART)");

            if (_setting_SkipIntro.Value)
                ___m_state = 2;
        }
    }
}
