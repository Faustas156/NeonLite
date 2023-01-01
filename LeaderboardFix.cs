using HarmonyLib;
using System.Reflection;

namespace NeonWhiteQoL
{
    public class LeaderboardFix
    {
        private static int page = 0;
        private static readonly FieldInfo _leaderboadsRefInfo = typeof(LeaderboardIntegrationSteam).GetField("leaderboardsRef", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo friendsFilter = typeof(Leaderboards).GetField("friendsFilter", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Initialize()
        {
            HarmonyLib.Harmony harmony = new("de.MOPSKATER.LeaderboardFix");

            MethodInfo target = typeof(LeaderboardIntegrationSteam).GetMethod("DownloadEntries");
            HarmonyMethod patch = new(typeof(LeaderboardFix).GetMethod("PreDownloadEntries"));
            harmony.Patch(target, patch);

            target = typeof(Leaderboards).GetMethod("OnLeftArrowPressed");
            patch = new(typeof(LeaderboardFix).GetMethod("PreOnLeftArrowPressed"));
            harmony.Patch(target, patch);

            target = typeof(Leaderboards).GetMethod("OnRightArrowPressed");
            patch = new(typeof(LeaderboardFix).GetMethod("PreOnRightArrowPressed"));
            harmony.Patch(target, patch);
        }
        
        public static void ToggleMod(int value)
        {
            if (value == 0) 
            {
                Initialize();
                return;
            }
            
            MethodInfo method = typeof(LeaderboardIntegrationSteam).GetMethod("DownloadEntries");
            NeonLite.Harmony.Unpatch(method, HarmonyPatchType.Prefix);
            
            method = typeof(Leaderboards).GetMethod("OnLeftArrowPressed");
            NeonLite.Harmony.Unpatch(method, HarmonyPatchType.Prefix);
            
            method = typeof(Leaderboards).GetMethod("OnRightArrowPressed");
            NeonLite.Harmony.Unpatch(method, HarmonyPatchType.Prefix);
        }

        public static bool PreOnLeftArrowPressed(Leaderboards __instance)
        {

            if ((bool)friendsFilter.GetValue(__instance) && page > 0)
                page--;
            return true;
        }

        public static bool PreOnRightArrowPressed(Leaderboards __instance)
        {
            if ((bool)friendsFilter.GetValue(__instance))
                page++;
            return true;
        }

        public static bool PreDownloadEntries(ref int start, ref int end, ref bool friend, ref bool globalNeonRankings)
        {
            if (!friend) return true;

            if (!SteamManager.Initialized) return false;

            ScoreData[] array = new ScoreData[10];
            for (int i = 0; i < array.Length; i++)
                array[i] = LeaderboardIntegrationSteam.GetScoreDataAtGlobalRank(i + 1 + (page * 10), true, globalNeonRankings);

            Leaderboards leaderboard = (Leaderboards)_leaderboadsRefInfo.GetValue(null);
            leaderboard.DisplayScores_AsyncRecieve(array, true);
            return false;
        }
    }
}
