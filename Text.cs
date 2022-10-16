using UnityEngine;

namespace NeonWhiteQoL
{
    internal class Text : MonoBehaviour
    {
        GUIStyle style = new GUIStyle()
        {
            font = Resources.Load("fonts/source code pro/sourcecodepro-medium") as Font,
            fontSize = 18,
        };

        void OnGUI()
        {
            style.normal.textColor = Color.yellow;
            DontDestroyOnLoad(transform.gameObject);
            GUI.Label(new Rect(1545, 1055, 1920, 1080), "NeonLite Developer Build - v1.3.0", style);
        }

    }
}
