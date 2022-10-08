using HarmonyLib;
using System.Reflection;

namespace NeonWhiteQoL
{
    public class RemoveMission
    {
        public static void Initialize()
        {
            MethodInfo method = typeof(MenuScreenLocation).GetMethod("CreateActionButton");
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(RemoveMission).GetMethod("PostRemoveButton"));
            NeonLite.Harmony.Patch(method, harmonyMethod);
        }
        // todo: add a way to reenable the continue mission button, mainly for convenience
        public static bool PostRemoveButton(HubAction hubAction)
        {
            if (hubAction.ID == "PORTAL_CONTINUE_MISSION")
            {
                return false;
            }
            return true;
        }
    }
}
