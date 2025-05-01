using HarmonyLib;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace NeonLite.Modules.UI
{
    internal class Deltatime : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static bool hit = false;

        static GameObject dtLevel;
        static GameObject dtRush;

        static long oldPB = -1;
        static bool wasFinished;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI/In-game", "deltatime", "Deltatime", "Displays a time comparing to your last personal best.", true);
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(Game), "OnLevelWin", PreWin, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(MenuScreenResults), "OnSetVisible", PostSetVisible, Patching.PatchTarget.Postfix);

            if (!activate)
            {
                if (dtLevel)
                    UnityEngine.Object.Destroy(dtLevel);
                if (dtRush)
                    UnityEngine.Object.Destroy(dtRush);
            }

            active = activate;
        }

        static void OnLevelLoad(LevelData _) => hit = false;

        static void PreWin()
        {
            LevelStats levelStats = GameDataManager.levelStats[NeonLite.Game.GetCurrentLevel().levelID];
            LevelRushData bestLevelRushData = LevelRush.GetLevelRushDataByType(LevelRush.GetCurrentLevelRushType());
            oldPB = LevelRush.IsLevelRush() ? (LevelRush.IsHellRush() ? bestLevelRushData.bestTime_HellMicroseconds : bestLevelRushData.bestTime_HeavenMicroseconds) : levelStats.GetTimeBestMicroseconds();
            wasFinished = (LevelRush.IsLevelRush() ? oldPB != -1 : levelStats.GetCompleted());
        }

        static void PostSetVisible()
        {
            bool isLevelRush = LevelRush.IsLevelRush();
            long newTime = isLevelRush ? LevelRush.GetCurrentLevelRushTimerMicroseconds() : NeonLite.Game.GetCurrentLevelTimerMicroseconds();
            long bestTime = oldPB;

            long delta = (bestTime - newTime) / 1000;
            bool newBest = delta < 0;

            string deltaTimeString = (newBest ? "+" : "-") + Helpers.FormatTime(Math.Abs(delta), null, '.', true);

            Debug.Log(bestTime + "   " + newTime);

            TextMeshProUGUI text;
            GameObject levelTimeObject;

            if (!isLevelRush)
            {
                if (dtLevel == null)
                {
                    levelTimeObject = ((MenuScreenResults)MainMenu.Instance()._screenResults)._resultsScreenNewBestTimeIndicator;
                    dtLevel = UnityEngine.Object.Instantiate(levelTimeObject, levelTimeObject.transform.parent);
                    dtLevel.transform.SetSiblingIndex(levelTimeObject.transform.GetSiblingIndex() + 1);
                    dtLevel.name = "Delta Time";
                    dtLevel.transform.localPosition += new Vector3(-5, -30, 0);
                }
                dtLevel.SetActive(wasFinished);

                text = dtLevel.GetComponent<TextMeshProUGUI>();
                text.SetText(deltaTimeString);
                text.color = newBest ? Color.red : Color.green;
                return;
            }

            if (dtRush == null)
            {
                levelTimeObject = MainMenu.Instance()._screenLevelRushComplete.timeText.gameObject;
                dtRush = UnityEngine.Object.Instantiate(levelTimeObject, levelTimeObject.transform.parent);
                dtRush.transform.SetSiblingIndex(levelTimeObject.transform.GetSiblingIndex() + 1);
                dtRush.name = "Delta Time Rush";
                dtRush.transform.localPosition += new Vector3(0, -30, 0);
            }
            dtRush.SetActive(wasFinished);
            text = dtRush.GetComponent<TextMeshProUGUI>();
            text.SetText(deltaTimeString);
            text.color = newBest ? Color.red : Color.green;
        }
    }
}
