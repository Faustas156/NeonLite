using System.Reflection;
using TMPro;
using UnityEngine;

namespace NeonLite.GameObjects
{
    internal class RocketHealthIndicator : MonoBehaviour
    {
        private readonly Vector3 SCALE = new(8, 5, 1);
        private readonly TextMeshPro[] _textHolders = new TextMeshPro[4];

        private readonly FieldInfo ObjectPool = typeof(ObjectPool).GetField("pools", NeonLite.s_privateStatic);

        public static void Initialize()
        {
            new GameObject("Rocket health", typeof(RocketHealthIndicator));
        }

        private void Start()
        {
            GameObject bars = new("Rocket Healths");
            bars.transform.parent = GameObject.Find("HUD/Crosshair/").transform;
            bars.AddComponent<Canvas>();
            bars.transform.localPosition = new(2.4f, -0.1f, 0f);
            bars.transform.localScale = new(0.01f, 0.02f, 1f);
            bars.layer = 5;

            GameObject rocket1 = new("Rocket1");
            rocket1.transform.SetParent(bars.transform, false);
            rocket1.transform.localScale = SCALE;
            rocket1.transform.localPosition = new(-45f, 25f, 0f);
            rocket1.layer = 5;
            _textHolders[0] = rocket1.AddComponent<TextMeshPro>();
            _textHolders[0].color = Color.red;

            GameObject rocket2 = new("Rocket2");
            rocket2.transform.SetParent(bars.transform, false);
            rocket2.transform.localScale = SCALE;
            rocket2.transform.localPosition = new(-45f, 0f, 0f);
            rocket2.layer = 5;
            _textHolders[1] = rocket2.AddComponent<TextMeshPro>();
            _textHolders[1].color = Color.red;

            GameObject rocket3 = new("Rocket3");
            rocket3.transform.SetParent(bars.transform, false);
            rocket3.transform.localScale = SCALE;
            rocket3.transform.localPosition = new(-45f, -25f, 0f);
            rocket3.layer = 5;
            _textHolders[2] = rocket3.AddComponent<TextMeshPro>();
            _textHolders[2].color = Color.red;

            GameObject rocket4 = new("Rocket4");
            rocket4.transform.SetParent(bars.transform, false);
            rocket4.transform.localScale = SCALE;
            rocket4.transform.localPosition = new(-45f, -50f, 0f);
            rocket4.layer = 5;
            _textHolders[3] = rocket4.AddComponent<TextMeshPro>();
            _textHolders[3].color = Color.red;
        }

        private void Update()
        {
        }
    }
}
