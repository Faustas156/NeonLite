#pragma warning disable IDE0130
using System.Runtime.CompilerServices;
using MelonLoader;
using NeonLite.Modules.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NeonLite.Modules
{
    [Module]
    public static class PastSight
    {
        const bool priority = false;
        const bool active = true;

        static float timer = 1;
        public static float Opacity => AxKEasing.EaseInCubic(0, 1, Math.Abs(timer));

        public static bool Active => timer < 0;

        public static event Action<bool> OnActive;

        static MelonPreferences_Entry<Key> key;

        static ConditionalWeakTable<LevelStats, LevelStats> preWins = new();
        internal static readonly Dictionary<object, Extra> registered = [];

        static void Setup()
        {
            key = Settings.Add(Settings.h, "UI", "pastSight", "Past Sight Keybind",
                "Holding this key down will let you see the last run's time instead of your PB in places where you would normally see it.", Key.RightAlt);
        }

        static void Activate(bool _)
        {
            NeonLite.holder.AddComponent<Fader>();

            Patching.AddPatch(typeof(LevelInfo), "SetLevel", PreSetLevelInfo, Patching.PatchTarget.Prefix);
            Patching.AddPatch(typeof(LevelInfo), "SetLevel", PostSetLevelInfo, Patching.PatchTarget.Postfix);

            Patching.AddPatch(typeof(Game), "OnLevelWin", PreWin, Patching.PatchTarget.Prefix);
        }

        public static long GetTimePastSight(this LevelStats stats)
        {
            if (Active)
                return stats.GetTimeLastMicroseconds();
            return stats.GetTimeBestMicroseconds();
        }

        public static long GetTimePastSight(this LevelStats stats, bool preWinIfActive)
        {
            if (preWinIfActive && Active)
                return stats.GetPreWinStats().GetTimeLastMicroseconds();
            return stats.GetTimePastSight();
        }

        // Will return the existing stats if there's no pre-win stats.
        public static LevelStats GetPreWinStats(this LevelStats stats)
        {
            if (preWins.TryGetValue(stats, out var ret))
                return ret;
            return stats;
        }

        static void PreWin()
        {
            var stats = GameDataManager.GetLevelStats(NeonLite.Game.GetCurrentLevel().levelID);
            if (stats == null)
                return;

            var prev = preWins.GetOrCreateValue(stats);
            prev._timeLastMicroseconds = stats._timeLastMicroseconds;
            prev._timeBestMicroseconds = stats._timeBestMicroseconds;
            prev._newBest = stats._newBest;
        }

        static void OnLevelLoad(LevelData level)
        {
            if (!level)
                return;

            var stats = GameDataManager.GetLevelStats(level.levelID);
            if (stats == null)
                return;

            if (preWins.TryGetValue(stats, out var prev))
            {
                prev._timeLastMicroseconds = stats._timeLastMicroseconds;
                prev._timeBestMicroseconds = stats._timeBestMicroseconds;
                prev._newBest = stats._newBest;
            }
        }

        internal class Extra
        {
            public LevelData level;
        }

        class LevelInfoExtra : Extra
        {
            public bool isNew;
            public string bestTimeKey;
        }

        static void PreSetLevelInfo(LevelInfo __instance, LevelData level, bool isNewScore)
        {
            if (!level)
                return;

            if (!registered.TryGetValue(__instance, out var extr))
            {
                extr = new LevelInfoExtra();
                registered.Add(__instance, extr);
            }
            var extra = extr as LevelInfoExtra;
            extra.level = level;
            extra.isNew = isNewScore;
        }
        static void PostSetLevelInfo(LevelInfo __instance, LevelData level)
        {
            if (!level)
                return;

            if (!registered.TryGetValue(__instance, out var extr))
            {
                extr = new LevelInfoExtra();
                registered.Add(__instance, extr);
            }
            var extra = extr as LevelInfoExtra;

            const string prevtime = "NeonLite/LEVELINFO_TIME_PREVIOUS";

            var stats = GameDataManager.GetLevelStats(level.levelID);
            if (stats == null)
                return;

            if (__instance._levelBestTimeDescription_Localized.localizationKey != prevtime)
                extra.bestTimeKey = __instance._levelBestTimeDescription_Localized.localizationKey;

            __instance._levelBestTime.text = Helpers.FormatTime(stats.GetTimePastSight(true) / 1000, split: '.', cutoff: true);
            if (Active)
                __instance._levelBestTimeDescription_Localized.SetKey(prevtime);
            else if (__instance._levelBestTimeDescription_Localized.localizationKey != extra.bestTimeKey)
                __instance._levelBestTimeDescription_Localized.SetKey(extra.bestTimeKey);

            // ensure alpha

            __instance._levelBestTimeDescription.alpha = Opacity;
            __instance._levelBestTime.alpha = Opacity;

            if (!level.isSidequest)
                __instance._levelMedal.SetAlpha(Opacity);
            else
                __instance._crystalHolderFilledImage.SetAlpha(Opacity);

        }

        class Fader : MonoBehaviour
        {
            const float SPEED = 0.25f;

            void Update()
            {
                var sign = Math.Sign(timer);
                bool pressed = false;
                if (Keyboard.current != null && Keyboard.current[key.Value].isPressed)
                    pressed = true;

                if (pressed)
                {
                    timer -= Time.unscaledDeltaTime / SPEED;
                    if (timer < -1)
                        timer = -1;
                }
                else
                {
                    timer += Time.unscaledDeltaTime / SPEED;
                    if (timer > 1)
                        timer = 1;
                }

                SetOpacity();
                if (sign != Math.Sign(timer))
                    OnFlip();
            }

            static void SetOpacity()
            {
                foreach (var kv in registered)
                {
                    if (kv.Key is LevelInfo levelInfo)
                    {
                        levelInfo._levelBestTimeDescription.alpha = Opacity;
                        levelInfo._levelBestTime.alpha = Opacity;

                        if (!kv.Value.level.isSidequest)
                            levelInfo._levelMedal.SetAlpha(Opacity);
                        else
                            levelInfo._crystalHolderFilledImage.SetAlpha(Opacity);
                    }
                    else if (kv.Key is Deltatime dt)
                    {
                        dt.text.alpha = Opacity;
                    }
                }
            }

            static void OnFlip()
            {
                OnActive?.Invoke(Active);
                foreach (var kv in registered)
                {
                    if (kv.Key is LevelInfo levelInfo)
                    {
                        var extra = kv.Value as LevelInfoExtra;
                        levelInfo.SetLevel(extra.level, false, extra.isNew, true);
                    }
                }
            }
        }
    }
}
