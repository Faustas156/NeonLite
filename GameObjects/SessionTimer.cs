using UnityEngine;

namespace NeonLite.GameObjects
{
    public class SessionTimer : MonoBehaviour
    {
        private readonly Rect _rectSessionTimer = new(10, 0, 100, 70);
        private readonly GUIStyle _style = new()
        {
            font = Resources.Load("fonts/nova_mono/novamono") as Font,
            fontSize = 20
        };

        private void Start()
        {
            if (!NeonLite.SessionTimer_display.Value)
            {
                Destroy(this);
                return;
            }
            _style.normal.textColor = Color.white;
        }

        private void OnGUI() =>
            GUI.Label(_rectSessionTimer, Utils.FloatToTime(Time.realtimeSinceStartup, "#00:00"), _style);
    }
}
