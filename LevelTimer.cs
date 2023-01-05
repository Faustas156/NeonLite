using UnityEngine;

namespace NeonWhiteQoL
{
    internal class LevelTimer : MonoBehaviour
    {
        public static float LevelSessionTimer { get; set; } = 0f;
        private static string levelID = "";

        GUIStyle style = new GUIStyle()
        {
            font = Resources.Load("fonts/nova_mono/novamono") as Font,
            fontSize = 20
        };

        void Awake()
        {
            style.normal.textColor = Color.white;
            if (Singleton<Game>.Instance.GetCurrentLevel().levelID == levelID)
                return;
            levelID = Singleton<Game>.Instance.GetCurrentLevel().levelID;
            LevelSessionTimer = 0f;
        }

        void Update() => LevelSessionTimer += Time.deltaTime;

        void OnGUI() => GUI.Label(new Rect(10, 20, 100, 70), Utils.FloatToTime(LevelSessionTimer, "#00:00"), style);
    }
}
