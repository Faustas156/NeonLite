﻿using HarmonyLib;
using MelonLoader.TinyJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

#if !XBOX
using Steamworks;
#endif

namespace NeonLite.Modules.Misc
{
    internal class Cheaters : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        public static ulong[] bannedIDs;
        static readonly HashSet<int> activeCheaters = [];

#if XBOX
        const string filename = "cheaterlist-xbox.json";
#else
        const string filename = "cheaterlist.json";
#endif
        const string URL = "https://raw.githubusercontent.com/Faustas156/NeonLite/main/Resources/" + filename;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "cheaters", "Cheater Banlist", "Highlights known cheaters as red on the leaderboard.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;

            Helpers.DownloadURL(URL, request =>
            {
                string backup = Path.Combine(Helpers.GetSaveDirectory(), "NeonLite", filename);
                Helpers.CreateDirectories(backup);
                var load = request.result == UnityEngine.Networking.UnityWebRequest.Result.Success && Load(request.downloadHandler.text);
                if (load)
                    File.WriteAllText(backup, request.downloadHandler.text);
                else if (!File.Exists(backup) || !Load(File.ReadAllText(backup)))
                {
                    NeonLite.Logger.Warning("Could not load up to date cheater list. Loading the backup resource; this could be really outdated!");
#if XBOX
                    var resource = Resources.r.cheaterlist_xbox;
#else
                    var resource = Resources.r.cheaterlist;
#endif
                    if (!Load(Encoding.UTF8.GetString(resource)))
                        NeonLite.Logger.Error("Failed to load the cheater list.");
                }
            });
        }

        static bool Load(string js)
        {
            try
            {
                var variant = JSON.Load(js) as ProxyArray;

                bannedIDs = new ulong[variant.Count];
                for (int i = 0; i < variant.Count; i++)
                    bannedIDs[i] = variant[i];
            }
            catch (Exception e)
            {
                NeonLite.Logger.Error("Failed to parse cheaterlist:");
                NeonLite.Logger.Error(e);
                return false;
            }
            return true;
        }

#if XBOX
        static readonly MethodInfo ogtransform = AccessTools.Method(typeof(LeaderboardIntegrationBitcode), "TransformRankingsToScoreData");
#else
        static readonly MethodInfo ogscore = AccessTools.Method(typeof(LeaderboardIntegrationSteam), "GetScoreDataAtGlobalRank");
        static readonly MethodInfo ogdllb = AccessTools.Method(typeof(SteamUserStats), "GetDownloadedLeaderboardEntry");
#endif
        static readonly MethodInfo ogset = AccessTools.Method(typeof(LeaderboardScore), "SetScore");
        static readonly MethodInfo ogrecv = AccessTools.Method(typeof(Leaderboards), "DisplayScores_AsyncRecieve");

        static void Activate(bool activate)
        {
            if (activate)
            {
#if XBOX
                NeonLite.Harmony.Patch(ogtransform, prefix: Helpers.HM(PreTransformRankingsToScoreData));
#else
                NeonLite.Harmony.Patch(ogscore, prefix: Helpers.HM(PreGetScoreDataAtGlobalRank));
                NeonLite.Harmony.Patch(ogdllb, postfix: Helpers.HM(PostGetDownloadedLeaderboardEntry));
#endif
                NeonLite.Harmony.Patch(ogset, postfix: Helpers.HM(PostSetScore));
                NeonLite.Harmony.Patch(ogrecv, postfix: Helpers.HM(PostRecieve));
            }
            else
            {
#if XBOX
                NeonLite.Harmony.Unpatch(ogtransform, Helpers.MI(PreTransformRankingsToScoreData));
#else
                NeonLite.Harmony.Unpatch(ogscore, Helpers.MI(PreGetScoreDataAtGlobalRank));
                NeonLite.Harmony.Unpatch(ogdllb, Helpers.MI(PostGetDownloadedLeaderboardEntry));
#endif
                NeonLite.Harmony.Unpatch(ogset, Helpers.MI(PostSetScore));
                NeonLite.Harmony.Unpatch(ogrecv, Helpers.MI(PostRecieve));
            }

            active = activate;
        }

#if XBOX
        private static void PreTransformRankingsToScoreData(IReadOnlyList<BitCode.Platform.Leaderboards.Ranking> rankings)
        {
            if (bannedIDs == null)
                return;

            foreach (var ranking in rankings)
            {
                var user = (BitCode.Platform.PlayFab.PlayFabRemoteAccount)ranking.User;
                var id = Convert.ToUInt64(user.PlayerId, 16);
                if (bannedIDs.Contains(id))
                    activeCheaters.Add((int)ranking.Rank);
            }
        }
#else
        static bool? friendOnly;
        static int global;

        private static void PreGetScoreDataAtGlobalRank(ref int globalRank, ref bool friendsOnly)
        {
            friendOnly = friendsOnly;
            global = globalRank;
        }

        private static void PostGetDownloadedLeaderboardEntry(ref SteamLeaderboardEntries_t hSteamLeaderboardEntries, ref int index, LeaderboardEntry_t pLeaderboardEntry, ref int[] pDetails, ref int cDetailsMax, ref bool __result)
        {
            if (bannedIDs == null)
                return;
            if (friendOnly.HasValue && pLeaderboardEntry.m_steamIDUser.m_SteamID != 0 && bannedIDs.Contains(pLeaderboardEntry.m_steamIDUser.m_SteamID))
                activeCheaters.Add(friendOnly.Value ? global : pLeaderboardEntry.m_nGlobalRank);
            friendOnly = null;
        }
#endif

        private static void PostSetScore(LeaderboardScore __instance, ref ScoreData newData)
        {
            if (bannedIDs == null || !activeCheaters.Contains(newData._ranking))
            {
                // lrogue reference
                DateTime now = DateTime.Now;
                if (!newData._userScore || now.Month != 4 || now.Day != 1)
                    return;
            }

            __instance._ranking.color = Color.red;
            __instance._username.color = Color.red;
            __instance._scoreValue.color = Color.red;
        }

        private static void PostRecieve() => activeCheaters.Clear();
    }
}