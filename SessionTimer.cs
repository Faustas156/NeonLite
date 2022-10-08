using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonWhiteQoL
{
    public class SessionTimer : MonoBehaviour
    {
        GUIStyle style = new GUIStyle()
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
            if (SceneManager.GetActiveScene().name == "Heaven_Environment")
                return;
            new GameObject("LevelTimer", typeof(LevelTimer));
        }

        void OnGUI()
        {
            GUI.Label(new Rect(10, 0, 100, 70), Utils.FloatToTime(Time.realtimeSinceStartup, "#00:00"), style);
        }
    }
}
