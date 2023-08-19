using Steamworks;
using UnityEngine;

namespace NeonLite.GameObjects
{
    internal class RestartCounter : MonoBehaviour
    {
        public static Dictionary<string, int> LevelRestarts { get; private set; }
        public static int CurrentRestarts { get; private set; }
        private static string s_currentLevel;
        private static string _path;
        private static readonly string _filename = "restartcounter.txt";
        private static bool run = false;

        private readonly Rect _rectTotal = new(10, 40, 100, 70);
        private readonly Rect _rectSession = new(10, 60, 100, 70);
        private readonly GUIStyle _style = new()
        {
            font = Resources.Load("fonts/nova_mono/novamono") as Font,
            fontSize = 20
        };

        public static void Initialize()
        {
            run = SteamManager.Initialized;
            if (!run || (LevelRush.IsLevelRush() && LevelRush.IsHellRush())) return;
            _path = Application.persistentDataPath + "/" + SteamUser.GetSteamID().m_SteamID.ToString();

            if (LevelRestarts == null)
            {
                if (!File.Exists(_path + "/" + _filename))
                {
                    LevelRestarts = new();
                    NeonLite.SaveToFile<Dictionary<string, int>>(_path, _filename, LevelRestarts);
                }
                else
                    LevelRestarts = NeonLite.ReadFile<Dictionary<string, int>>(_path, _filename);
            }
            new GameObject("RestartCounter").AddComponent<RestartCounter>();
        }

        private void Start()
        {
            if (!run) Destroy(this);

            _style.normal.textColor = Color.white;
            string newLevel = NeonLite.Game.GetCurrentLevel().levelID;

            if (s_currentLevel != newLevel)
            {
                s_currentLevel = newLevel;
                CurrentRestarts = 0;
            }
            CurrentRestarts++;

            if (LevelRestarts.ContainsKey(s_currentLevel))
                LevelRestarts[s_currentLevel] = LevelRestarts[s_currentLevel] + 1;
            else
                LevelRestarts[s_currentLevel] = 1;

            NeonLite.SaveToFile<Dictionary<string, int>>(_path, _filename, LevelRestarts);
        }

        private void OnGUI()
        {
            if (NeonLite.restarts_total.Value)
                GUI.Label(_rectTotal, "Total Attempts: " + LevelRestarts[s_currentLevel], _style);
            if (NeonLite.restarts_session.Value)
                GUI.Label(_rectSession, "Attempts: " + CurrentRestarts, _style);
        }
    }
}
