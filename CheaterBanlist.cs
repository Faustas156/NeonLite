using HarmonyLib;
using Steamworks;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace NeonWhiteQoL
{
    public class CheaterBanlist : MonoBehaviour
    {
        // the private static attribute below is used for debugging purposes and getting steamids lol
        private static FieldInfo currentLeaderboardEntriesGlobal = typeof(LeaderboardIntegrationSteam).GetField("currentLeaderboardEntriesGlobal", BindingFlags.NonPublic | BindingFlags.Static);
        public static bool isLoaded = false;
        public static bool? friendsOnly = null;
        public static int globalRank;
        public static List<int> cheaters = new();
        public static ulong[] bannedIDs;
        public static string test = string.Empty;

        public void Start()
        {
            StartCoroutine(DownloadCheaters());

            MethodInfo target = typeof(LeaderboardIntegrationSteam).GetMethod("GetScoreDataAtGlobalRank", BindingFlags.Static | BindingFlags.Public);
            HarmonyMethod patch = new(typeof(CheaterBanlist).GetMethod("PreGetScoreDataAtGlobalRank"));
            NeonLite.Harmony.Patch(target, patch);

            target = typeof(SteamUserStats).GetMethod("GetDownloadedLeaderboardEntry", BindingFlags.Static | BindingFlags.Public);
            patch = new(typeof(CheaterBanlist).GetMethod("PostGetDownloadedLeaderboardEntry"));
            NeonLite.Harmony.Patch(target, null, patch);

            target = typeof(LeaderboardScore).GetMethod("SetScore");
            patch = new(typeof(CheaterBanlist).GetMethod("PostSetScore"));
            NeonLite.Harmony.Patch(target, null, patch);

            target = typeof(Leaderboards).GetMethod("DisplayScores_AsyncRecieve");
            patch = new(typeof(CheaterBanlist).GetMethod("PostDisplayScores_AsyncRecieve"));
            NeonLite.Harmony.Patch(target, null, patch);
        }

        public IEnumerator DownloadCheaters()
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get("https://raw.githubusercontent.com/Faustas156/NeonLiteBanList/main/banlist.txt"))
            {
                yield return webRequest.SendWebRequest();

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
                        test = webRequest.downloadHandler.text;
                        string[] downloadedCheaters = webRequest.downloadHandler.text.Split();
                        bannedIDs = new ulong[downloadedCheaters.Length];
                        for (int i = 0; i < downloadedCheaters.Length; i++)
                        {
                            bannedIDs[i] = ulong.Parse(GetNumbers(downloadedCheaters[i]));
                        }
                        isLoaded = true;
                        break;
                }
            }
        }
        private static string GetNumbers(string input)
        {
            return new string(input.Where(c => char.IsDigit(c)).ToArray());
        }

        public static void PreGetScoreDataAtGlobalRank(ref int globalRank, ref bool friendsOnly, ref bool globalNeonRanking)
        {
            CheaterBanlist.friendsOnly = friendsOnly;
            CheaterBanlist.globalRank = globalRank;
        }

        public static void PostGetDownloadedLeaderboardEntry(ref SteamLeaderboardEntries_t hSteamLeaderboardEntries, ref int index, LeaderboardEntry_t pLeaderboardEntry, ref int[] pDetails, ref int cDetailsMax, ref bool __result)
        {
            if (friendsOnly != null && pLeaderboardEntry.m_steamIDUser.m_SteamID != 0 && bannedIDs.Contains(pLeaderboardEntry.m_steamIDUser.m_SteamID))
                cheaters.Add((bool)friendsOnly ? globalRank : pLeaderboardEntry.m_nGlobalRank);
            friendsOnly = null;
        }

        public static void PostSetScore(LeaderboardScore __instance, ref ScoreData newData, ref bool globalNeonRankings)
        {
            //SteamUserStats.GetDownloadedLeaderboardEntry((SteamLeaderboardEntries_t)currentLeaderboardEntriesGlobal.GetValue(null), (newData._ranking - 1) % 10, out LeaderboardEntry_t leaderboardEntry_t, new int[1], 1);
            //Debug.Log(leaderboardEntry_t.m_steamIDUser.m_SteamID + " " + newData._ranking);
            if (!cheaters.Contains(newData._ranking)) return;

            __instance._ranking.color = Color.red;
            __instance._username.color = Color.red;
            __instance._scoreValue.color = Color.red;
        }

        public static void PostDisplayScores_AsyncRecieve() => cheaters.Clear();
    }
}
