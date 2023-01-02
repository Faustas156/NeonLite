using UnityEngine;

namespace NeonWhiteQoL
{
    internal class HUDManager : MonoBehaviour
    {
        private GameObject PlayerUIPortrait, Backstory, DamageOverlay, BoostOverlay, ShockerOverlay, TelefragOverlay, BottomBar, UIScreenFader;
        private float stopWatch = 1;
        void Start()
        {
            PlayerUIPortrait = transform.Find("Player/PlayerAnchor/PlayerUIPortrait").gameObject;
            Backstory = transform.Find("Player/PlayerAnchor/Backstory").gameObject;
            BottomBar = transform.Find("Overlays/BottomBar").gameObject;
            DamageOverlay = transform.Find("Overlays/DamageOverlay").gameObject;
            BoostOverlay = transform.Find("Overlays/BoostOverlay").gameObject;
            ShockerOverlay = transform.Find("Overlays/ShockerOverlay").gameObject;
            TelefragOverlay = transform.Find("Overlays/TelefragOverlay").gameObject;
            UIScreenFader = transform.Find("Overlays/UIScreenFader").gameObject;
        }

        void Update()
        {
            stopWatch += Time.deltaTime;
            if (stopWatch < 0.5f) return;
            stopWatch = 0;
            PlayerUIPortrait.SetActive(!NeonLite.playerUIportrait_display.Value);
            Backstory.SetActive(!NeonLite.backstory_display.Value);
            BottomBar.SetActive(!NeonLite.bottombar_display.Value);
            DamageOverlay.SetActive(!NeonLite.damageOverlay_display.Value);
            BoostOverlay.SetActive(!NeonLite.boostOverlay_display.Value);
            ShockerOverlay.SetActive(!NeonLite.shockerOverlay_display.Value);
            TelefragOverlay.SetActive(!NeonLite.telefragOverlay_display.Value);
            UIScreenFader.SetActive(!NeonLite.uiScreenFader_display.Value);
        }
    }
}
