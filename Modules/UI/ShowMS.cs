using HarmonyLib;
using MelonLoader;
using System;
using System.Reflection;
using System.Text;

namespace NeonLite.Modules.UI
{
    internal class ShowMS : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        public static MelonPreferences_Entry<bool> setting;
        public static MelonPreferences_Entry<bool> extended;

        public static bool forceAll = false;

        static void Setup()
        {
            setting = Settings.Add(Settings.h, "UI", "showMS", "Show full milliseconds in-game", "Show all 3 digits of milliseconds for the timer and finish.", true);
            extended = Settings.Add(Settings.h, "UI", "showMore", "Show full milliseconds everywhere", "Show all 3 digits of milliseconds everywhere applicable.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo ogtt = AccessTools.Method(typeof(PlayerUI), "UpdateTimerText");
        static readonly MethodInfo ogrush = AccessTools.Method(typeof(MenuScreenLevelRushComplete), "OnSetVisible");
        static readonly MethodInfo oglevel = AccessTools.Method(typeof(MenuScreenResults), "OnSetVisible");
        static readonly MethodInfo oggtf = AccessTools.Method(typeof(Game), "GetTimerFormatted");
        static readonly MethodInfo ogwin = AccessTools.Method(typeof(Game), "OnLevelWin");


        static void Activate(bool activate)
        {
            if (activate)
            {
                NeonLite.Harmony.Patch(ogtt, prefix: Helpers.HM(OnTimerUpdate));
                NeonLite.Harmony.Patch(ogrush, postfix: Helpers.HM(OnRushFinish));
                NeonLite.Harmony.Patch(oglevel, postfix: Helpers.HM(OnLevelFinish));
                NeonLite.Harmony.Patch(oggtf, prefix: Helpers.HM(GetTimerFormatted));
                NeonLite.Harmony.Patch(ogwin, prefix: Helpers.HM(OnWin));
            }
            else
            {
                NeonLite.Harmony.Unpatch(ogtt, Helpers.MI(OnTimerUpdate));
                NeonLite.Harmony.Unpatch(ogrush, Helpers.MI(OnRushFinish));
                NeonLite.Harmony.Unpatch(oglevel, Helpers.MI(OnLevelFinish));
                NeonLite.Harmony.Unpatch(oggtf, Helpers.MI(GetTimerFormatted));
                NeonLite.Harmony.Unpatch(ogwin, Helpers.MI(OnWin));
            }

            active = activate;
        }

        static void OnLevelLoad(LevelData _) => forceAll = false;


        static readonly StringBuilder timerBuilder = new();
        public static string FormatTimeNoArgs(long timeMS)
        {
            // minimal (but still advanced) ver of Helpers.FormatTime
            timerBuilder.Clear();
            int num = (int)(timeMS / 60000L);
            int num2 = (int)(timeMS / 1000L) % 60;
            int num3 = (int)(timeMS - (num * 60000L) - (num2 * 1000L));

            if (num >= 100)
                for (int i = (int)Math.Log10(num); i >= 2; --i)
                    timerBuilder.Append((char)(num / (int)Math.Pow(10, i) % 10 + '0'));
            
            num %= 100;
            timerBuilder.Append((char)((num / 10) + '0'));
            timerBuilder.Append((char)((num % 10) + '0'));

            timerBuilder.Append(':');
            timerBuilder.Append((char)((num2 / 10) + '0'));
            timerBuilder.Append((char)((num2 % 10) + '0'));
            timerBuilder.Append(':');
            timerBuilder.Append((char)((num3 / 100) + '0'));
            num3 %= 100;
            timerBuilder.Append((char)((num3 / 10) + '0'));
            timerBuilder.Append((char)((num3 % 10) + '0'));

            return timerBuilder.ToString();
        }

        static bool OnTimerUpdate(ref PlayerUI __instance)
        {
            __instance.timerText.text = FormatTimeNoArgs(NeonLite.Game.GetCurrentLevelTimerMicroseconds() / 1000);
            return false;
        }
        static void OnRushFinish(ref MenuScreenLevelRushComplete __instance) => __instance.timeText.SetText(Helpers.FormatTime(NeonLite.Game.GetCurrentLevelTimerMicroseconds() / 1000, true, '.', true));
        static void OnLevelFinish(ref MenuScreenResults __instance) => __instance._resultsScreenLevelTime.SetText(Helpers.FormatTime(NeonLite.Game.GetCurrentLevelTimerMicroseconds() / 1000, true, '.', true));
        static bool GetTimerFormatted(long timeInMicroSeconds, ref string __result)
        {
            __result = Helpers.FormatTime(timeInMicroSeconds / 1000, forceAll || extended.Value, '.', true);
            return false;
        }
        static void OnWin(LevelData ____currentLevel, LevelPlaythrough ____currentPlaythrough) => forceAll = ____currentPlaythrough.GetCurrentTimeMicroseconds() < GameDataManager.levelStats[____currentLevel.levelID].GetTimeBestMicroseconds();
    }
}
