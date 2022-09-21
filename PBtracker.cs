using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace NeonWhiteQoL
{
    public class PBtracker
    {
        private static Game game;
        private static string delta = "";
        private static bool newbest;
        public static void Initialize()
        {
            game = Singleton<Game>.Instance;
            MethodInfo method = typeof(Game).GetMethod("OnLevelWin");
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(PBtracker).GetMethod("PreOnLevelWin"));
            NeonLite.Harmony.Patch(method, harmonyMethod);

            method = typeof(MenuScreenResults).GetMethod("OnSetVisible");
            harmonyMethod = new HarmonyMethod(typeof(PBtracker).GetMethod("PostOnSetVisible"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);
        }
        public static bool PreOnLevelWin()
        {
            long besttime, newtime;

            if (game.GetCurrentLevel().levelRushType == LevelRush.LevelRushType.None)
            { // Normal level
                LevelInformation levelInformation = game.GetGameData().GetLevelInformation(game.GetCurrentLevel());
                besttime = GameDataManager.levelStats[levelInformation.levelID].GetTimeBestMicroseconds();
                FieldInfo fi = game.GetType().GetField("_currentPlaythrough", BindingFlags.Instance | BindingFlags.NonPublic);
                LevelPlaythrough currentPlaythrough = (LevelPlaythrough)fi.GetValue(game);
                newtime = currentPlaythrough.GetCurrentTimeMicroseconds();
            }
            else
            { // Level Rush
                LevelRushStats currentLevelRushStats = LevelRush.GetCurrentLevelRush();
                LevelRushData bestLevelRushData = LevelRush.GetLevelRushDataByType(currentLevelRushStats.levelRushType);

                besttime = currentLevelRushStats.heavenRush ? bestLevelRushData.bestTime_HeavenMicroseconds : bestLevelRushData.bestTime_HellMicroseconds;
                newtime = currentLevelRushStats.currentTimerMicroseconds;
            }

            long deltatime = (besttime - newtime) / 1000;
            newbest = deltatime < 0;
            TimeSpan t = TimeSpan.FromMilliseconds((double)Math.Abs(deltatime));
            delta = (newbest ? "+" : "-") + string.Format("{0:0}:{1:00}.{2:000}",
                                                t.Minutes,
                                                t.Seconds,
                                                t.Milliseconds);

            return true;
        }
        public static void PostOnSetVisible()
        {
            GameObject bestText = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Results Panel/New Best Text");
            GameObject deltaTime = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Results Panel/Delta Time");

            if (deltaTime == null)
            {
                deltaTime = UnityEngine.Object.Instantiate(bestText, bestText.transform.parent);
                deltaTime.name = "Delta Time";
                deltaTime.transform.localPosition += new Vector3(-5, -30, 0);
                deltaTime.SetActive(true);
            }
            TextMeshProUGUI text = deltaTime.GetComponent<TextMeshProUGUI>();
            text.SetText(delta);
            text.color = newbest ? Color.red : Color.green;

            if (!LevelRush.IsLevelRush()) return;

            GameObject bestTextRush = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Level Rush Complete Panel/Level Time Text");
            GameObject deltaTimeRush = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Level Rush Complete Panel/Delta Time Rush");

            if (deltaTimeRush == null)
            {
                deltaTimeRush = UnityEngine.Object.Instantiate(bestTextRush, bestTextRush.transform.parent);
                deltaTimeRush.name = "Delta Time Rush";
                deltaTimeRush.transform.localPosition += new Vector3(0, -30, 0);
                deltaTimeRush.SetActive(true);
            }
            text = deltaTimeRush.GetComponent<TextMeshProUGUI>();
            text.SetText(delta);
            text.color = newbest ? Color.red : Color.green;
        }
    }
}