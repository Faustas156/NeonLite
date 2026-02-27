using TMPro;
using UnityEngine;

namespace NeonLite.Modules.UI.Status
{
    [Module(200)]
    internal class StatusText : MonoBehaviour
    {
        static internal StatusText i;
        const bool priority = false;
        const bool active = true;
        static bool tried = false;

        static GameObject prefab;

        internal static bool ready = false;
        internal static event Action OnTextReady;

        static void Setup()
        {
            NeonLite.OnBundleLoad += bundle =>
            {
                prefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/StatusTexts.prefab");
                if (tried)
                    Activate(true);
            };
        }

        static void Activate(bool _)
        {
            tried = true;
            if (prefab)
                Utils.InstantiateUI(prefab, "StatusText", NeonLite.mmHolder.transform).AddComponent<StatusText>();
        }

        TextMeshProUGUI baseText;
        Canvas c;

        void Awake()
        {
            i = this;
            baseText = GetComponentInChildren<TextMeshProUGUI>(true);
            baseText.gameObject.SetActive(false);
            c = GetComponentInParent<Canvas>();
            Update();
        }

        void Start()
        {
            ready = true;
            OnTextReady?.Invoke();
        }

        internal TextMeshProUGUI MakeText(string name, string text)
        {
            var textO = Utils.InstantiateUI(baseText.gameObject, name, transform).GetComponent<TextMeshProUGUI>();
            textO.text = text;
            textO.gameObject.SetActive(true);
            return textO;
        }

        void Update() => transform.localPosition = c.ViewportToCanvasPosition(new Vector3(0f, 0f, 0));

    }
}
