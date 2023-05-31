using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonWhiteQoL
{

    //things i want added:
    // - current level name + IL pb or level rush level pb (if possible)
    // - next level name + IL PB or the latter
    // - previous level name + how much time saved/IL time, whatevers
    // - level rush restart count (how many resets per session)
    // - seed
    // - level progress (1/96 in this example) USE THIS -> LevelRush.m_currentLevelRush.currentLevelIndex;   
    // - timer
    // - adjustable alpha/settings
    // - autosplitter (if possible, should be made easier)

    // important classes: LevelRushStats, LevelRush

    // important thingies: LevelRush.GetCurrentLevelRushType()
    //                     GetCurrentLevelRushTimerMicroseconds() reuse conversion from other classes
    //  Debug.Log(LevelRush.GetNumLevelsInRush(LevelRush.LevelRushType.WhiteRush)); [replace WhiteRush with other rushes]
    // OnLevelLoadComplete (can be used to track level splits, reset counter, etc.)

    //make it so that once you load into the map (level) it checks for if it's a LevelRush, if not do not activate, but if so, spam Console Commands Debug Log

    public class LevelRushHelper : MonoBehaviour
    {
        private static int restarts = 0;
        private Color color = new Color(1, 1, 1, 0.5f);
        private Rect boxRect = new Rect(0, 200, 375, 700);
        private Rect labelRect = new Rect(10, 600, 1000, 700);

        private readonly GUIStyle style = new GUIStyle()
        {
            font = Resources.Load("fonts/nova_mono/novamono") as Font,
            fontSize = 20
        };

        public static void Initialize()
        {
            MethodInfo method = typeof(LevelRush).GetMethod("ClearLevelRushStats", BindingFlags.Public | BindingFlags.Static);
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(LevelRushHelper).GetMethod("PostClearLevelRushStats"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);

            method = typeof(MenuScreenLevelRush).GetMethod("StartLevelRush");
            harmonyMethod = new HarmonyMethod(typeof(LevelRushHelper).GetMethod("PostStartLevelRush"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);

            Singleton<Game>.Instance.OnLevelLoadComplete += DisplayHelper;

            //method = typeof(LevelRush).GetMethod("GetCurrentLevelRushLevelData");
            //harmonyMethod = new HarmonyMethod(typeof(LevelRushHelper).GetMethod("CurrentLevelName"));
            //NeonLite.Harmony.Patch(method, null, harmonyMethod);
        }

        private static void DisplayHelper()
        {
            if (!LevelRush.IsLevelRush() || false || SceneManager.GetActiveScene().name == "Menu") return;

            new GameObject("DisplayHelper", typeof(LevelRushHelper));
        }

        public static void PostClearLevelRushStats()
        {
            restarts++;
        }

        public static void PostStartLevelRush()
        {
            restarts = 0;
        }
        void Start()
        {
            style.normal.background = Texture2D.blackTexture;
        }



        void OnGUI()
        {
            GUI.backgroundColor = color;
            GUI.Box(boxRect, "");
            GUI.Label(labelRect, "hello world hello world hello world " + restarts);
        }

    }
}
