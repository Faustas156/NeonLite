using HarmonyLib;
using MelonLoader;
using System.Text;
using UnityEngine;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class IGTimer : Module
    {
        private static MelonPreferences_Entry<bool> IGTimer_display;
        private static MelonPreferences_Entry<Color> IGTimer_color;


        private static readonly StringBuilder timerBuilder = new();

        public IGTimer()
        {
            IGTimer_display = NeonLite.neonLite_config.CreateEntry("Display in-depth in-game timer", true, description: "Allows the modification of the timer and lets you display milliseconds.");
            IGTimer_color = NeonLite.neonLite_config.CreateEntry("In-game Timer Color", Color.white, description: "Customization settings for the in-game timer, does not apply to result screen time.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuScreenResults), "OnSetVisible")]
        private static void PostOnSetVisible(ref MenuScreenResults __instance)
        {
            if (!IGTimer_display.Value) return;

            long millisecondTimer = NeonLite.Game.GetCurrentLevelTimerMicroseconds() / 1000;
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(millisecondTimer);

            string resulttime = string.Format("{0:0}:{1:00}.{2:000}",
                                                timeSpan.Minutes,
                                                timeSpan.Seconds,
                                                timeSpan.Milliseconds);

            __instance._resultsScreenLevelTime.SetText(resulttime);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerUI), "UpdateTimerText")]
        private static bool PreUpdateTimerText(ref PlayerUI __instance)
        {
            if (!IGTimer_display.Value) return true;

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

            __instance.timerText.color = IGTimer_color.Value;
            __instance.timerText.text = timerBuilder.ToString();
            return false;
        }
    }
}
