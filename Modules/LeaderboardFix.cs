using HarmonyLib;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class LeaderboardFix : Module
    {
        private static int page = 0;


        [HarmonyPrefix]
        [HarmonyPatch(typeof(Leaderboards), "OnLeftArrowPressed")]
        private static bool PreOnLeftArrowPressed(ref bool ___friendsFilter)
        {
            if (___friendsFilter && page > 0)
                page--;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Leaderboards), "OnRightArrowPressed")]
        private static bool PreOnRightArrowPressed(ref bool ___friendsFilter)
        {
            if (___friendsFilter)
                page++;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LeaderboardIntegrationSteam), "DownloadEntries")]
        private static bool PreDownloadEntries(ref bool friend, ref bool globalNeonRankings, ref Leaderboards ___leaderboardsRef)
        {
            if (!friend) return true;

            if (!SteamManager.Initialized) return false;

            ScoreData[] array = new ScoreData[10];
            for (int i = 0; i < array.Length; i++)
                array[i] = LeaderboardIntegrationSteam.GetScoreDataAtGlobalRank(i + 1 + (page * 10), true, globalNeonRankings);

            ___leaderboardsRef.DisplayScores_AsyncRecieve(array, true);
            return false;
        }
    }
}