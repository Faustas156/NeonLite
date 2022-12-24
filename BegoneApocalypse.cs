using HarmonyLib;
using System.Reflection;

namespace NeonWhiteQoL
{
    internal class BegoneApocalypse
    {
        public static void Initialize()
        {
            MethodInfo method = typeof(MenuScreenMapAesthetics).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(BegoneApocalypse).GetMethod("RemoveApocalypse"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);
        }

        public static void RemoveApocalypse(MenuScreenMapAesthetics __instance)
        {
            __instance.SetApocalypse(!NeonLite.Apocalypse_display.Value);
        }
    }
}
