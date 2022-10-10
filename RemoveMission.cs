using HarmonyLib;
using System.Reflection;

namespace NeonWhiteQoL
{
    public class RemoveMission
    {
        public static void Initialize()
        {
            MethodInfo method = typeof(MenuScreenLocation).GetMethod("CreateActionButton");
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(RemoveMission).GetMethod("PreCreateActionButton"));
            NeonLite.Harmony.Patch(method, harmonyMethod);
        }

        public static bool PreCreateActionButton(HubAction hubAction)
        {
            //return hubAction.ID != "PORTAL_CONTINUE_MISSION" || NeonLite.RemoveMission_display.Value;
            if (hubAction.ID == "PORTAL_CONTINUE_MISSION")

                return !NeonLite.RemoveMission_display.Value;

            return true;
        }
    }
}
