using HarmonyLib;
using Steamworks;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using static MelonLoader.MelonLogger;

namespace NeonWhiteQoL
{
    public class CheaterBanlist
    {

        private static int page = 0;
        private static readonly FieldInfo _leaderboadsRefInfo = typeof(LeaderboardIntegrationSteam).GetField("leaderboardsRef", BindingFlags.NonPublic | BindingFlags.Static);
        private static FieldInfo x = typeof(LeaderboardIntegrationSteam).GetField("currentLeaderboardEntriesGlobal", BindingFlags.NonPublic | BindingFlags.Static);
        private static FieldInfo z = typeof(LeaderboardIntegrationSteam).GetField("globalNeonRankingsRequest", BindingFlags.NonPublic | BindingFlags.Static);

        //use as reference later
        //GetScoreDataAtGlobalRank displays your current rank value (might be able to get steamIDs?)
        //if you wanna do visual stuff, make sure to check the Leaderboards class -> SetModeGlobalNeonScore

        public static void Initialize()
        {

            MethodInfo target = typeof(LeaderboardIntegrationSteam).GetMethod("OnLeaderboardScoreDownloadGlobalResult2");
            HarmonyMethod patch = new(typeof(CheaterBanlist).GetMethod("GlobalResults"));
            NeonLite.Harmony.Patch(target, patch);

            target = typeof(LeaderboardIntegrationSteam).GetMethod("GetScoreDataAtGlobalRank");
            patch = new(typeof(CheaterBanlist).GetMethod("PreScoreDataGlobal"));
            NeonLite.Harmony.Patch(target, patch);
        }
        public static void GlobalResults(ref LeaderboardScoresDownloaded_t pCallback, ref bool bIOFailure)
        {
            if (bIOFailure) return;

            var y = x.GetValue(null);
            y = pCallback.m_hSteamLeaderboardEntries;
            UnityEngine.Debug.Log(pCallback.m_hSteamLeaderboardEntries);

            ScoreData[] array = new ScoreData[10];
            for (int i = 0; i < array.Length; i++)
            {
                var yy = z.GetValue(null);
                array[i] = LeaderboardIntegrationSteam.GetScoreDataAtGlobalRank(i + 1, false, (bool)yy);
                UnityEngine.Debug.Log(array[i]);
            }
            bool flag = false;
            for (int j = 0; j < array.Length; j++)
            {
                if (array[j]._init)
                {
                    flag = true;
                }
            }
        }

        public static bool PreScoreDataRank(ref int globalRank, ref bool friendsOnly, ref bool globalNeonRanking, ScoreData scoreData)
        {
            if (!SteamManager.Initialized) return false;

            int[] array = new int[1];

            LeaderboardEntry_t leaderboardEntry_t;

            if (friendsOnly)
            {
                SteamUserStats.GetDownloadedLeaderboardEntry(LeaderboardIntegrationSteam.currentLeaderboardEntriesFriends, globalRank - 1, out leaderboardEntry_t, array, 1);
            }
            else
            {
                SteamUserStats.GetDownloadedLeaderboardEntry(LeaderboardIntegrationSteam.currentLeaderboardEntriesGlobal, globalRank - 1, out leaderboardEntry_t, array, 1);
            }
            int num = 0;
            int num2 = -1;
            int num3 = -1;
            if (LeaderboardIntegrationSteam.m_levelRushType != LevelRush.LevelRushType.None)
            {
                LeaderboardScoreCalculation.GetLevelRushScoreData(leaderboardEntry_t.m_nScore, array[0], out num);
            }
            else if (globalNeonRanking)
            {
                LeaderboardScoreCalculation.GetGlobalNeonScoreData(leaderboardEntry_t.m_nScore, array[0], out num);
            }
            else
            {
                LeaderboardScoreCalculation.GetLevelScoreData(leaderboardEntry_t.m_nScore, LeaderboardIntegrationSteam.currentLevelData, out num, out num2);
            }
            Texture2D steamImageAsTexture2D = LeaderboardIntegrationSteam.GetSteamImageAsTexture2D(SteamFriends.GetMediumFriendAvatar(leaderboardEntry_t.m_steamIDUser));
            bool flag2 = leaderboardEntry_t.m_nGlobalRank == LeaderboardIntegrationSteam.leaderboardsRef.GetUserRanking();
            return new ScoreData(friendsOnly ? globalRank : leaderboardEntry_t.m_nGlobalRank, (flag2 && !friendsOnly) ? LeaderboardIntegrationSteam.previousUserRanking : (-1), steamImageAsTexture2D, SteamFriends.GetFriendPersonaName(leaderboardEntry_t.m_steamIDUser), (long)num, num2, flag2, num3, flag);
        }
    }
}
