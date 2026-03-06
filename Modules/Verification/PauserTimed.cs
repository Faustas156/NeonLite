using I2.Loc;
using NeonLite.Modules.UI.Status;
using TMPro;
using UnityEngine;

namespace NeonLite.Modules.Verification
{
    // what's your pauser doin??
    [Module]
    internal static class PauserTimed
    {
        const bool priority = false;
        const bool active = true;

        static bool running = false;
        static bool paused = false;
        static float rushTimer = 0;

        const float RUSH_LIMIT = 60 * 5;

        static bool levelFail = false;
        const string LEVEL_FAILTEXT = "Paused mid-level";
        static bool rushFail = false;
        const string RUSH_FAILTEXT = "Pauses exceeded {0}m";

        static void Setup()
        {
            StatusText.OnTextReady += SetText;
            Verifier.OnReset += OnReset;
        }

        static void Activate(bool _)
        {
            NeonLite.Game.OnLevelLoadComplete += ResetTimes;
            Patching.AddPatch(typeof(MainMenu), "SetState", PostSetState, Patching.PatchTarget.Postfix);
        }

        static void OnReset()
        {
            running = false;
            levelFail = false;

            if (!LevelRush.IsLevelRush() || LevelRush.GetCurrentLevelRushTimerMicroseconds() <= 0)
            {
                rushTimer = 0;
                rushFail = false;

                rushText.gameObject.SetActive(false);
                RushTimer.localizeCache ??= LocalizationManager.GetTranslation(RushTimer.KEY);
            }
        }

        static void ResetTimes()
        {
            if (!LoadManager.currentLevel || LoadManager.currentLevel.type == LevelData.LevelType.Hub)
                return;

            paused = false;
            running = true;
        }


        static readonly HashSet<MainMenu.State> pauseStates = [MainMenu.State.Pause, MainMenu.State.Options, MainMenu.State.OptionsRebind];

        static void PostSetState(MainMenu.State newState)
        {
            if (!pauseStates.Contains(newState))
            {
                if (LevelRush.IsLevelRush())
                    rushText.gameObject.SetActive(false);
                paused = false;
                return;
            }

            if (LevelRush.IsLevelRush())
                rushText.gameObject.SetActive(!rushFail);

            if (!running)
                return;

            paused = true;
            if (!levelFail)
            {
                Verifier.SetRunUnverifiable(typeof(PauserTimed), LEVEL_FAILTEXT);
                levelFail = true;
            }
        }

        static TextMeshProUGUI rushText;
        class RushTimer : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            public static string localizeCache;
            public const string KEY = "NeonLite/RUSHTIMER_TIMELEFT";

            public void ChangeFont() { }

            public void Localize() => SetKey(KEY);

            public void SetKey(string key) => localizeCache = LocalizationManager.GetTranslation(key);

            void Update()
            {
                rushTimer += Time.unscaledDeltaTime;

                if (rushTimer > RUSH_LIMIT)
                {
                    gameObject.SetActive(false);
                    Verifier.SetRushUnverifiable(typeof(PauserTimed), RUSH_FAILTEXT);
                    rushFail = true;
                    return;
                }

                rushText.alpha = 1;

                var timeLeft = (int)(RUSH_LIMIT - rushTimer);
                rushText.text = string.Format(localizeCache, timeLeft / 60, timeLeft % 60);
            }
        }

        static void SetText()
        {
            rushText = StatusText.i.MakeText("Rush Timer", "aaaaa", 10);
            rushText.gameObject.SetActive(false);
            rushText.color = Color.white;
            rushText.alpha = 0;

            rushText.GetOrAddComponent<RushTimer>();
            rushText.transform.SetSiblingIndex(StatusText.i.transform.Find("verify").GetSiblingIndex() + 1);
        }
    }
}
