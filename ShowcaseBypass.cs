using HarmonyLib;
using System.Reflection;

namespace NeonWhiteQoL
{
    internal class ShowcaseBypass
    {
        public static void Initialize()
        {
            MethodInfo method = typeof(MainMenu).GetMethod("SetItemShowcaseCard");
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(ShowcaseBypass).GetMethod("PreSetItemShowcaseCard"));
            NeonLite.Harmony.Patch(method, harmonyMethod);
        }

        public static bool PreSetItemShowcaseCard(MainMenu __instance, ref PlayerCardData cardData, ref Action callback)
        {
            if (callback != null) callback();
            return false;
        }
    }
}
