using HarmonyLib;
using UnityEngine;

namespace NeonLite.GameObjects
{
    [HarmonyPatch]
    internal class SessionPB : MonoBehaviour
    {
        private static string _currentLevel;
        public static Dictionary<string, (long, string)> LevelPB { get; private set; } = [];

        private readonly Rect _rectSessionPB = new(10, 80, 100, 70);
        private readonly GUIStyle _style = new()
        {
            font = Resources.Load("fonts/nova_mono/novamono") as Font,
            fontSize = 20
        };

        public static void Initialize()
        {
            NeonLite.Game.winAction += () =>
            {
                long millisecondTimer = NeonLite.Game.GetCurrentLevelTimerMicroseconds() / 1000;
                TimeSpan timeSpan = TimeSpan.FromMilliseconds(millisecondTimer);

                long levelPB = LevelPB[_currentLevel].Item1;

                if (millisecondTimer >= levelPB) return;

                string resulttime = string.Format("{0:0}:{1:00}.{2:000}",
                                                    timeSpan.Minutes,
                                                    timeSpan.Seconds,
                                                    timeSpan.Milliseconds);
                LevelPB[_currentLevel] = (millisecondTimer, resulttime);
            };
        }

        private void Start()
        {
            _style.normal.textColor = Color.white;
            _currentLevel = NeonLite.Game.GetCurrentLevel().levelID;
            if (!LevelPB.ContainsKey(_currentLevel))
                LevelPB.Add(_currentLevel, (long.MaxValue, "0:00:000"));
        }

        private void OnGUI()
        {
            if (NeonLite.s_Setting_RestartsSession.Value)
                GUI.Label(_rectSessionPB, "SessionPB: " + LevelPB[_currentLevel].Item2, _style);
        }
    }
}
