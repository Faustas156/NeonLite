using HarmonyLib;
using MelonLoader;
using TMPro;
using UnityEngine;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class DnfTime : Module
    {
        private static bool timeFrozen = false;
        private static MelonPreferences_Entry<bool> _setting_DNF;


        public DnfTime()
        {
            _setting_DNF = NeonLite.Config_NeonLite.CreateEntry("DNF", true, description: "Shows your potential time if you didn't finish the level.");
            Singleton<Game>.Instance.OnLevelLoadComplete += () => timeFrozen = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelGate), "OnTriggerStay")]
        private static void PostOnTriggerStay(ref LevelGate __instance)
        {
            if (__instance.Unlocked || timeFrozen || !_setting_DNF.Value || (LevelRush.IsLevelRush() && LevelRush.GetCurrentLevelRush().randomizedIndex.Length - 1 != LevelRush.GetCurrentLevelRush().currentLevelIndex)) return;

            timeFrozen = true;
            GameObject frozenTime = UnityEngine.Object.Instantiate(RM.ui.timerText.gameObject, RM.ui.timerText.transform);
            frozenTime.transform.localPosition += new Vector3(0, 35, 0);
            Game game = Singleton<Game>.Instance;
            long best = GameDataManager.levelStats[game.GetCurrentLevel().levelID].GetTimeBestMicroseconds();
            TextMeshPro frozenText = frozenTime.GetComponent<TextMeshPro>();
            frozenText.color = best < game.GetCurrentLevelTimerMicroseconds() ? Color.red : Color.green;
            frozenText.text = "DNF: " + IGTimer.CreateTimerText();
        }
    }
}