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
            baseText.GetOrAddComponent<Order>().order = int.MinValue;
            baseText.gameObject.SetActive(false);
            c = GetComponentInParent<Canvas>();
            Update();
        }

        void Start()
        {
            ready = true;
            OnTextReady?.Invoke();
        }

        class Order : MonoBehaviour
        {
            public int order;
        }

        internal TextMeshProUGUI MakeText(string name, string text, int order)
        {
            var textO = Utils.InstantiateUI(baseText.gameObject, name, transform).GetComponent<TextMeshProUGUI>();
            textO.text = text;
            textO.GetOrAddComponent<Order>().order = order;
            textO.gameObject.SetActive(true);

            // have 2 keep seperate because reordering in a foreach transform is bad :broken:
            var ordered = GetComponentsInChildren<Transform>(true) // catch the inactices too
                                    .Where(t => t.parent == transform) // directs only
                                    .OrderByDescending(t => t.GetComponent<Order>().order)
                                    .GetEnumerator();

            for (int i = 0; ordered.MoveNext(); ++i)
                ordered.Current.SetSiblingIndex(i);

            return textO;
        }

        [Obsolete("Use MakeText with int order instead.")]
        internal TextMeshProUGUI MakeText(string name, string text) => MakeText(name, text, 0);

        void Update() => transform.localPosition = c.ViewportToCanvasPosition(new Vector3(0f, 0f, 0));
    }
}
