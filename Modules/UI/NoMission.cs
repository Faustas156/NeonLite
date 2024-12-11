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
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(MenuScreenLocation), "CreateActionButton");
        static void Activate(bool activate)
        {
            if (activate)
                Patching.AddPatch(original, PreCreateButton, Patching.PatchTarget.Prefix);
            else
                Patching.RemovePatch(original, PreCreateButton);

            active = activate;
        }

        static bool PreCreateButton(ref HubAction hubAction) => hubAction.ID != "PORTAL_CONTINUE_MISSION";
    }
}
