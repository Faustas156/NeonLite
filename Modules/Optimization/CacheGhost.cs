using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace NeonLite.Modules.Optimization
{
    internal class CacheGhost : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static readonly Dictionary<string, GhostSave> ghosts = [];

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "cacheGhosts", "Cache Ghosts", "Caches the ghosts so they don't have to reload every restart.\nDisable if you're messing with ghosts manually.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo ogload = AccessTools.Method(typeof(GhostUtils), "LoadLevelDataCompressed");
#if XBOX
        static readonly MethodInfo ogsave = AccessTools.Method(typeof(GhostRecorder), "SaveCompressedInternalAsync");
#else
        static readonly MethodInfo ogsave = AccessTools.Method(typeof(GhostRecorder), "SaveCompressedInternal");
#endif
        static readonly MethodInfo deserialize = AccessTools.Method(typeof(GhostUtils), "DeserializeLevelDataCompressed");

        static void Activate(bool activate)
        {
            if (activate)
            {
                NeonLite.Harmony.Patch(ogload, prefix: Helpers.HM(LoadDataRewrite));
                NeonLite.Harmony.Patch(ogsave, prefix: Helpers.HM(OnGhostSave));
            }
            else
            {
                ghosts.Clear();
                NeonLite.Harmony.Unpatch(ogload, Helpers.MI(LoadDataRewrite));
                NeonLite.Harmony.Unpatch(ogsave, Helpers.MI(OnGhostSave));
            }

            active = activate;
        }

        static bool LoadDataRewrite(ref GhostSave ghostSave, GhostUtils.GhostType ghostType, ulong saveId, Action callback)
        {
            string path = "";
            if (!GhostUtils.GetPath(ghostType, ref path))
                return false;
            path += Path.DirectorySeparatorChar.ToString() + saveId.ToString() + ".phant";

            if (ghosts.ContainsKey(path))
            {
                ghostSave = ghosts[path];
                callback?.Invoke();
                return false;
            }

            ghostSave = new();

            if (!File.Exists(path))
            {
                callback?.Invoke();
                return false;
            }

            try
            {
                deserialize.Invoke(null, [ghostSave, File.ReadAllText(path), callback]);
                ghosts.Add(path, ghostSave);
            }
            catch
            {
                File.Delete(path);
            }

            callback?.Invoke();
            return false;
        }
        static void OnGhostSave(string path) => ghosts.Remove(path);
    }
}
