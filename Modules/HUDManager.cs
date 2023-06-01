using ClockStone;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NeonWhiteQoL.Modules
{
    internal class HUDManager : MonoBehaviour
    {
        private static GameObject PlayerUIPortrait, Backstory, DamageOverlay, BoostOverlay, ShockerOverlay, TelefragOverlay, BottomBar, UIScreenFader;
        private AudioController audioController;
        private float stopWatch = 1;

        public static void Initialize()
        {
            MethodInfo method = typeof(UIScreenFader).GetMethod("FadeScreen", BindingFlags.Instance | BindingFlags.Public);
            HarmonyMethod harmonyMethod = new (typeof(HUDManager).GetMethod("UIScreenFaderFix"));
            NeonLite.Harmony.Patch(method, harmonyMethod);
        }

        public static bool UIScreenFaderFix(ref Action onComplete)
        {
            if (NeonLite.uiScreenFader_display.Value)
            {
                onComplete?.Invoke();
                return false;
            }
            return true;
        }

        void Start()
        {
            audioController = SingletonMonoBehaviour<AudioController>.Instance;
            PlayerUIPortrait = transform.Find("Player/PlayerAnchor/PlayerUIPortrait").gameObject;
            Backstory = transform.Find("Player/PlayerAnchor/Backstory").gameObject;
            BottomBar = transform.Find("Overlays/BottomBar").gameObject;
            DamageOverlay = transform.Find("Overlays/DamageOverlay").gameObject;
            BoostOverlay = transform.Find("Overlays/BoostOverlay").gameObject;
            ShockerOverlay = transform.Find("Overlays/ShockerOverlay").gameObject;
            TelefragOverlay = transform.Find("Overlays/TelefragOverlay").gameObject;
            UIScreenFader = transform.Find("Overlays/UIScreenFader").gameObject;
            //White = transform.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Results Panel/LevelCompleteScreen/LevelCompleteAnim/White").gameObject;
        }

        void Update()
        {
            stopWatch += Time.deltaTime;
            if (stopWatch < 0.5f) return;
            stopWatch = 0;

            audioController.ambienceSoundEnabled = !NeonLite.ambience_disabled.Value;
            PlayerUIPortrait.SetActive(!NeonLite.playerUIportrait_display.Value);
            Backstory.SetActive(!NeonLite.backstory_display.Value);
            BottomBar.SetActive(!NeonLite.bottombar_display.Value);
            DamageOverlay.SetActive(!NeonLite.damageOverlay_display.Value);
            BoostOverlay.SetActive(!NeonLite.boostOverlay_display.Value);
            ShockerOverlay.SetActive(!NeonLite.shockerOverlay_display.Value);
            TelefragOverlay.SetActive(!NeonLite.telefragOverlay_display.Value);
            UIScreenFader.SetActive(!NeonLite.uiScreenFader_display.Value);
            //White.SetActive(!NeonLite.whiteResult_display.Value);
        }
    }
}
