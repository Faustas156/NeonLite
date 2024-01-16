using System.Reflection;
using TMPro;
using UnityEngine;

namespace NeonLite.GameObjects
{
    internal class CoyoteAssistant : MonoBehaviour
    {
        private float _displayDuration = 0f;
        private readonly GameObject _jumpText;

        private bool _triggered = false;
        private readonly FieldInfo _jumpForgivenessTimer = typeof(FirstPersonDrifter).GetField("jumpForgivenessTimer", NeonLite.s_privateInstance);

        public CoyoteAssistant()
        {
            _jumpText = new("Coyote Assistant");
            _jumpText.transform.parent = gameObject.transform;
            _jumpText.transform.localPosition = new(180, 100);
            _jumpText.transform.rotation = Quaternion.Euler(0f, 0f, 320f);

            TextMeshProUGUI text = _jumpText.AddComponent<TextMeshProUGUI>();
            text.fontSize = 28;
            text.outlineWidth = 0.05f;
            text.outlineColor = Color.yellow;
            text.color = Color.red;
            text.SetText("JUMP!");
            _jumpText.SetActive(false);
        }

        void LateUpdate()
        {
            if (NeonLite.s_Setting_CoyoteAssistant.Value == -1f || RM.drifter == null) return;

            float timer = (float) _jumpForgivenessTimer.GetValue(RM.drifter);

            if (_displayDuration <= 0)
                _jumpText.SetActive(false);
            else
                _displayDuration -= Time.deltaTime;

            if (timer <= 0f) return;

            if (timer <= NeonLite.s_Setting_CoyoteAssistant.Value && !_triggered)
            {
                _triggered = true;
                _displayDuration = 0.1f;
                _jumpText.SetActive(true);
            }
            else
                _triggered = false;

        }
    }
}
