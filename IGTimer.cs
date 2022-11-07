using HarmonyLib;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

namespace NeonWhiteQoL
{
    internal class IGTimer
    {
        private static Game game;
        public static string resulttime = "";
        public static void Initialize()
        {
            game = Singleton<Game>.Instance;

            MethodInfo method = typeof(MenuScreenResults).GetMethod("OnSetVisible");
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(IGTimer).GetMethod("PostOnSetVisible"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);

            method = typeof(PlayerUI).GetMethod("UpdateTimerText", BindingFlags.NonPublic | BindingFlags.Instance);
            harmonyMethod = new HarmonyMethod(typeof(IGTimer).GetMethod("PreUpdateTimerText"));
            NeonLite.Harmony.Patch(method, harmonyMethod);
        }

        public static void PostOnSetVisible()
        {
            if (!NeonLite.IGTimer_display.Value)
                return;

            long microsecondTimer;

            FieldInfo fi = game.GetType().GetField("_currentPlaythrough", BindingFlags.Instance | BindingFlags.NonPublic);
            LevelPlaythrough currentPlaythrough = (LevelPlaythrough)fi.GetValue(game);
            microsecondTimer = currentPlaythrough.GetCurrentTimeMicroseconds();


            long millisecondTimer = microsecondTimer / 1000;

            TimeSpan t = TimeSpan.FromMilliseconds(millisecondTimer);

            resulttime = String.Format("{0:0}:{1:00}.{2:000}",
                                                t.Minutes,
                                                t.Seconds,
                                                t.Milliseconds);

            GameObject centiseconds = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Results Panel/Level Time Text");
            //GameObject milliseconds = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Results Panel/Level Milliseconds Text");

            //if (milliseconds == null)
            //{
            //    milliseconds = UnityEngine.Object.Instantiate(centiseconds, centiseconds.transform.parent);
            //    milliseconds.name = "Level Milliseconds Text";
            //    milliseconds.SetActive(true);
            //    milliseconds.transform.localPosition += new Vector3(0, 0, 0);
            //}
            //TextMeshProUGUI text = milliseconds.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI text = centiseconds.GetComponent<TextMeshProUGUI>();
            //centiseconds.SetActive(false);
            text.SetText(resulttime);
            LevelStats levelStats = GameDataManager.levelStats[game.GetCurrentLevel().levelID];
            text.color = levelStats.IsNewBest() ? Color.green : Color.white;


        }
        public static bool PreUpdateTimerText(PlayerUI __instance)
        {
            if (!NeonLite.IGTimer_display.Value)
                return true;

            FieldInfo timerBuilderInfo = typeof(PlayerUI).GetField("timerBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
            StringBuilder timerBuilder = (StringBuilder)timerBuilderInfo.GetValue(__instance);

            long currentLevelTimerMilliseconds = Singleton<Game>.Instance.GetCurrentLevelTimerMicroseconds() / 1000;
            int num = (int)(currentLevelTimerMilliseconds / 60000L);
            int num2 = (int)(currentLevelTimerMilliseconds / 1000L) % 60;
            int num3 = (int)(currentLevelTimerMilliseconds - (long)(num * 60000) - (long)(num2 * 1000));
            timerBuilder.Clear();
            if (num > 99)
            {
                timerBuilder.Append((char)(num / 100 + 48));
            }
            timerBuilder.Append((char)(num / 10 + 48));
            timerBuilder.Append((char)(num % 10 + 48));
            timerBuilder.Append(':');
            timerBuilder.Append((char)(num2 / 10 + 48));
            timerBuilder.Append((char)(num2 % 10 + 48));
            timerBuilder.Append(':');
            timerBuilder.Append((char)(num3 / 100 + 48));
            num3 %= 100;
            timerBuilder.Append((char)(num3 / 10 + 48));
            timerBuilder.Append((char)(num3 % 10 + 48));
            __instance.timerText.text = timerBuilder.ToString();

            return false;
        }

        //public static void OnPostUpdateTimerText(PlayerUI __instance)
        //{
        //    if (!NeonLite.IGTimer_display.Value)
        //        return;

        //    if ()
        //}
    }
}
