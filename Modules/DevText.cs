using TMPro;
using UnityEngine;

namespace NeonLite.Modules
{
#if DEBUG
    internal class DevText : MonoBehaviour, IModule
    {
#pragma warning disable CS0414
        const bool priority = false;
        const bool active = true;
        static bool tried = false;

        static GameObject prefab;

        TextMeshProUGUI text;
        Canvas c;

        static void Setup()
        {
            NeonLite.OnBundleLoad += bundle =>
            {
                NeonLite.Logger.DebugMsg("devText onBundleLoad");

                prefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/DevText.prefab");
                if (tried)
                    Activate(true);
            };
        }

        static void Activate(bool _)
        {
            tried = true;
            if (prefab)
                Utils.InstantiateUI(prefab, "DevText", NeonLite.mmHolder.transform).AddComponent<DevText>();
        }

        void Start()
        {
            text = GetComponent<TextMeshProUGUI>();
            text.alpha = .7f;

            c = GetComponentInParent<Canvas>();
        }

        void Update()
        {
            transform.localPosition = c.ViewportToCanvasPosition(new Vector3(1f, 0f, 0));
        }

    }
#endif
}
