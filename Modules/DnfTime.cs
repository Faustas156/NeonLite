using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace NeonWhiteQoL.Modules
{
    internal class DnfTime
    {
        private static bool timeFrozen = false;
        public static void Initialize()
        {
            Singleton<Game>.Instance.OnLevelLoadComplete += () => timeFrozen = false;

            MethodInfo method = typeof(LevelGate).GetMethod("OnTriggerStay", BindingFlags.NonPublic | BindingFlags.Instance);
            HarmonyMethod harmonyMethod = new (typeof(DnfTime).GetMethod("PostOnTriggerStay"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);
        }

        public static void PostOnTriggerStay(ref LevelGate __instance)
        {
            if (__instance.Unlocked || timeFrozen || !NeonLite.dnf_enabler.Value) return;

            timeFrozen = true;
            GameObject frozenTime = UnityEngine.Object.Instantiate(RM.ui.timerText.gameObject, RM.ui.timerText.transform);
            frozenTime.transform.localPosition += new Vector3(0, 35, 0);
            Game game = Singleton<Game>.Instance;
            long best = GameDataManager.levelStats[game.GetCurrentLevel().levelID].GetTimeBestMicroseconds();
            TextMeshPro frozenText = frozenTime.GetComponent<TextMeshPro>();
            frozenText.color = best < game.GetCurrentLevelTimerMicroseconds() ? Color.red : Color.green;
            frozenText.text = "DNF: " + frozenText.text;
        }
    }
}
