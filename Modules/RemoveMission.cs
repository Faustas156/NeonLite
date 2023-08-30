using HarmonyLib;
using MelonLoader;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class RemoveMission : Module
    {
        private static MelonPreferences_Entry<bool> _setting_RemoveNextMission;

        public RemoveMission() =>
            _setting_RemoveNextMission = NeonLite.Config_NeonLite.CreateEntry("Remove Start Mission button in Job Archive", false, description: "Sick and tired of the big, bulky \"Start Mission\" button that appears? Now you can get rid of it, forever!");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuScreenLocation), "CreateActionButton")]
        private static bool PreCreateActionButton(HubAction hubAction)
        {
            if (hubAction.ID == "PORTAL_CONTINUE_MISSION")
                return !_setting_RemoveNextMission.Value;
            return true;
        }
    }
}
