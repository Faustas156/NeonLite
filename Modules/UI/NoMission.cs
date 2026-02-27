using System.Reflection;

namespace NeonLite.Modules.UI
{
    [Module(10)]
    internal static class NoMission
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI", "noMission", "Remove Start Mission Button", "Sick and tired of the big, bulky \"Start Mission\" button that appears? Now you can get rid of it, forever!", true);
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(MenuScreenLocation), "CreateActionButton", PreCreateButton, Patching.PatchTarget.Prefix);

            active = activate;
        }

        static bool PreCreateButton(HubAction hubAction) => hubAction.ID != "PORTAL_CONTINUE_MISSION" || NeonLite.Game.GetGameData().GetStoryStatus() != StoryStatus.CampaignComplete;
    }
}
