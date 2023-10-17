using HarmonyLib;
using UnityEngine;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class Exp : Module
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuCharacterRelationshipGift), "PlayGiftRoutine")]
        private static bool Experimental(ref Action onGiftHit)
        {
            onGiftHit?.Invoke();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InsightXpGems), "RewardCoroutine")]
        private static bool Experimental1() => false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WaitForSeconds), MethodType.Constructor, new Type[] { typeof(float) })]
        private static void Experimental2(ref float seconds)
        {
            if (seconds == 0.75f)
                seconds = 0.01f;
        }
    }
}
