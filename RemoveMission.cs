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
        
        public static void ToggleMod(int value)
        {
            if (value == 0) 
            {
                Initialize();
                return;
            }
            
            MethodInfo method = typeof(MenuScreenLocation).GetMethod("CreateActionButton");
            NeonLite.Harmony.Unpatch(method, HarmonyPatchType.Prefix);
        }

        public static bool PreCreateActionButton(HubAction hubAction)
        {
            if (hubAction.ID == "PORTAL_CONTINUE_MISSION")

                return !NeonLite.RemoveMission_display.Value;

            return true;
        }
    }
}
