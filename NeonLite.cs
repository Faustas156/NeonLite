using MelonLoader;

namespace NeonWhiteQoL
{
    public class NeonLite : MelonMod
    {
        public static HarmonyLib.Harmony harmony { get; private set; } 

        public override void OnApplicationLateStart()
        {
            harmony = new HarmonyLib.Harmony("NAMEHERE");
            PBtracker.Initialize();
            GreenHP.Initialize();
        }
    }
}
