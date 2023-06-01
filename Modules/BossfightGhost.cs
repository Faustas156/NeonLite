using HarmonyLib;
using System.Reflection;

namespace NeonWhiteQoL.Modules
{
    internal class BossfightGhost
    {
        private static readonly FieldInfo m_dontRecord = typeof(GhostRecorder).GetField("m_dontRecord", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void Initialize()
        {
            MethodInfo method = typeof(GhostRecorder).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            HarmonyMethod harmonyMethod = new (typeof(BossfightGhost).GetMethod("RecordGhost"));
            NeonLite.Harmony.Patch(method, harmonyMethod);

            method = typeof(GhostPlayback).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            harmonyMethod = new (typeof(BossfightGhost).GetMethod("PreLateUpdate"));
            NeonLite.Harmony.Patch(method, harmonyMethod);
        }

        public static bool RecordGhost(GhostRecorder __instance)
        {
            RM.ghostRecorder = __instance;
            if (LevelRush.IsHellRush())
                m_dontRecord.SetValue(__instance, true);
            return false;
        }

        public static bool PreLateUpdate()
        {
            if (NeonLite.BossGhost_recorder.Value)
                return true;

            if (Singleton<Game>.Instance.GetCurrentLevel().isBossFight || LevelRush.IsHellRush())
                return false;

            return true;
        }
    }
}
