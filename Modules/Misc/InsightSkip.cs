using HarmonyLib;
using System;
using System.Reflection;

namespace NeonLite.Modules.Misc
{
    internal class InsightSkip : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;
        static bool first = true;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "insightOff", "Skip Insight Screen", "No longer displays the \"Insight Crystal Dust (Empty)\" screen after finishing a sidequest level.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(MainMenu), "SetItemShowcaseCard");
        static readonly MethodInfo ognewgame = AccessTools.Method(typeof(MainMenu), "OnPressButtonStartGame");
        static void Activate(bool activate)
        {
            if (activate && (!active || first))
            {
                NeonLite.Harmony.Patch(original, prefix: Helpers.HM(PreShowcase));
                NeonLite.Harmony.Patch(ognewgame, prefix: Helpers.HM(PreNewGame));
            }
            else if (!activate && active)
            {
                NeonLite.Harmony.Unpatch(original, Helpers.MI(PreShowcase));
                NeonLite.Harmony.Unpatch(ognewgame, Helpers.MI(PreNewGame));
            }

            first = false;
            active = activate;
        }

        static bool PreShowcase(ref Action callback)
        {
            callback?.Invoke();
            return false;
        }

        static void PreNewGame() => Activate(false);
    }
}
