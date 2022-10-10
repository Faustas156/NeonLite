using MelonLoader;
using UnityEngine;

namespace NeonWhiteQoL
{
    public class NeonLite : MelonMod
    {
        public static HarmonyLib.Harmony Harmony { get; private set; }
        public override void OnApplicationLateStart()
        {
            Harmony = new HarmonyLib.Harmony("NAMEHERE");
            PBtracker.Initialize();
            GreenHP.Initialize();
            SkipIntro.Initialize();
            RemoveMission.Initialize();
            LeaderboardFix.Initialize();
            GameObject timer = new GameObject("SessionTimer", typeof(SessionTimer));
        }

        public static MelonPreferences_Category neonLite_config;
        public static MelonPreferences_Entry<bool> PBtracker_display;
        public static MelonPreferences_Entry<bool> GreenHP_display;
        public static MelonPreferences_Entry<bool> RemoveMission_display;
        public static MelonPreferences_Entry<bool> SessionTimer_display;
        public static MelonPreferences_Entry<bool> LevelTimer_display;

        public override void OnApplicationStart()
        {
            neonLite_config = MelonPreferences.CreateCategory("NeonLite Settings");
            PBtracker_display = neonLite_config.CreateEntry("Enable PB Tracker", true, null, "Displays a time based on whether or not you got a new personal best."); 
            GreenHP_display = neonLite_config.CreateEntry("Enable Neon Green's HP", true, null, "Displays the HP of Neon Green in Text Form.");
            RemoveMission_display = neonLite_config.CreateEntry("Remove Start Mission button in Job Archive", true, null, "Sick and tired of the big, bulky \"Start Mission\" button that appears? Now you can get rid of it, forever!");
            SessionTimer_display = neonLite_config.CreateEntry("Display Session Timer", true, null, "Tracks your current play session time.");
            LevelTimer_display = neonLite_config.CreateEntry("Display Level Timer", true, null, "Tracks the time you've spent on the current level you're playing.");
        }
    }
}
