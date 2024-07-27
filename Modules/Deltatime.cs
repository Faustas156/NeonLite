using HarmonyLib;
using MelonLoader;
using TMPro;
using UnityEngine;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class Deltatime : Module
    {
        private static GameObject s_deltaTime, s_deltaTimeRush;
        private static MelonPreferences_Entry<bool> _setting_Deltatime;

        private static long _oldPB = -1;

        public Deltatime() =>
            _setting_Deltatime = NeonLite.Config_NeonLite.CreateEntry("Deltatime", true, description: "Displays a time based on whether or not you got a new personal best.");



        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), "OnLevelWin")]
        public static void PreUpdateTimeMicroseconds()
        {
            if (!_setting_Deltatime.Value) return;
            LevelStats levelStats = GameDataManager.levelStats[NeonLite.Game.GetCurrentLevel().levelID];
            LevelRushData bestLevelRushData = LevelRush.GetLevelRushDataByType(LevelRush.GetCurrentLevelRushType());
            _oldPB = LevelRush.IsLevelRush() ? (LevelRush.IsHellRush() ? bestLevelRushData.bestTime_HellMicroseconds : bestLevelRushData.bestTime_HeavenMicroseconds) : levelStats.GetTimeBestMicroseconds();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuScreenResults), "OnSetVisible")]
        public static void PostSetVisible()
        {
            if (!_setting_Deltatime.Value) return;

            bool isLevelRush = LevelRush.IsLevelRush();
            long newTime = isLevelRush ? LevelRush.GetCurrentLevelRushTimerMicroseconds() : NeonLite.Game.GetCurrentLevelTimerMicroseconds();
            long bestTime = _oldPB;

            long delta = (bestTime - newTime) / 1000;
            bool newBest = delta < 0;
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(Math.Abs(delta));

            string deltaTimeString = (newBest ? "+" : "-") + string.Format("{0:0}:{1:00}.{2:000}",
                                                timeSpan.Minutes,
                                                timeSpan.Seconds,
                                                timeSpan.Milliseconds);

            Debug.Log(bestTime + "   " + newTime);

            TextMeshProUGUI text;
            GameObject levelTimeObject;

            if (!isLevelRush)
            {
                if (s_deltaTime == null)
                {
                    levelTimeObject = ((MenuScreenResults)MainMenu.Instance()._screenResults)._resultsScreenNewBestTimeIndicator;
                    s_deltaTime = UnityEngine.Object.Instantiate(levelTimeObject, levelTimeObject.transform.parent);
                    s_deltaTime.name = "Delta Time";
                    s_deltaTime.transform.localPosition += new Vector3(-5, -30, 0);
                    s_deltaTime.SetActive(true);
                }

                text = s_deltaTime.GetComponent<TextMeshProUGUI>();
                text.SetText(deltaTimeString);
                text.color = newBest ? Color.red : Color.green;
                return;
            }

            if (s_deltaTimeRush == null)
            {
                levelTimeObject = MainMenu.Instance()._screenLevelRushComplete.timeText.gameObject;
                s_deltaTimeRush = UnityEngine.Object.Instantiate(levelTimeObject, levelTimeObject.transform.parent);
                s_deltaTimeRush.name = "Delta Time Rush";
                s_deltaTimeRush.transform.localPosition += new Vector3(0, -30, 0);
                s_deltaTimeRush.SetActive(true);
            }
            text = s_deltaTimeRush.GetComponent<TextMeshProUGUI>();
            text.SetText(deltaTimeString);
            text.color = newBest ? Color.red : Color.green;
        }
    }
}