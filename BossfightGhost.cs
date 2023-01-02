using HarmonyLib;
using System.Reflection;

namespace NeonWhiteQoL
{
    internal class BossfightGhost
    {
        private static readonly FieldInfo m_dontRecord = typeof(GhostRecorder).GetField("m_dontRecord", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void Initialize()
        {
            MethodInfo method = typeof(GhostRecorder).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(BossfightGhost).GetMethod("RecordGhost"));
            NeonLite.Harmony.Patch(method, harmonyMethod);

            method = typeof(GhostPlayback).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            harmonyMethod = new HarmonyMethod(typeof(BossfightGhost).GetMethod("PreLateUpdate"));
            NeonLite.Harmony.Patch(method, harmonyMethod);
        }

        public static bool RecordGhost(GhostRecorder __instance)
        {
            RM.ghostRecorder = __instance;
            if (LevelRush.IsHellRush())
                m_dontRecord.SetValue(__instance, true);
            return false;
        }

        public static bool PreLateUpdate(GhostPlayback __instance)
        {
            if (NeonLite.BossGhost_recorder.Value)
                return true;

            if (Singleton<Game>.Instance.GetCurrentLevel().isBossFight || LevelRush.IsHellRush())
                return false;

            return true;
        }
    }
}
