using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace NeonWhiteQoL.Modules
{
    public class PBtracker
    {
        private static TextMeshProUGUI text = null;
        private static string delta = string.Empty;
        private static bool newBest = false;

        private static string NEWBEST_TEXT_PATH = "Main Menu/Canvas/Ingame Menu/Menu Holder/Results Panel/New Best Text";
        private static string DELTATIME_PATH = "Main Menu/Canvas/Ingame Menu/Menu Holder/Results Panel/Delta Time";

        private static string NEWBEST_RUSH_PATH = "Main Menu/Canvas/Ingame Menu/Menu Holder/Level Rush Complete Panel/Level Time Text";
        private static string DELTATIME_RUSH_PATH = "Main Menu/Canvas/Ingame Menu/Menu Holder/Level Rush Complete Panel/Delta Time Rush";

        private static readonly FieldInfo _currentPlaythrough = typeof(Game).GetField("_currentPlaythrough", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Initialize()
        {
            MethodInfo method = typeof(Game).GetMethod("OnLevelWin");
            HarmonyMethod harmonyMethod = new (typeof(PBtracker).GetMethod("PreOnLevelWin"));
            NeonLite.Harmony.Patch(method, harmonyMethod);

            method = typeof(MenuScreenResults).GetMethod("OnSetVisible");
            harmonyMethod = new (typeof(PBtracker).GetMethod("PostOnSetVisible"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);

            method = typeof(MenuScreenResults).GetMethod("OnSetInvisible");
            harmonyMethod = new(typeof(PBtracker).GetMethod("Destroy"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);
        }

        public static bool PreOnLevelWin()
        {
            if (!NeonLite.PBtracker_display.Value)
                return true;

            if (LevelRush.IsLevelRush()) return true;
            delta = GetDeltaTimeString();
            return true;
        }

        public static void PostOnSetVisible()
        {
            if (!NeonLite.PBtracker_display.Value)
                return;

            int yPos = newBest ? -30 : -30;
            int xPos = LevelRush.IsLevelRush() ? -5 : 0;

            Color color = newBest ? Color.red : Color.green;

            var bestText = GameObject.Find(NEWBEST_TEXT_PATH);
            var deltaTime = GameObject.Find(DELTATIME_PATH);

            if (LevelRush.IsLevelRush())
            {
                bestText = GameObject.Find(NEWBEST_RUSH_PATH);
                deltaTime = GameObject.Find(DELTATIME_RUSH_PATH);
            }

            deltaTime = UnityEngine.Object.Instantiate(bestText, bestText.transform.parent);
            deltaTime.transform.localPosition += new Vector3(xPos, yPos, 0);
            deltaTime.SetActive(true);

            text = deltaTime.GetComponent<TextMeshProUGUI>();
            text.SetText(delta);
            text.color = color;
        }
    
        private static string GetDeltaTimeString()
        {
            var game = Singleton<Game>.Instance;

            long bestTime, newTime;

            if (!LevelRush.IsLevelRush())
            { // Normal level, nvm both are fucking broken idk what happened
                LevelInformation levelInformation = game.GetGameData().GetLevelInformation(game.GetCurrentLevel());
                bestTime = GameDataManager.levelStats[levelInformation.levelID].GetTimeBestMicroseconds();
                LevelPlaythrough currentPlaythrough = (LevelPlaythrough)_currentPlaythrough.GetValue(game);
                newTime = currentPlaythrough.GetCurrentTimeMicroseconds();
            }
            else
            { // Level Rush, LEVEL RUSHES ARE BROKEN
                LevelRushData bestLevelRushData = LevelRush.GetLevelRushDataByType(LevelRush.GetCurrentLevelRushType());
                bestTime = LevelRush.IsHellRush() ? bestLevelRushData.bestTime_HellMicroseconds : bestLevelRushData.bestTime_HeavenMicroseconds;
                newTime = LevelRush.GetCurrentLevelRushTimerMicroseconds();
            }

            long deltaTime = (bestTime - newTime) / 1000;
            newBest = deltaTime < 0;
            TimeSpan t = TimeSpan.FromMilliseconds(Math.Abs(deltaTime));

            return (newBest ? "+" : "-") + string.Format("{0:0}:{1:00}.{2:000}", 
                                                t.Minutes,
                                                t.Seconds,
                                                t.Milliseconds);
        }
        public static void Destroy() => UnityEngine.Object.Destroy(text);

        //Old Method

        //public static void PostOnSetVisible()
        //{
        //    if (!NeonLite.PBtracker_display.Value)
        //        return;

        //    bool isLevelRush = LevelRush.IsLevelRush();
        //    if (delta == string.Empty)
        //        delta = GetDeltaTimeString(true);

        //    GameObject bestText = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Results Panel/New Best Text");
        //    GameObject deltaTime = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Results Panel/Delta Time");


        //    if (deltaTime == null)
        //    {
        //        deltaTime = UnityEngine.Object.Instantiate(bestText, bestText.transform.parent);
        //        deltaTime.name = "Delta Time";
        //        deltaTime.transform.localPosition += new Vector3(-5, -30, 0);
        //        deltaTime.SetActive(true);
        //    }

        //    TextMeshProUGUI text = deltaTime.GetComponent<TextMeshProUGUI>();
        //    text.SetText(delta);
        //    text.color = newbest ? Color.red : Color.green;

        //    if (!isLevelRush) return;

        //    GameObject bestTextRush = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Level Rush Complete Panel/Level Time Text");
        //    GameObject deltaTimeRush = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Level Rush Complete Panel/Delta Time Rush");

        //    if (deltaTimeRush == null)
        //    {
        //        deltaTimeRush = UnityEngine.Object.Instantiate(bestTextRush, bestTextRush.transform.parent);
        //        deltaTimeRush.name = "Delta Time Rush";
        //        deltaTimeRush.transform.localPosition += new Vector3(0, -30, 0);
        //        deltaTimeRush.SetActive(true);
        //    }
        //    text = deltaTimeRush.GetComponent<TextMeshProUGUI>();
        //    text.SetText(delta);
        //    text.color = newbest ? Color.red : Color.green;

        //    delta = string.Empty; //this causes a memory leak, after every reset, it's reset back to empty
        //}
    }
}