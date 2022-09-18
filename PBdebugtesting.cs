using NeonWhiteQoL;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using MelonLoader;
using Microsoft.SqlServer.Server;
using TMPro;

[assembly: MelonInfo(typeof(PBdebugtesting), "Neon White QoL Test", "1.0.0", "Faustas & MOPPI")]

namespace NeonWhiteQoL
{
    public class PBdebugtesting : MelonMod
    {
        private static Game game;
        private static string delta = "";
        private static bool newbest;
        public override void OnApplicationLateStart()
        {
            game = Singleton<Game>.Instance;
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("NAMEHERE");
            MethodInfo method = typeof(Game).GetMethod("OnLevelWin");
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(PBdebugtesting).GetMethod("PreOnLevelWin"));
            harmony.Patch(method, harmonyMethod);

            method = typeof(MenuScreenResults).GetMethod("OnSetVisible");
            harmonyMethod = new HarmonyMethod(typeof(PBdebugtesting).GetMethod("PostOnSetVisible"));
            harmony.Patch(method, null, harmonyMethod);
        }
        public static bool PreOnLevelWin()
        {
            LevelInformation levelInformation = game.GetGameData().GetLevelInformation(game.GetCurrentLevel());
            long besttime = GameDataManager.levelStats[levelInformation.levelID].GetTimeBestMicroseconds();
            FieldInfo fi = game.GetType().GetField("_currentPlaythrough", BindingFlags.Instance | BindingFlags.NonPublic);
            LevelPlaythrough currentPlaythrough = (LevelPlaythrough)fi.GetValue(game);
            long newtime = currentPlaythrough.GetCurrentTimeMicroseconds();

            long deltatime = (besttime - newtime) / 1000;
            newbest = deltatime < 0;

            TimeSpan t = TimeSpan.FromMilliseconds((double)Math.Abs(deltatime));
            delta = (newbest ? "+" : "-") + string.Format("{0:0}:{1:00}.{2:000}",
                                                t.Minutes,
                                                t.Seconds,
                                                t.Milliseconds);
            return true;
        }
        public static void PostOnSetVisible()
        {
            GameObject bestText = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Results Panel/New Best Text");
            GameObject deltaTime = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Results Panel/Delta Time");

            if (deltaTime == null)
            {
                deltaTime = UnityEngine.Object.Instantiate(bestText, bestText.transform.parent);
                Debug.Log("object is here it exists xd");
                deltaTime.name = "Delta Time";
                deltaTime.transform.localPosition += new Vector3(-5, -30, 0);
                deltaTime.SetActive(true);
            }
            TextMeshProUGUI text = deltaTime.GetComponent<TextMeshProUGUI>();
            text.SetText(delta);
            text.color = newbest ? Color.red : Color.green;
        }
    }
}