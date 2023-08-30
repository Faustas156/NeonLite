using HarmonyLib;
using MelonLoader;
using Steamworks;
using System.Reflection;
using UnityEngine;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class CheaterBanlist : Module
    {
        private const string FILENAME = "cheaterlist.json";
        private const string URL = "https://raw.githubusercontent.com/MOPSKATER/NeonLite/main/Resources/cheaterlist.json";
        private static MelonPreferences_Entry<bool> _setting_Banlist;

        // the private static attribute below is used for debugging purposes and getting steamids lol
        private static readonly FieldInfo currentLeaderboardEntriesGlobal = typeof(LeaderboardIntegrationSteam).GetField("currentLeaderboardEntriesGlobal", NeonLite.s_privateStatic);
        private static bool? friendsOnly = null;
        private static int globalRank;
        private static readonly List<int> cheaters = new();
        private static ulong[] bannedIDs;

        public CheaterBanlist()
        {
            _setting_Banlist = NeonLite.Config_NeonLite.CreateEntry("Enable Cheater Banlist", true, description: "Cheaters will be displayed red");

            RessourcesUtils.DownloadRessource<ulong[]>(URL, result =>
            {
                string path;
                if (result.success)
                {
                    bannedIDs = (ulong[])result.data;

                    if (bannedIDs == null || bannedIDs.Length == 0)
                    {
                        Debug.LogWarning("List of banned IDs empty");
                        return;
                    }

                    //Download succeeded => create local file
                    if (RessourcesUtils.GetDirectoryPath(out path))
                        RessourcesUtils.SaveToFile<ulong[]>(path, FILENAME, bannedIDs);
                    return;
                }

                else if (RessourcesUtils.GetDirectoryPath(out path) && File.Exists(path + FILENAME))
                    //Download failed => find local file
                    bannedIDs = RessourcesUtils.ReadFile<ulong[]>(path, FILENAME);
                else
                    //Local file not found => read file from resources
                    bannedIDs = RessourcesUtils.ReadFile<ulong[]>(Properties.Resources.cheaterlist);
            });
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
            if (!_setting_Banlist.Value || bannedIDs == null) return;
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

            if (!(_setting_Banlist.Value && bannedIDs != null && cheaters.Contains(newData._ranking))) return;

            __instance._ranking.color = Color.red;
            __instance._username.color = Color.red;
            __instance._scoreValue.color = Color.red;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Leaderboards), "DisplayScores_AsyncRecieve")]
        private static void PostDisplayScores_AsyncRecieve() => cheaters.Clear();
    }
}
