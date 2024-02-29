using TMPro;
using UnityEngine;

namespace NeonLite
{
    internal class Beta : MonoBehaviour
    {
        private void Start()
        {
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.Log("Creating canvas");
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            GameObject textHolder = new("textHolderBeta");
            textHolder.transform.parent = gameObject.transform;
            textHolder.layer = 5;
            textHolder.transform.position = new(110, 1320);

            TextMeshProUGUI text = textHolder.AddComponent<TextMeshProUGUI>();
            text.overflowMode = TextOverflowModes.Overflow;
            text.fontSize = 22f;
            text.enableWordWrapping = true;
            text.outlineColor = Color.black;
            text.outlineWidth = 0.15f;
            text.lineSpacing = -30f;
            text.SetText("BETABUILD");
        }
    }
}
