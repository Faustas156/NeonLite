using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace NeonLite.Modules.UI
{
    internal class DNFTime : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static bool hit = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI/In-game", "dnf", "Show DNFs", "Shows the time you would have got if you had killed all the demons in a level.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(LevelGate), "OnTriggerStay");
        static void Activate(bool activate)
        {
            if (activate)
                NeonLite.Harmony.Patch(original, postfix: Helpers.HM(OnTrigger));
            else
                NeonLite.Harmony.Unpatch(original, Helpers.MI(OnTrigger));

            active = activate;
        }

        static void OnLevelLoad(LevelData _) => hit = false;

        static void OnTrigger(ref LevelGate __instance)
        {
            if (__instance.Unlocked || hit || (LevelRush.IsLevelRush() && LevelRush.GetCurrentLevelRush().randomizedIndex.Length - 1 != LevelRush.GetCurrentLevelRush().currentLevelIndex))
                return;

            hit = true;
            GameObject frozenTime = UnityEngine.Object.Instantiate(RM.ui.timerText.gameObject, RM.ui.timerText.transform);
            frozenTime.transform.localPosition += new Vector3(0, 35, 0);
            Game game = NeonLite.Game;
            long best = GameDataManager.levelStats[game.GetCurrentLevel().levelID].GetTimeBestMicroseconds();
            TextMeshPro frozenText = frozenTime.GetComponent<TextMeshPro>();
            frozenText.color = best < game.GetCurrentLevelTimerMicroseconds() ? Color.red : Color.green;
            var local = Localization.Setup(frozenText);
            local.SetKey("NeonLite/DNF", [new("{0}", Helpers.FormatTime(game.GetCurrentLevelTimerMicroseconds() / 1000, ShowMS.setting.Value), false)]);
        }
    }
}
