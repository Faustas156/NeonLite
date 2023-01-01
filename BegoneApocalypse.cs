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
        
        public static void ToggleMod(int value)
        {
            if (value == 0) 
            {
                Initialize();
                return;
            }
            
            MethodInfo method = typeof(MenuScreenMapAesthetics).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            NeonLite.Harmony.Unpatch(method, HarmonyPatchType.Postfix);
        }

        public static void RemoveApocalypse(MenuScreenMapAesthetics __instance)
        {
            if (NeonLite.Apocalypse_display.Value)
                __instance.SetApocalypse(false);
        }
    }
}
