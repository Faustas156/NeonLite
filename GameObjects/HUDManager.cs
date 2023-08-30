using ClockStone;
using UnityEngine;

namespace NeonLite.GameObjects
{
    internal class HUDManager : MonoBehaviour
    {
        internal static void Initialize()
        {
            AudioController audioController = SingletonMonoBehaviour<AudioController>.Instance;
            GameObject WhitePortrait = RM.ui.portraitUI.gameObject;
            GameObject Backstory = WhitePortrait.transform.parent.Find("Backstory").gameObject;

            audioController.ambienceSoundEnabled = !NeonLite.s_Setting_DisableAmbiance.Value;
            WhitePortrait.SetActive(!NeonLite.s_Setting_PlayerPortrait.Value);
            Backstory.SetActive(!NeonLite.s_Setting_BackstoryDisplay.Value);
        }
    }
}
