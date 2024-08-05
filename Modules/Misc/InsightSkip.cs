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

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "insightOff", "Skip Insight Screen", "No longer displays the \"Insight Crystal Dust (Empty)\" screen after finishing a sidequest level.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(MainMenu), "SetItemShowcaseCard");
        static void Activate(bool activate)
        {
            if (activate)
                NeonLite.Harmony.Patch(original, prefix: Helpers.HM(PreShowcase));
            else
                NeonLite.Harmony.Unpatch(original, Helpers.MI(PreShowcase));

            active = activate;
        }

        static bool PreShowcase(ref Action callback)
        {
            callback?.Invoke();
            return false;
        }
    }
}
