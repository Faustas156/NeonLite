using HarmonyLib;
using MelonLoader;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class RemoveMission : Module
    {
        public static MelonPreferences_Entry<bool> RemoveMission_display;

        public RemoveMission() =>
            RemoveMission_display = NeonLite.neonLite_config.CreateEntry("Remove Start Mission button in Job Archive", false, description: "Sick and tired of the big, bulky \"Start Mission\" button that appears? Now you can get rid of it, forever!");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuScreenLocation), "CreateActionButton")]
        private static bool PreCreateActionButton(HubAction hubAction)
        {
            if (hubAction.ID == "PORTAL_CONTINUE_MISSION")
                return !RemoveMission_display.Value;
            return true;
        }
    }
}
