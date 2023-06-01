using HarmonyLib;
using System.Reflection;

namespace NeonWhiteQoL.Modules
{
    internal class ShowcaseBypass
    {
        public static void Initialize()
        {
            MethodInfo method = typeof(MainMenu).GetMethod("SetItemShowcaseCard");
            HarmonyMethod harmonyMethod = new (typeof(ShowcaseBypass).GetMethod("PreSetItemShowcaseCard"));
            NeonLite.Harmony.Patch(method, harmonyMethod);
        }

        public static bool PreSetItemShowcaseCard(MainMenu __instance, ref PlayerCardData cardData, ref Action callback)
        {
            if (!NeonLite.InsightScreen_enable.Value)
                return true;

            callback?.Invoke();
            return false;
        }
    }
}
