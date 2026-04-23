using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Web;
using HarmonyLib;
using I2.Loc;
using MelonLoader.TinyJSON;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

#pragma warning disable IDE0130
namespace NeonLite.Modules
{
    [Module]
    public static class SpeedrunCom
    {
        const bool priority = false;
        const bool active = true;

        class Request(string path)
        {
            const string URL = "https://www.speedrun.com/api/v1";
            readonly string path = path;
            readonly NameValueCollection query = HttpUtility.ParseQueryString("");

            internal Request Add(string key, string value)
            {
                query.Add(key, value);
                return this;
            }

            internal UnityWebRequest Get()
            {
                var urib = new UriBuilder(URL);
                urib.Path += path;
                if (query != null)
                    urib.Query = query.ToString();
                var req = UnityWebRequest.Get(urib.ToString());
                req.SetRequestHeader("User-Agent", $"NeonLite/{VersionText.ver}");
                return req;
            }

            internal void Get(Action<Variant> callback)
            {
                var req = Get();
                req.SendWebRequest().completed += _ =>
                {
                    try
                    {
                        if (req.result != UnityWebRequest.Result.Success)
                            throw new HttpRequestException(req.error);

                        callback.Invoke(JSON.Load(req.downloadHandler.text));
                    }
                    catch (Exception e)
                    {
                        NeonLite.Logger.Warning($"Failed to fetch from SRC{path}?{query}:");
                        NeonLite.Logger.Error(e);
                    }
                };
            }
        }

        static void Activate(bool _)
        {
            Patching.AddPatch(typeof(Leaderboards), "SetLevel", FetchWR, Patching.PatchTarget.Postfix);
            Patching.AddPatch(typeof(Leaderboards), "DisplayScores_AsyncRecieve", CheckWR, Patching.PatchTarget.Postfix);

            new Request("/games")
                .Add("abbreviation", "neon_white")
                .Add("embed", "categories,variables")
                .Get(json =>
                {
                    var gd = NeonLite.Game.GetGameData();
                    var gameData = json["data"][0];
                    gameID = gameData["id"];

                    var categories = gameData["categories"]["data"] as ProxyArray;
                    sysIDLevels = categories.First(x => x["name"] == SYSTEM)["id"];
                    rushIDCategory = categories.First(x => x["name"] == RUSH_CATEGORY)["id"];

                    var variables = gameData["variables"]["data"] as ProxyArray;
                    var sysVar = variables.First(x => x["name"] == SYSTEM_VAR);
                    sysIDFullVar = sysVar["id"];
                    sysIDSelect = (sysVar["values"]["values"] as ProxyObject)
                                    .First(kv => kv.Value["label"] == SYSTEM).Key;

                    var runVar = variables.First(x => x["name"] == RUNTYPE_VAR);
                    runTypeVar = runVar["id"];
                    runTypeNormal = (runVar["values"]["values"] as ProxyObject)
                                    .First(kv => kv.Value["label"] == RUNTYPE_NORM).Key;

                    var missions = gd.GetCampaigns().SelectMany(x => x.missionData);
                    foreach (var mission in missions)
                    {
                        var locale = LocalizationManager.GetTranslation(mission.missionDisplayName, overrideLanguage: "English");
                        var missionvar = variables.DefaultIfEmpty(null)
                            .FirstOrDefault(x => x["name"] == locale);
                        if (missionvar == null)
                            continue;

                        Level idbase = new()
                        {
                            mVarID = missionvar["id"],
                            mLevelID = missionvar["scope"]["level"]
                        };

                        var lvlvars = missionvar["values"]["values"] as ProxyObject;
                        NeonLite.Logger.DebugMsg(lvlvars.ToJSON());
                        foreach (var level in mission.levels)
                        {
                            locale = LocalizationManager.GetTranslation(level.GetLevelDisplayName(), overrideLanguage: "English");
                            if (lvlvars.Any(kv => kv.Value["label"] == locale))
                            {
                                var lvlvar = lvlvars.First(kv => kv.Value["label"] == locale);
                                levelIDs.Add(level.levelID, idbase with { levelID = lvlvar.Key });
                                _wrCache.Add("L/" + level.levelID, new());
                            }
                        }
                    }
                });
        }

        static string gameID;

#if !XBOX
        const string SYSTEM = "PC (Steam)";
#else
        const string SYSTEM = "Xbox/Game Pass";
#endif

        static string sysIDLevels;

        const string SYSTEM_VAR = "System";
        // use this for the *WHOLE VARIABLE*
        static string sysIDFullVar;
        // use this for SELECTING with the variable
        static string sysIDSelect;

        const string RUNTYPE_VAR = "Run Type";
        const string RUNTYPE_NORM = "Normal";
        static string runTypeVar;
        static string runTypeNormal;

        const string RUSH_CATEGORY = "Level Rush";
        static string rushIDCategory;

        record class Level
        {
            public string mLevelID;
            public string mVarID;

            public string levelID;
        }
        static readonly Dictionary<string, Level> levelIDs = [];
        static readonly Dictionary<string, WRCache> _wrCache = [];

        class WRCache
        {
            public long time = long.MinValue;
            internal DateTime lastFetched = DateTime.MinValue;
        }

        static readonly string[] TIME_FORMATS_SRC = [
            @"\P\Th\Hm\Ms\.fff\S",
            @"\P\Tm\Ms\.fff\S",
            @"\P\Ts\.fff\S",
        ];

        static void FetchLevelWR(string levelID)
        {
            var cache = _wrCache["L/" + levelID];
            var idStore = levelIDs[levelID];
            cache.lastFetched = DateTime.UtcNow;
            new Request($"/leaderboards/{gameID}/level/{idStore.mLevelID}/{sysIDLevels}")
                .Add("top", "1")
                .Add($"var-{idStore.mVarID}", $"{idStore.levelID}") // select the level
                .Add($"var-{runTypeVar}", $"{runTypeNormal}") // ensure normal
                .Get(json =>
                {
                    var times = json["data"]["runs"] as ProxyArray;
                    if (times.Count <= 0)
                        return; // lol this mf has no runs

                    var timespan = TimeSpan.ParseExact(times[0]["run"]["times"]["primary"], TIME_FORMATS_SRC, CultureInfo.InvariantCulture);
                    cache.time = (long)(timespan.TotalMilliseconds * 1000);
                });
        }

        public static long GetLevelWR(string levelID)
        {
            var cache = _wrCache["L/" + levelID];
            if (cache.lastFetched <= DateTime.UtcNow.AddMinutes(-1))
                FetchLevelWR(levelID);
            return cache.time;
        }


        static void FetchWR(LevelData newData) => GetLevelWR(newData.levelID);

        static readonly string TIME_FORMAT_LB = @"m\:ss\.fff";
        static void CheckWR(bool atleastOneEntry, LevelData ___currentLevelData, List<GameObject> ___createdScores)
        {
            if (!atleastOneEntry || !___currentLevelData)
                return;

            var wr = GetLevelWR(___currentLevelData.levelID);
            if (wr == long.MinValue)
                return;
            wr /= 1000;
            foreach (var lbscore in ___createdScores.Select(x => x.GetComponent<LeaderboardScore>()))
            {
                var timespan = TimeSpan.ParseExact(lbscore._scoreValue.text, TIME_FORMAT_LB, CultureInfo.InvariantCulture);
                if (wr <= timespan.TotalMilliseconds)
                    continue;

                lbscore.GetOrAddComponent<CanvasGroup>().alpha = 0.6f;
                lbscore.GetComponentsInChildren<TextMeshProUGUI>().Do(x => x.fontStyle |= FontStyles.Italic);
                lbscore.GetComponentsInChildren<Text>().Do(x => x.fontStyle |= FontStyle.Italic);
            }
        }
    }
}
