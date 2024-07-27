using ClockStone;
using HarmonyLib;
using UnityEngine;

namespace NeonLite.GameObjects
{
    [HarmonyPatch]
    internal class HUDManager : MonoBehaviour
    {
        internal static void Initialize()
        {
            AudioController audioController = SingletonMonoBehaviour<AudioController>.Instance;
            GameObject whitePortrait = RM.ui.portraitUI.gameObject;
            GameObject backstory = whitePortrait.transform.parent.Find("Backstory").gameObject;
            GameObject bottomBar = RM.ui.transform.Find("Overlays/BottomBar/").gameObject;

            audioController.ambienceSoundEnabled = !NeonLite.s_Setting_DisableAmbiance.Value;
            whitePortrait.SetActive(!NeonLite.s_Setting_PlayerPortrait.Value);
            backstory.SetActive(!NeonLite.s_Setting_BackstoryDisplay.Value);
            bottomBar.SetActive(!NeonLite.s_Setting_BottombarDisplay.Value);
            RM.ui.deathOverlay.SetActive(!NeonLite.s_Setting_DamageOverlayDisplay.Value);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerUI), "SetWarningLowHealth")]
        private static void OverrideSetWarningLowHealth(ref bool on) =>
            on = !NeonLite.s_Setting_DamageOverlayDisplay.Value && on;


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIScreenFader), "FadeScreen")]
        private static bool PreFadeScreen(ref UIScreenFader.FadeType ft, ref float time) =>
            !(NeonLite.s_Setting_ShockerOverlayDisplay.Value && ft == UIScreenFader.FadeType.FadeIn && time == 0.333f);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerUI), "SetTelefragOverlay")]
        private static void PreFadeScreen(ref bool on) =>
            on &= !NeonLite.s_Setting_TelefragOverlayDisplay.Value;
    }
}
