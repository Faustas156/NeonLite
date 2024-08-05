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
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(MenuScreenLocation), "CreateActionButton");
        static void Activate(bool activate)
        {
            if (activate)
                NeonLite.Harmony.Patch(original, prefix: Helpers.HM(PreCreateButton));
            else
                NeonLite.Harmony.Unpatch(original, Helpers.MI(PreCreateButton));

            active = activate;
        }

        static bool PreCreateButton(ref HubAction hubAction) => hubAction.ID != "PORTAL_CONTINUE_MISSION";
    }
}
