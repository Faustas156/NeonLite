﻿using HarmonyLib;
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
        
        public static void ToggleMod(int value)
        {
            if (value == 0) 
            {
                Initialize();
                return;
            }
            
            MethodInfo method = typeof(MainMenu).GetMethod("SetItemShowcaseCard");
            NeonLite.Harmony.Unpatch(method, HarmonyPatchType.Prefix);
        }

        public static bool PreSetItemShowcaseCard(MainMenu __instance, ref PlayerCardData cardData, ref Action callback)
        {
            if (!NeonLite.InsightScreen_enable.Value)
                return true;

            if (callback != null) callback();
            return false;
        }
    }
}
