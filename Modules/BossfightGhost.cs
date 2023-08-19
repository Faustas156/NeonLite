using HarmonyLib;
using MelonLoader;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class BossfightGhost : Module
    {
        private static MelonPreferences_Entry<bool> BossGhost_recorder;

        public BossfightGhost() =>
            BossGhost_recorder = NeonLite.neonLite_config.CreateEntry("Boss Recorder", true, description: "Allows you to record and playback a ghost for the boss levels.");


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GhostRecorder), "Start")]
        private static void PostStart(ref bool ___m_dontRecord)
        {
            if (BossGhost_recorder.Value)
                ___m_dontRecord = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GhostPlayback), "Start")]
        private static bool PreStart()
        {
            if (Singleton<Game>.Instance.GetCurrentLevel().isBossFight || LevelRush.IsHellRush())
                return BossGhost_recorder.Value;
            return true;
        }
    }
}
