using UnityEngine;

namespace NeonLite.Modules
{
    internal class LevelTimer : MonoBehaviour
    {
        public static float LevelSessionTimer { get; private set; } = 0f;
        private static string s_levelID = "";

        private readonly Rect _rectLevelTimer = new(10, 20, 100, 70);
        private readonly GUIStyle _style = new()
        {
            font = Resources.Load("fonts/nova_mono/novamono") as Font,
            fontSize = 20
        };

        public static void Initialize()
        {
            if (NeonLite.s_Setting_LevelTimer.Value && !(LevelRush.IsLevelRush() && LevelRush.IsHellRush()))
                new GameObject("LevelTimer", typeof(LevelTimer));
        }

        private void Awake()
        {
            _style.normal.textColor = Color.white;
            if (Singleton<Game>.Instance.GetCurrentLevel().levelID == s_levelID)
                return;
            s_levelID = Singleton<Game>.Instance.GetCurrentLevel().levelID;
            LevelSessionTimer = 0f;
        }

        private void FixedUpdate() => LevelSessionTimer += Time.deltaTime;

        private void OnGUI() => GUI.Label(_rectLevelTimer, Utils.FloatToTime(LevelSessionTimer, "#00:00"), _style);
    }
}
