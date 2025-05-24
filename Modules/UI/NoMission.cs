using HarmonyLib;
using System.Reflection;

namespace NeonLite.Modules.UI
{
    internal class NoMission : IModule
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
            if (activate)
                Patching.TogglePatch(activate, typeof(MenuScreenLocation), "CreateActionButton", PreCreateButton, Patching.PatchTarget.Prefix);
            else
                Patching.RemovePatch(typeof(MenuScreenLocation), "CreateActionButton", PreCreateButton);

            active = activate;
        }

        static bool PreCreateButton(ref HubAction hubAction) => hubAction.ID != "PORTAL_CONTINUE_MISSION";
    }
}
