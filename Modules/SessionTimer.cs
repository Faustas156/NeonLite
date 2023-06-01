using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonWhiteQoL.Modules
{
    public class SessionTimer : MonoBehaviour
    {
        private readonly GUIStyle style = new ()
        {
            font = Resources.Load("fonts/nova_mono/novamono") as Font,
            fontSize = 20
        };

        void Awake()
        {
            style.normal.textColor = Color.white;
            DontDestroyOnLoad(transform.gameObject);
            Singleton<Game>.Instance.OnLevelLoadComplete += LevelLoaded;
        }

        internal void LevelLoaded()
        {
            if (SceneManager.GetActiveScene().name == "Heaven_Environment" || !NeonLite.LevelTimer_display.Value)
                return;
            new GameObject("LevelTimer", typeof(LevelTimer));
        }

        void OnGUI()
        {
            if (NeonLite.SessionTimer_display.Value)
                GUI.Label(new Rect(10, 0, 100, 70), Utils.FloatToTime(Time.realtimeSinceStartup, "#00:00"), style);
        }
    }
}
