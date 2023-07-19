using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NeonWhiteQoL.Modules
{
    public class BestRun
    {
        private static readonly FieldInfo _currentPlaythrough = typeof(Game).GetField("_currentPlaythrough", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly GUIStyle style = new()
        {
            font = Resources.Load("fonts/nova_mono/novamono") as Font,
            fontSize = 20
        };

        public static string GetBestRunDelta()
        {
            var game = Singleton<Game>.Instance;

            long BestTime;

            if (!LevelRush.IsLevelRush())
            {
                LevelPlaythrough currentPlaythrough = (LevelPlaythrough)_currentPlaythrough.GetValue(game);
                BestTime = currentPlaythrough.GetCurrentTimeMicroseconds();
            }

        }
        void Awake()
        {
            style.normal.textColor = Color.white;
        }

        //void OnGUI() => GUI.Label(new Rect(10, 80, 100, 70), Utils.FloatToTime(LevelSessionTimer, "#0:00.000"), style);
    }
}
