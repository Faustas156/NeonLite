using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace NeonLite.Modules.UI
{
    [Module(10)]
    internal class Deltatime : MonoBehaviour
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static bool hit = false;

        internal static TextMeshProUGUI dtLevel;
        static TextMeshProUGUI dtRush;

        static long oldPB = -1;
        static bool wasFinished;

        static bool preModified;

        public TextMeshProUGUI text;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI/In-game", "deltatime", "Deltatime", "Displays a time comparing to your last personal best.", true);
            active = setting.SetupForModule(Activate, static (_, after) => after);

            PastSight.OnActive += OnPastSight;
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(Game), "OnLevelWin", PreWin, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(MenuScreenResults), "OnSetVisible", Helpers.HM(PostSetVisible).SetPriority(Priority.LowerThanNormal), Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(MenuScreenLevelRushComplete), "OnSetVisible", Helpers.HM(PostSetVisibleRush).SetPriority(Priority.LowerThanNormal), Patching.PatchTarget.Postfix);

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
            if (LevelRush.IsLevelRush())
            {
                LevelRushData bestLevelRushData = LevelRush.GetLevelRushDataByType(LevelRush.GetCurrentLevelRushType())
                    ?? new(LevelRush.LevelRushType.Count); // lol
                oldPB = LevelRush.IsHellRush() ? bestLevelRushData.bestTime_HellMicroseconds : bestLevelRushData.bestTime_HeavenMicroseconds;
                wasFinished = oldPB != -1;
            }
            else
            {
                LevelStats levelStats = GameDataManager.levelStats[NeonLite.Game.GetCurrentLevel().levelID];
                // we use pastsight as a backend for levels
                wasFinished = levelStats.GetCompleted();
                preModified = Anticheat.modified.Contains(levelStats);
            }
        }

        static void OnPastSight(bool _) => PostSetVisible(MainMenu.Instance()._screenResults as MenuScreenResults);

        static void PostSetVisible(MenuScreenResults __instance)
        {
            if (!active)
                return;

            bool isLevelRush = LevelRush.IsLevelRush();
            if (isLevelRush)
                return;

            LevelStats levelStats = GameDataManager.levelStats[NeonLite.Game.GetCurrentLevel().levelID];
            var prev = levelStats.GetPreWinStats();

            long newTime = isLevelRush ? LevelRush.GetCurrentLevelRushTimerMicroseconds() : NeonLite.Game.GetCurrentLevelTimerMicroseconds();
            long comp = PastSight.Active ? prev._timeLastMicroseconds : prev._timeBestMicroseconds;

            long delta = (comp - newTime) / 1000;
            bool loses = delta < 0;

            string deltaTimeString = ((comp == newTime) ? "\u00B1" : (loses ? "+" : "-"))
                                    + Helpers.FormatTime(Math.Abs(delta), null, '.', true);

            NeonLite.Logger.DebugMsg(comp + "   " + newTime);

            var baseNB = __instance._resultsScreenNewBestTimeIndicator;

            if (dtLevel == null)
            {
                dtLevel = UnityEngine.Object.Instantiate(baseNB, baseNB.transform.parent).GetComponent<TextMeshProUGUI>();
                dtLevel.name = "Delta Time";
                dtLevel.transform.SetSiblingIndex(baseNB.transform.GetSiblingIndex() + 1);
                Component.Destroy(dtLevel.GetComponent<AxKLocalizedText>());

                var component = dtLevel.GetOrAddComponent<Deltatime>(); // literally just use as a marker for PastSight
                component.text = dtLevel;
                PastSight.registered.Add(component, null);
            }

            if (Anticheat.Active && wasFinished)
            {
                if (!preModified && Anticheat.modified.Contains(levelStats)) // if wasn't modified and now was
                    dtLevel.gameObject.SetActive(false);
                else
                    dtLevel.gameObject.SetActive(true);
            }
            else
                dtLevel.gameObject.SetActive(wasFinished);

            dtLevel.SetText(deltaTimeString);
            if (comp == newTime)
                dtLevel.color = __instance._resultsScreenLevelTime.color;
            else
                dtLevel.color = loses ? Color.red : baseNB.GetComponent<TextMeshProUGUI>().color;

            dtLevel.transform.localPosition = baseNB.transform.localPosition + new Vector3(-5, levelStats.IsNewBest() ? -30 : -10, 0);
            dtLevel.alpha = PastSight.Opacity;
        }

        static void PostSetVisibleRush(MenuScreenLevelRushComplete __instance, bool ___failed)
        {
            long newTime = LevelRush.GetCurrentLevelRushTimerMicroseconds();

            long delta = (oldPB - newTime) / 1000;
            bool loses = delta < 0;

            string deltaTimeString = (loses ? "+" : "-") + Helpers.FormatTime(Math.Abs(delta), null, '.', true);

            NeonLite.Logger.DebugMsg(oldPB + "   " + newTime);

            var baseNB = __instance.bestTimeText.gameObject;

            if (dtRush == null)
            {
                dtRush = UnityEngine.Object.Instantiate(baseNB, baseNB.transform.parent).GetComponent<TextMeshProUGUI>();
                dtRush.name = "Delta Time";
                dtRush.transform.SetSiblingIndex(baseNB.transform.GetSiblingIndex() + 1);
                Component.Destroy(dtRush.GetComponent<AxKLocalizedText>());

                dtRush.horizontalAlignment = HorizontalAlignmentOptions.Center;
                dtRush.margin = Vector4.zero;

                dtRush.rectTransform.pivot = new(1, 0.5f);
                // do this to configure the Y
                dtRush.transform.localPosition = __instance.timeText.transform.localPosition + new Vector3(0, -40, 0);
                // configure X
                var rect = dtRush.rectTransform;
                var last = rect.localPosition;
                last.x = __instance.timeText.rectTransform.offsetMax.x - 10; // sure. whatever
                rect.localPosition = last;
            }

            dtRush.gameObject.SetActive(!Anticheat.Active && wasFinished && !___failed);

            // do the math to configure the size
            var lastS = dtRush.rectTransform.sizeDelta;
            lastS.x = __instance.timeText.GetPreferredValues().x + __instance.timeText.margin.z; // add the right margin again
            dtRush.rectTransform.sizeDelta = lastS;

            dtRush.SetText(deltaTimeString);
            dtRush.color = loses ? Color.red : baseNB.GetComponent<TextMeshProUGUI>().color;
        }
    }
}
