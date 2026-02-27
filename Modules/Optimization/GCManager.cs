using UnityEngine.Scripting;

namespace NeonLite.Modules.Optimization
{
    [Module]
    internal static class GCManager
    {
        const bool priority = true;
        const bool active = true;

        public enum GCType
        {
            Initialization,
            Patching,
            FastStart,
            FastStartAudio,
            SuperRestart,

            Count
        }

        static readonly bool[] gcs = new bool[(int)GCType.Count];

        static void Setup()
        {
            GarbageCollector.GCModeChanged += OnGCMode;
        }

        static void OnGCMode(GarbageCollector.Mode mode)
        {
            NeonLite.Logger.DebugMsg($"GCMODE SET {mode}");
            if (mode == GarbageCollector.Mode.Enabled && gcs.Any(static x => x))
                GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
        }

        public static void DisableGC(GCType type)
        {
            NeonLite.Logger.DebugMsg($"GCMODE DISABLE {type}");

            gcs[(int)type] = true;

            // gcs.any is now true

            GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
        }

        public static void EnableGC(GCType type)
        {
            NeonLite.Logger.DebugMsg($"GCMODE ENABLE {type}");

            gcs[(int)type] = false;

            if (gcs.All(static x => !x))
                GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
        }
    }
}
