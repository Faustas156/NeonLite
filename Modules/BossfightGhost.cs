using HarmonyLib;
using MelonLoader;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class BossfightGhost : Module
    {
        private static MelonPreferences_Entry<bool> _setting_GhostAnywere;

        public BossfightGhost() =>
            _setting_GhostAnywere = NeonLite.Config_NeonLite.CreateEntry("Boss Recorder", true, description: "Allows you to record and playback a ghost for the boss levels.");


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GhostRecorder), "Start")]
        private static void PostStart(ref bool ___m_dontRecord)
        {
            if (_setting_GhostAnywere.Value)
                ___m_dontRecord = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GhostPlayback), "Start")]
        private static bool PreStart()
        {
            if (Singleton<Game>.Instance.GetCurrentLevel().isBossFight | LevelRush.IsHellRush())
                return _setting_GhostAnywere.Value;
            return true;
        }
    }
}
