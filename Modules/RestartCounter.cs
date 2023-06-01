using Steamworks;
using System.Runtime.Serialization.Json;
using UnityEngine;

namespace NeonWhiteQoL.Modules
{
    internal class RestartCounter : MonoBehaviour
    {
        private static Dictionary<string, int> dict;
        private static string currentLevel;
        private static int restarts;

        private readonly Game game = Singleton<Game>.Instance;

        private readonly GUIStyle style = new()
        {
            font = Resources.Load("fonts/nova_mono/novamono") as Font,
            fontSize = 20
        };

        public static void Initialize()
        {
            if (!SteamManager.Initialized) return;

            string path = Application.persistentDataPath + "\\" + SteamUser.GetSteamID().m_SteamID.ToString() + "\\restartcounter.txt";

            if (!File.Exists(path))
            {
                dict = new();
                SaveToFile();
                return;
            }

            Stream stream = File.Open(path, FileMode.Open);

            DataContractJsonSerializer serializer = new(typeof(Dictionary<string, int>));
            dict = (Dictionary<string, int>)serializer.ReadObject(stream);

            stream.Close();

        }

        private static void SaveToFile()
        {
            if (!SteamManager.Initialized) return;

            string path = Application.persistentDataPath + "\\" + SteamUser.GetSteamID().m_SteamID.ToString() + "\\restartcounter.txt";

            Stream stream = File.Open(path, FileMode.Create);
            DataContractJsonSerializer serializer = new(typeof(Dictionary<string, int>));
            serializer.WriteObject(stream, dict);
            stream.Close();
        }

        void Start()
        {
            style.normal.textColor = Color.white;
            string newLevel = game.GetCurrentLevel().levelID;

            if (currentLevel != newLevel)
            {
                currentLevel = newLevel;
                restarts = 0;
            }

            restarts++;

            if (dict.ContainsKey(currentLevel))
                dict[currentLevel] = dict[currentLevel] + 1;
            else
                dict[currentLevel] = 1;
            SaveToFile();
        }

        void OnGUI()
        {
            GUI.Label(new Rect(10, 40, 100, 70), "Total Restarts: " + dict[currentLevel], style);
            GUI.Label(new Rect(10, 60, 100, 70), "Restarts: " + restarts, style);
        }

    }
}
