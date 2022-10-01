using HarmonyLib;
using System.Reflection;

namespace NeonWhiteQoL
{
    public class SkipIntro
    {
        private static bool ran = false;
        public static void Initialize()
        {
            MethodInfo method = typeof(IntroCards).GetMethod("SetState", BindingFlags.NonPublic | BindingFlags.Instance);
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(SkipIntro).GetMethod("SkippingIntro"));
            NeonLite.Harmony.Patch(method, harmonyMethod);
        }

        public static void SkippingIntro(IntroCards __instance)
        {
            if (ran) return;
            __instance.introCards = new IntroCard[0];
            ran = true;
        }
    }
}
