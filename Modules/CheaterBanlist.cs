using HarmonyLib;
using MelonLoader;
using Steamworks;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class CheaterBanlist : Module
    {
        private static MelonPreferences_Entry<bool> enable_Banlist;
        private static bool listReady = false;
        private readonly string _filename = "cheaterlist.json";

        // the private static attribute below is used for debugging purposes and getting steamids lol
        private static readonly FieldInfo currentLeaderboardEntriesGlobal = typeof(LeaderboardIntegrationSteam).GetField("currentLeaderboardEntriesGlobal", NeonLite.s_privateStatic);
        private static bool? friendsOnly = null;
        private static int globalRank;
        private static readonly List<int> cheaters = new();
        private static ulong[] bannedIDs;

        public CheaterBanlist()
        {
            NeonLite.DownloadRessource<ulong[]>("https://raw.githubusercontent.com/MOPSKATER/NeonLite/main/Resources/cheaterlist.json", result =>
            {
                string path;
                if (result.success)
                {
                    bannedIDs = (ulong[])result.data;
                    listReady = true;

                    if (!GetFilePath(out path)) return;

                    path = Application.persistentDataPath + "/" + SteamUser.GetSteamID().m_SteamID.ToString() + "/NeonLite";
                    NeonLite.SaveToFile<ulong[]>(path, _filename, bannedIDs); //Download succeeded => create local file
                }

                //Download failed => find local file
                else if (GetFilePath(out path) && File.Exists(path))
                {
                    Debug.Log("TEST");
                    bannedIDs = NeonLite.ReadFile<ulong[]>(path, _filename);
                }
                else
                    //Local file not found => read file from resources
                    bannedIDs = NeonLite.ReadFile<ulong[]>(Properties.Resources.cheaterlist);
                listReady = true;

            });

            return;
            enable_Banlist = NeonLite.neonLite_config.CreateEntry("Enable Community Banlist", true, description: "Cheaters are colored red");
            UnityWebRequest webRequest = UnityWebRequest.Get("https://raw.githubusercontent.com/MOPSKATER/NeonLite/main/Resources/cheaterlist.json");
            webRequest.SendWebRequest().completed +=
                result =>
                {
                    string[] downloadedCheaters = null;
                    switch (webRequest.result)
                    {
                        case UnityWebRequest.Result.ConnectionError:
                        case UnityWebRequest.Result.DataProcessingError:
                            Debug.LogError("Error: " + webRequest.error);
                            break;
                        case UnityWebRequest.Result.ProtocolError:
                            Debug.LogError("HTTP Error: " + webRequest.error);
                            break;
                        case UnityWebRequest.Result.Success:
                            downloadedCheaters = webRequest.downloadHandler.text.Split();
                            bannedIDs = new ulong[downloadedCheaters.Length];
                            for (int i = 0; i < downloadedCheaters.Length; i++)
                            {
                                if (downloadedCheaters[i] != "")
                                    bannedIDs[i] = ulong.Parse(GetNumbers(downloadedCheaters[i]));
                            }
                            listReady = true;
                            break;
                    }
                    if (!SteamManager.Initialized && downloadedCheaters != null) return;
                    string path = Application.persistentDataPath + "/" + SteamUser.GetSteamID().m_SteamID.ToString() + "/NeonLite";

                    if (listReady)
                        //Download succeeded => create local file
                        NeonLite.SaveToFile<ulong[]>(path, _filename, bannedIDs);

                    //Download failed => find local file
                    else if (File.Exists(path))
                        bannedIDs = NeonLite.ReadFile<ulong[]>(path, _filename);

                    else
                        //Local file not found => read file from resources
                        bannedIDs = NeonLite.ReadFile<ulong[]>(Properties.Resources.cheaterlist);
                    listReady = true;
                };
        }

        private bool GetFilePath(out string path)
        {
            path = null;
            if (!SteamManager.Initialized) return false;
            
            path = Application.persistentDataPath + "/" + SteamUser.GetSteamID().m_SteamID.ToString() + "/NeonLite";
            return true;
        }

        private static string GetNumbers(string input) => new(input.Where(c => char.IsDigit(c)).ToArray());

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LeaderboardIntegrationSteam), "GetScoreDataAtGlobalRank")]
        private static void PreGetScoreDataAtGlobalRank(ref int globalRank, ref bool friendsOnly)
        {
            CheaterBanlist.friendsOnly = friendsOnly;
            CheaterBanlist.globalRank = globalRank;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamUserStats), "GetDownloadedLeaderboardEntry")]
        private static void PostGetDownloadedLeaderboardEntry(ref SteamLeaderboardEntries_t hSteamLeaderboardEntries, ref int index, LeaderboardEntry_t pLeaderboardEntry, ref int[] pDetails, ref int cDetailsMax, ref bool __result)
        {
            if (!enable_Banlist.Value || !listReady) return;
            if (friendsOnly != null && pLeaderboardEntry.m_steamIDUser.m_SteamID != 0 && bannedIDs.Contains(pLeaderboardEntry.m_steamIDUser.m_SteamID))
                cheaters.Add((bool)friendsOnly ? globalRank : pLeaderboardEntry.m_nGlobalRank);
            friendsOnly = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LeaderboardScore), "SetScore")]
        private static void PostSetScore(LeaderboardScore __instance, ref ScoreData newData)
        {
            //SteamUserStats.GetDownloadedLeaderboardEntry((SteamLeaderboardEntries_t)currentLeaderboardEntriesGlobal.GetValue(null), (newData._ranking - 1) % 10, out LeaderboardEntry_t leaderboardEntry_t, new int[1], 1);
            //Debug.Log(leaderboardEntry_t.m_steamIDUser.m_SteamID + " " + newData._ranking);

            if (!(enable_Banlist.Value && listReady && cheaters.Contains(newData._ranking))) return;

            __instance._ranking.color = Color.red;
            __instance._username.color = Color.red;
            __instance._scoreValue.color = Color.red;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Leaderboards), "DisplayScores_AsyncRecieve")]
        private static void PostDisplayScores_AsyncRecieve() => cheaters.Clear();
    }
}
