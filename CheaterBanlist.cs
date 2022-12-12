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
        //private static readonly FieldInfo _leaderboadsRefInfo = typeof(LeaderboardIntegrationSteam).GetField("leaderboardsRef", BindingFlags.NonPublic | BindingFlags.Static);
        //private static MethodInfo OnLoadComplete = typeof(LeaderboardIntegrationSteam).GetMethod("OnLoadComplete2", BindingFlags.NonPublic | BindingFlags.Static);
        //private static FieldInfo currentLeaderboardEntriesGlobal = typeof(LeaderboardIntegrationSteam).GetField("currentLeaderboardEntriesGlobal", BindingFlags.NonPublic | BindingFlags.Static);
        //private static FieldInfo globalNeonRankingsRequest = typeof(LeaderboardIntegrationSteam).GetField("globalNeonRankingsRequest", BindingFlags.NonPublic | BindingFlags.Static);
        public static bool isLoaded = false;
        public static bool? friendsOnly = null;
        public static List<int> cheaters = new();
        public static int globalRank;
        public static int counter = 0;
        public static ulong[] bannedIDs;
        public static string test = string.Empty;


        //use as reference later
        //GetScoreDataAtGlobalRank displays your current rank value (might be able to get steamIDs?)
        //if you wanna do visual stuff, make sure to check the Leaderboards class -> SetModeGlobalNeonScore

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

            //MethodInfo target = typeof(LeaderboardIntegrationSteam).GetMethod("OnLeaderboardScoreDownloadGlobalResult2");
            //HarmonyMethod patch = new(typeof(CheaterBanlist).GetMethod("GlobalResults"));
            //NeonLite.Harmony.Patch(target, patch);
        }

        public IEnumerator DownloadCheaters()
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get("https://raw.githubusercontent.com/Faustas156/NeonLite/main/testbanList.txt"))
            {
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(": HTTP Error: " + webRequest.error);
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
            if (friendsOnly != null && bannedIDs.Contains(pLeaderboardEntry.m_steamIDUser.m_SteamID))
            {
                cheaters.Add((bool)friendsOnly ? globalRank : pLeaderboardEntry.m_nGlobalRank);
            }
            friendsOnly = null;

            //__result = NativeMethods.ISteamUserStats_GetDownloadedLeaderboardEntry(CSteamAPIContext.GetSteamUserStats(), hSteamLeaderboardEntries, index, out pLeaderboardEntry, pDetails, cDetailsMax);
        }

        public static void PostSetScore(LeaderboardScore __instance, ref ScoreData newData, ref bool globalNeonRankings)
        {
            //LeaderboardEntry_t leaderboardEntry_t;
            //SteamUserStats.GetDownloadedLeaderboardEntry((SteamLeaderboardEntries_t)currentLeaderboardEntriesGlobal.GetValue(null), (newData._ranking - 1) % 10, out leaderboardEntry_t, new int[1], 1);
            //Debug.Log(newData._ranking + " " + newData._username + " " + leaderboardEntry_t.m_steamIDUser.m_SteamID + " " + leaderboardEntry_t.m_nGlobalRank);
            if (!cheaters.Contains(newData._ranking)) return;

            __instance._ranking.color = Color.red;
            __instance._username.color = Color.red;
            __instance._scoreValue.color = Color.red;

            if (++counter != cheaters.Count) return;
            cheaters.Clear();
            counter = 0;
        }


        // below all these comments is where you could remove certain people from the leaderboard, unfortunately this is too complex to program in, can cause long term issues, so for now, this has been left out, may be reworked in the future (hopefully).

        //specific reasons involve: steamapi issues, it's hard coded, no way to figure out, if you were to load in the first page, you could figure out the amount of cheaters and place yourself in the #4 (if you were #6 originally), but you could not figure out where to be placed if you were around #50 - #60
        //if we were to simply delete the cheaters instead, we would have empty gaps in the leaderboards, which would make it look ugly, and would be basically counterintuitive since the original proposed idea in my eyes was to remove cheaters + sort the players properly
        //if you request too many entries (to download entries) this could cause strain on both your pc and the steamapi servers (worst case scenario could break the leaderboards ENTIRELY), so we've avoided dealing with this for now
        //i really hope we can figure out the issue and solution for this. in the mean time, i am sorry. i hope that this current addition above won't be too disappointing.

        //public static bool GlobalResults(ref LeaderboardScoresDownloaded_t pCallback, ref bool bIOFailure) 
        //{
        //    if (bIOFailure)
        //    {
        //        OnLoadComplete.Invoke(null, new object[] { false, false, false });
        //        Debug.LogError("Failure downloading leaderboard scores.");
        //        return false;
        //    }

        //    currentLeaderboardEntriesGlobal.SetValue(null, pCallback.m_hSteamLeaderboardEntries);
        //    ScoreData[] array = new ScoreData[10];
        //    bool flag = false;
        //    for (int i = 0; i < array.Length; i++)
        //    {
        //        array[i] = LeaderboardIntegrationSteam.GetScoreDataAtGlobalRank(i + 1, false, (bool)globalNeonRankingsRequest.GetValue(null));
        //        if (array[i]._init)
        //            flag = true;
        //        Debug.Log(array[i]._username);
        //    }
        //    var x = LeaderboardIntegrationSteam.GetScoreDataAtGlobalRank(11, false, (bool)globalNeonRankingsRequest.GetValue(null));
        //    Debug.Log("out of bounds: " + x._username);
        //    Debug.LogError("loaded");
        //    ((Leaderboards)_leaderboadsRefInfo.GetValue(null)).DisplayScores_AsyncRecieve(array, flag);

        //    return false;
        //}
    }
}
