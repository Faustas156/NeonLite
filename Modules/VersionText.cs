using TMPro;
using UnityEngine;

namespace NeonLite.Modules
{
    internal class VersionText : MonoBehaviour, IModule
    {
#pragma warning disable CS0414
        const bool priority = false;
        const bool active = true;
        static bool tried = false;

        static GameObject prefab;

        TextMeshProUGUI text;
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
        }

        void Start()
        {
            text = GetComponent<TextMeshProUGUI>();
            text.text = $"NeonLite v{ver}";
        }
    }
}
