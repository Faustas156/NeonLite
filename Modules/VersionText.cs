using NeonLite.Modules.UI.Status;
using TMPro;
using UnityEngine;

namespace NeonLite.Modules
{
    [Module]
    internal class VersionText : MonoBehaviour
    {
#pragma warning disable CS0414
        const bool priority = false;
        const bool active = true;
        static bool tried = false;

        static GameObject prefab;

        internal static string ver;

        static void Setup()
        {
            NeonLite.OnBundleLoad += static bundle =>
            {
                prefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/VersionText.prefab");
                if (tried)
                    Activate(true);
            };
        }

        static void Activate(bool _)
        {
            tried = true;
            if (prefab)
                Utils.InstantiateUI(prefab, "VersionText", MainMenu.Instance()._screenTitle.transform.Find("Logo")).AddComponent<VersionText>();

            NeonLite.Game.winAction += OnLevelWin;
        }

        TextMeshProUGUI text;
        static TextMeshProUGUI smtext;
        GameObject verifyText;

        void Awake()
        {
            text = GetComponent<TextMeshProUGUI>();
            text.text = $"NeonLite v{ver}";

            if (StatusText.ready)
                OnTextReady();
            else
                StatusText.OnTextReady += OnTextReady;

            verifyText = transform.GetChild(0).gameObject;
            Verifier.SpriteAsset = verifyText.GetComponent<TextMeshProUGUI>().spriteAsset;
            Update();
        }

        void Update()
        {
            verifyText.SetActive(!Verifier.Verified);
        }

        void OnTextReady()
        {
            smtext = StatusText.i.MakeText("ver", $"NL{ver}", -100);
            smtext.color = text.color;
            smtext.colorGradient = text.colorGradient;
            smtext.colorGradientPreset = text.colorGradientPreset;
            smtext.fontSize = 18;
            smtext.gameObject.SetActive(false);
        }

        static void OnLevelLoad(LevelData _) => smtext.gameObject.SetActive(false);
        static void OnLevelWin() => smtext.gameObject.SetActive(true);
    }
}
