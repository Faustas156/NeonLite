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
            GameObject Backstory = WhitePortrait.transform.parent.gameObject;

            audioController.ambienceSoundEnabled = !NeonLite.ambience_disabled.Value;
            WhitePortrait.SetActive(!NeonLite.playerUIportrait_display.Value);
            Backstory.SetActive(!NeonLite.backstory_display.Value);
        }
    }
}
