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
    }
}
