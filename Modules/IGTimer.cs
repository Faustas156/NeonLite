using HarmonyLib;
using MelonLoader;
using System.Text;
using UnityEngine;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class IGTimer : Module
    {
        private static MelonPreferences_Entry<bool> _setting_IGTimer;
        private static MelonPreferences_Entry<Color> _setting_IGTimer_Color;


        private static readonly StringBuilder timerBuilder = new();

        public IGTimer()
        {
            _setting_IGTimer = NeonLite.Config_NeonLite.CreateEntry("Display in-depth in-game timer", true, description: "Allows the modification of the timer and lets you display milliseconds.");
            _setting_IGTimer_Color = NeonLite.Config_NeonLite.CreateEntry("In-game Timer Color", Color.white, description: "Customization settings for the in-game timer, does not apply to result screen time.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuScreenResults), "OnSetVisible")]
        private static void PostOnSetVisible(ref MenuScreenResults __instance)
        {
            if (!_setting_IGTimer.Value) return;

            long millisecondTimer = NeonLite.Game.GetCurrentLevelTimerMicroseconds() / 1000;
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(millisecondTimer);

            string resulttime = string.Format("{0:0}:{1:00}.{2:000}",
                                                timeSpan.Minutes,
                                                timeSpan.Seconds,
                                                timeSpan.Milliseconds);

            __instance._resultsScreenLevelTime.SetText(resulttime);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuScreenLevelRushComplete), "OnSetVisible")]
        private static void PostOnSetVisible(ref MenuScreenLevelRushComplete __instance)
        {
            if (!_setting_IGTimer.Value) return;

            long millisecondTimer = NeonLite.Game.GetCurrentLevelTimerMicroseconds() / 1000;
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(millisecondTimer);

            string resulttime = string.Format("{0:0}:{1:00}.{2:000}",
                                                timeSpan.Minutes,
                                                timeSpan.Seconds,
                                                timeSpan.Milliseconds);

            __instance.timeText.SetText(resulttime);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerUI), "UpdateTimerText")]
        private static bool PreUpdateTimerText(ref PlayerUI __instance)
        {
            if (!_setting_IGTimer.Value) return true;

            __instance.timerText.color = _setting_IGTimer_Color.Value;
            __instance.timerText.text = CreateTimerText();
            return false;
        }

        public static string CreateTimerText()
        {
            if (_setting_IGTimer.Value)
            {
                long currentLevelTimerMilliseconds = NeonLite.Game.GetCurrentLevelTimerMicroseconds() / 1000;
                timerBuilder.Clear();

                int num = (int)(currentLevelTimerMilliseconds / 60000L);
                int num2 = (int)(currentLevelTimerMilliseconds / 1000L) % 60;
                int num3 = (int)(currentLevelTimerMilliseconds - (num * 60000) - (num2 * 1000));

                if (num > 99)
                    timerBuilder.Append((char)((num / 100) + 48));

                timerBuilder.Append((char)((num / 10) + 48));
                timerBuilder.Append((char)((num % 10) + 48));
                timerBuilder.Append(':');
                timerBuilder.Append((char)((num2 / 10) + 48));
                timerBuilder.Append((char)((num2 % 10) + 48));
                timerBuilder.Append(':');
                timerBuilder.Append((char)((num3 / 100) + 48));
                num3 %= 100;
                timerBuilder.Append((char)((num3 / 10) + 48));
                timerBuilder.Append((char)((num3 % 10) + 48));

                return timerBuilder.ToString();
            }
            else
            {
                // copy of the OG, just don't wanna fetch it via reflection bc that's ugly
                long currentLevelTimerCentiseconds = Singleton<Game>.Instance.GetCurrentLevelTimerCentiseconds();
                timerBuilder.Clear();

                int num = (int)(currentLevelTimerCentiseconds / 6000);
                int num2 = (int)(currentLevelTimerCentiseconds / 100) % 60;
                int num3 = (int)(currentLevelTimerCentiseconds - num * 6000 - num2 * 100);

                if (num > 99)
                    timerBuilder.Append((char)(num / 100 + 48));

                timerBuilder.Append((char)(num / 10 + 48));
                timerBuilder.Append((char)(num % 10 + 48));
                timerBuilder.Append(':');
                timerBuilder.Append((char)(num2 / 10 + 48));
                timerBuilder.Append((char)(num2 % 10 + 48));
                timerBuilder.Append(':');
                timerBuilder.Append((char)(num3 / 10 + 48));
                timerBuilder.Append((char)(num3 % 10 + 48));

                return timerBuilder.ToString();
            }
        }
    }
}