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
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(MainMenu), "SetItemShowcaseCard", PreShowcase, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(MainMenu), "OnPressButtonStartGame", PreStartGame, Patching.PatchTarget.Prefix);

            active = activate;
        }

        static bool PreShowcase(ref Action callback)
        {
            callback?.Invoke();
            return false;
        }

        static void PreStartGame()
        {
            // if they haven't completed movement in this save
            if (GameDataManager.levelStats[Singleton<Game>.Instance.GetGameData().GetLevelDataIDsList(false)[0]].GetTimeLastMicroseconds() < 0)
                Activate(false);
        }
    }
}
