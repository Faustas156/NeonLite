using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace NeonWhiteQoL
{
    public class PBtracker
    {
        private static Game game;
        private static string delta = string.Empty;
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
            if (LevelRush.IsLevelRush()) return true;
            delta = GetDeltaTimeString(false);
            return true;
        }

        public static void PostOnSetVisible()
        {
            bool isLevelRush = LevelRush.IsLevelRush();
            if (delta != string.Empty)
                delta = GetDeltaTimeString(true);

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

            if (!isLevelRush) return;

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

            delta = string.Empty;
        }
        private static string GetDeltaTimeString(bool isLevelRush)
        {
            long besttime, newtime;

            if (!isLevelRush)
            { // Normal level
                LevelInformation levelInformation = game.GetGameData().GetLevelInformation(game.GetCurrentLevel());
                besttime = GameDataManager.levelStats[levelInformation.levelID].GetTimeBestMicroseconds();
                FieldInfo fi = game.GetType().GetField("_currentPlaythrough", BindingFlags.Instance | BindingFlags.NonPublic);
                LevelPlaythrough currentPlaythrough = (LevelPlaythrough)fi.GetValue(game);
                newtime = currentPlaythrough.GetCurrentTimeMicroseconds();
            }
            else
            { // Level Rush
                LevelRushData bestLevelRushData = LevelRush.GetLevelRushDataByType(LevelRush.GetCurrentLevelRushType());

                besttime = LevelRush.IsHellRush() ? bestLevelRushData.bestTime_HellMicroseconds : bestLevelRushData.bestTime_HeavenMicroseconds;
                newtime = LevelRush.GetCurrentLevelRushTimerMicroseconds();
            }

            long deltatime = (besttime - newtime) / 1000;
            newbest = deltatime < 0;
            TimeSpan t = TimeSpan.FromMilliseconds(Math.Abs(deltatime));

            return (newbest ? "+" : "-") + string.Format("{0:0}:{1:00}.{2:000}",
                                                t.Minutes,
                                                t.Seconds,
                                                t.Milliseconds);
        }
    }
}