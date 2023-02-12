using Steamworks;
using System.Runtime.Serialization.Json;
using UnityEngine;

namespace NeonWhiteQoL
{
    //internal class RestartCounter : MonoBehaviour
    //{
    //    Game game = Singleton<Game>.Instance;
    //    private Dictionary<String, int> dict = new();

    //    void Start()
    //    {
    //        game.OnLevelLoadComplete += OnLevelLoaded;
    //        SaveToFile();
    //    }

    //    private void OnLevelLoaded()
    //    {
    //        string currentLevel = game.GetCurrentLevel().levelID;

    //        if (dict.ContainsKey(currentLevel))
    //        {
    //            dict[currentLevel] += 1;
    //        }
    //        else
    //        {
    //            dict[currentLevel] = 1;
    //        }
    //        SaveToFile();
    //    }
    //    private void SaveToFile()
    //    {
    //        if (!SteamManager.Initialized)
    //        {
    //            return;
    //        }

    //        string path = Application.persistentDataPath + "\\" + SteamUser.GetSteamID().m_SteamID.ToString() + "\\restartcounter.txt";
    //        Stream stream = File.Open(path, FileMode.Create);

    //        Debug.Log(path);
    //        DataContractJsonSerializer serializer = new(typeof(Dictionary<string, int>));
    //        serializer.WriteObject(stream, dict);

    //        stream.Close();
    //        Debug.Log("closed");
    //    }

    //}
}
