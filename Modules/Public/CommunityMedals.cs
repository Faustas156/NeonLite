using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using I2.Loc;
using MelonLoader;
using MelonLoader.TinyJSON;
using NeonLite.Modules.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable IDE0130
namespace NeonLite.Modules
{
    [Module(10)]
    public static class CommunityMedals
    {
#pragma warning disable CS0414
        const bool priority = false;
        static bool active = false;

        // All times (*including* bronze/silver/gold/ace)
        public static Dictionary<string, long[]> medalTimes = [];

        const string filename = "communitymedals.json";
        const string URL = "https://raw.githubusercontent.com/Faustas156/NeonLite/main/Resources/communitymedals.json";

        // All stamps (null for bronze/silver/gold/ace)
        public static Sprite[] Stamps { get; private set; }
        // All crystals (bronze for not done, silver+ for done, ... , modded)
        public static Sprite[] Crystals { get; private set; }
        // All medals
        public static Sprite[] Medals { get; private set; }
        // All colors (including custom ones for pre-dev)
        public static Color[] Colors { get; private set; } = [
            new Color32(0xD1, 0x66, 0x20, 0xFF),
            new Color32(0x54, 0x54, 0x54, 0xFF),
            new Color32(0xD1, 0x9C, 0x38, 0xFF),
            new Color32(0x49, 0xA6, 0x9F, 0xFF),
            new(0.420f, 0.015f, 0.043f),
            new(0.388f, 0.8f, 0.388f),
            new(0.674f, 0.313f, 0.913f),
            new(0.043f, 0.317f, 0.901f),
            new(0.976f, 0.341f, 0f) // todo: TODO: todo: CHANGE IF WE DECIDE TO IMPLEMENT IT!!!!!!
        ];

        public enum MedalEnum
        {
            Bronze,
            Silver,
            Gold,
            Ace,
            Dev,
            Emerald,
            Amethyst,
            Sapphire,
            Plus
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static MedalEnum E(int i) => (MedalEnum)i;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int I(MedalEnum e) => (int)e;


        static bool assetReadyUnderlying;
        public static bool Ready
        {
            get { return assetReadyUnderlying && medalTimes.Count != 0; }
            private set
            {
                assetReadyUnderlying = value;
            }
        }

        public static event Action AssetsFinished;
        static bool fetched;
        static bool loaded;

        public static MelonPreferences_Entry<bool> setting;
        internal static MelonPreferences_Entry<bool> oldStyle;
        internal static MelonPreferences_Entry<bool> hideOld;
        internal static MelonPreferences_Entry<bool> hideLeaderboard;
        public static MelonPreferences_Entry<float> hueShift;
        internal static MelonPreferences_Entry<string> overrideURL;
#if !XBOX
        internal static MelonPreferences_Entry<bool> uploadGlobal;
        public static MelonPreferences_Entry<LBDisplay> showGlobalMedals;
#endif
        public static Material HueShiftMat { get; private set; } = null;
        static Material defaultMat;

#if !XBOX
        const string LB_FILE = "nl_commedal";
#endif

        static void Setup()
        {
            setting = Settings.Add(Settings.h, "Medals", "comMedals", "Community Medals", "Shows new community medals past the developer red times to aim for.", true);
            hueShift = Settings.Add(Settings.h, "Medals", "hueShift", "Hue Shift", "Changes the hue of *all* medals (and related) help aid colorblind users in telling them apart.", 0f, new MelonLoader.Preferences.ValueRange<float>(0, 1));
            oldStyle = Settings.Add(Settings.h, "Medals", "oldStyle", "Stamp Style", "Display the community medals in the level info as it was pre-3.0.0.", false);
            hideOld = Settings.Add(Settings.h, "Medals", "hideOld", "Hide Times", "Hides unachieved medal times.", false);
            hideLeaderboard = Settings.Add(Settings.h, "Medals", "hideLeaderboard", "Hide Leaderboard Medals", "Unachieved medals will appear the same as your own on the leaderboards.", false);
#if !XBOX
            uploadGlobal = Settings.Add(Settings.h, "Medals", "uploadGlobal", "Upload to Global", "Whether to upload your medal data to global. This uploads all your level medals for other mods to potentially look at. Works with extended medals.", true);
            showGlobalMedals = Settings.Add(Settings.h, "Medals", "showGlobalMedals", "Global Medals to Display", "Which medals to show on the global leaderboard.", LBDisplay.Dev | LBDisplay.Emerald | LBDisplay.Amethyst | LBDisplay.Sapphire);
#endif
            overrideURL = Settings.Add(Settings.h, "Medals", "overrideURL", "Extension URL", "Specifies additional community medals JSON URL to apply on top of the existing community medals.", "");

            active = setting.SetupForModule(Activate, static (_, after) => after);
            hueShift.OnEntryValueChanged.Subscribe(static (_, after) => HueShiftMat?.SetFloat("_Shift", after));
            overrideURL.OnEntryValueChanged.Subscribe(static (_, after) => RefetchMedals());

            NeonLite.OnBundleLoad += AssetsDone;
#if !XBOX
            SteamLBFiles.OnLBWrite += OnSteamLBWrite;
            SteamLBFiles.RegisterForLoad(LB_FILE, OnSteamLBRead);
#endif
        }

        static bool Load(string js)
        {
            try
            {
                var variant = JSON.Load(js) as ProxyObject;

                foreach (var pk in variant)
                {
                    var level = NeonLite.Game.GetGameData().GetLevelData(pk.Key);
                    if (level == null)
                        continue;

                    List<long> community = [.. pk.Value as ProxyArray];
                    while (community.Count < 4)
                        community.Add(long.MinValue);

                    List<long> initial = [];

                    if (!level.isSidequest)
                    {
                        initial = [
                            long.MaxValue,
                            Utils.ConvertSeconds_FloatToMicroseconds(level.GetTimeSilver()),
                            Utils.ConvertSeconds_FloatToMicroseconds(level.GetTimeGold()),
                            Utils.ConvertSeconds_FloatToMicroseconds(level.GetTimeAce()),
                            Utils.ConvertSeconds_FloatToMicroseconds(level.GetTimeDev()),
                        ];
                    }
                    else
                    {
                        initial = [
                            long.MaxValue,
                            long.MaxValue,
                            long.MaxValue,
                            long.MaxValue,
                            long.MinValue, // so it travels down and hits ace instead of dev
                        ];

                    }

                    medalTimes[pk.Key] = [
                        .. initial,
                    .. community
                    ];
                }
            }
            catch (Exception e)
            {
                medalTimes.Clear();
                NeonLite.Logger.Error("Failed to parse community medals:");
                NeonLite.Logger.Error(e);
                return false;
            }
            return true;
        }

        public static int GetMedalIndex(string level, long time = -1)
        {
            var stats = GameDataManager.GetLevelStats(level);
            if (!stats.GetCompleted())
                return -1;

            if (time == -1)
                time = stats._timeBestMicroseconds;

            var times = medalTimes[level];
            for (int i = times.Length - 1; i >= 0; i--)
            {
                if (time <= times[i])
                    return i;
            }
            return 0;
        }

        public static void RefetchMedals()
        {
            fetched = false;
            OnLevelLoad(null);
        }

        static void DownloadOGMedals()
        {
            fetched = true;
            Helpers.DownloadURL(URL, request =>
            {
                string backup = Path.Combine(Helpers.GetSaveDirectory(), "NeonLite", filename);
                Helpers.CreateDirectories(backup);
                var load = request.result == UnityEngine.Networking.UnityWebRequest.Result.Success && Load(request.downloadHandler.text);
                if (load)
                    File.WriteAllText(backup, request.downloadHandler.text);
                else if (!File.Exists(backup) || !Load(File.ReadAllText(backup)))
                {
                    NeonLite.Logger.Warning("Could not load up to date community medals. Loading the backup resource; this could be really outdated!");
                    if (!Load(Resources.communitymedals.GetUTF8String()))
                        NeonLite.Logger.Error("Failed to load community medals.");
                }
                else
                    load = true;

                fetched = load;

                if (load)
                {
                    if (overrideURL.Value != "")
                    {
                        void FetchNext(string next)
                        {
                            var split = next.Split(['\n'], 2);
                            var url = split[0].Trim();

                            Helpers.DownloadURL(url, request =>
                            {
                                var load = request.result == UnityEngine.Networking.UnityWebRequest.Result.Success && Load(request.downloadHandler.text);
                                if (!load)
                                    NeonLite.Logger.Warning($"Failed to load extended community medals from URL {url}.");

                                if (split.Length <= 1)
                                    NeonLite.Logger.Msg("Finished loading extended community medals!");
                                else
                                    FetchNext(split[1]);
                            });
                        }
                        FetchNext(overrideURL.Value);
                    }
                    else
                        NeonLite.Logger.Msg("Fetched community medals!");
                }
            });
        }

        internal static void OnLevelLoad(LevelData _)
        {
            if (!fetched)
            {
                medalTimes.Clear();

                DownloadOGMedals();
            }

            if (loaded)
                AssetsDone(NeonLite.bundle);
        }

        static void Activate(bool activate)
        {
            OnLevelLoad(null);

            Patching.TogglePatch(activate, typeof(LevelInfo), "SetLevel", Helpers.HM(PostSetLevel).SetPriority(Priority.First), Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(MenuButtonLevel), "SetLevelData", PostSetLevelData, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(LeaderboardScore), "SetScore", PostSetScore, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(Game), "OnLevelWin", PreOnWin, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(MenuScreenResults), "SetMedal", PostSetMedal, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(MenuScreenResults), "OnSetVisible", PostSetVisible, Patching.PatchTarget.Postfix);

            if (!activate)
            {
                foreach (var li in UnityEngine.Object.FindObjectsOfType<LevelInfo>())
                    PostSetLevel(li, null); // for !active, revert stuff -- for active, setup some small stuff
            }

            active = activate;
        }

        public static Color AdjustedColor(Color color)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            h -= hueShift.Value;
            while (h < 0)
                h += 1;
            return Color.HSVToRGB(h, s, v);
        }
        public static void AdjustMaterial(Graphic graphic)
        {
            if (graphic.material != HueShiftMat)
                graphic.material = HueShiftMat;
        }

        static void AssetsDone(AssetBundle bundle)
        {
            loaded = true;
            NeonLite.Logger.DebugMsg("CommunityMedals onBundleLoad");
            if (!NeonLite.activateLate)
                return;
            loaded = false;

            var gamedata = NeonLite.Game.GetGameData();

            Medals = [
                gamedata.medalSprite_Bronze,
                gamedata.medalSprite_Silver,
                gamedata.medalSprite_Gold,
                gamedata.medalSprite_Ace,
                gamedata.medalSprite_Dev,
                bundle.LoadAsset<Sprite>("Assets/Sprites/MedalEmerald.png"),
                bundle.LoadAsset<Sprite>("Assets/Sprites/MedalAmethyst.png"),
                bundle.LoadAsset<Sprite>("Assets/Sprites/MedalSapphire.png"),
                bundle.LoadAsset<Sprite>("Assets/Sprites/MedalPlus.png"),
            ];

            var levelInfo = ((MenuScreenStaging)MainMenu.Instance()._screenStaging)
                    ._leaderboardsAndLevelInfoRef
                    .levelInfoRef;
            var devStamp = levelInfo.devStamp.transform
                    .Find("MikeyStampGraphic").GetComponent<Image>().sprite;

            Stamps = [
                null,
                null,
                null,
                null,
                devStamp,
                bundle.LoadAsset<Sprite>("Assets/Sprites/MikeyEmerald.png"),
                bundle.LoadAsset<Sprite>("Assets/Sprites/MikeyAmethyst.png"),
                bundle.LoadAsset<Sprite>("Assets/Sprites/MikeySapphire.png"),
                bundle.LoadAsset<Sprite>("Assets/Sprites/MikeyPlus.png"),
            ];

            Crystals = [
                levelInfo._crystalSpriteSidequestEmpty,
                levelInfo._crystalSpriteSidequestFilled,
                levelInfo._crystalSpriteSidequestFilled,
                levelInfo._crystalSpriteSidequestFilled,
                levelInfo._crystalSpriteSidequestFilled,
                bundle.LoadAsset<Sprite>("Assets/Sprites/CrystalEmerald.png"),
                bundle.LoadAsset<Sprite>("Assets/Sprites/CrystalAmethyst.png"),
                bundle.LoadAsset<Sprite>("Assets/Sprites/CrystalSapphire.png"),
                bundle.LoadAsset<Sprite>("Assets/Sprites/CrystalPlus.png"),
            ];

            HueShiftMat = bundle.LoadAsset<Material>("Assets/Material/HueShift.mat");
            HueShiftMat.SetFloat("_Shift", hueShift.Value);

            Ready = true;
            AssetsFinished?.Invoke();
        }

        static readonly MethodInfo styleTime = Helpers.Method(typeof(LevelInfo), "StyleMedalTime");

        static void PostSetLevel(LevelInfo __instance, LevelData level)
        {
            if (!defaultMat)
                defaultMat = __instance._crystalHolderFilledImage.material;

            if (!Ready)
                return;

            Image aceImage = __instance._aceMedalBG.transform.parent.Find("Medal Icon").GetComponent<Image>();
            Image goldImage = __instance._goldMedalBG.transform.parent.Find("Medal Icon").GetComponent<Image>();
            Image silverImage = __instance._silverMedalBG.transform.parent.Find("Medal Icon").GetComponent<Image>();

            Image[] stamps = __instance.devStamp.GetComponentsInChildren<Image>();
            if (stamps.Length < 3) return;

            if (!active || level == null || !medalTimes.ContainsKey(level.levelID))
            {
                aceImage.sprite = Medals[I(MedalEnum.Ace)];
                goldImage.sprite = Medals[I(MedalEnum.Gold)];
                silverImage.sprite = Medals[I(MedalEnum.Silver)];

                stamps[1].sprite = Stamps[I(MedalEnum.Dev)];
                stamps[2].sprite = Stamps[I(MedalEnum.Dev)];

                __instance.devTime.color = Colors[I(MedalEnum.Dev)];
                DestroyNextTime(__instance);

                return;
            }


            GameData gameData = NeonLite.Game.GetGameData();
            LevelStats levelStats = gameData.GetLevelStats(level.levelID);

            if (!levelStats.GetCompleted()) return;

            AdjustMaterial(stamps[1]);
            AdjustMaterial(stamps[2]);

            AdjustMaterial(aceImage);
            AdjustMaterial(goldImage);
            AdjustMaterial(silverImage);

            AdjustMaterial(__instance._levelMedal);
            if (level.isSidequest)
                AdjustMaterial(__instance._crystalHolderFilledImage);
            else
                __instance._crystalHolderFilledImage.material = defaultMat;

            long[] communityTimes = medalTimes[level.levelID];
            int medalEarned = GetMedalIndex(level.levelID);

            if (medalEarned < I(MedalEnum.Dev) && (!level.isSidequest || !levelStats.GetCompleted() || oldStyle.Value))
            {
                aceImage.sprite = Medals[I(MedalEnum.Ace)];
                goldImage.sprite = Medals[I(MedalEnum.Gold)];
                silverImage.sprite = Medals[I(MedalEnum.Silver)];
                return;
            }

            {
                // pastsight compatibility
                int pastSight = GetMedalIndex(level.levelID, levelStats.GetTimePastSight(true));
                if (!level.isSidequest)
                    __instance._levelMedal.sprite = Medals[pastSight];
                else
                    __instance._crystalHolderFilledImage.sprite = Crystals[pastSight];
            }

            stamps[1].sprite = Stamps[medalEarned];
            stamps[2].sprite = Stamps[medalEarned];

            if (oldStyle.Value)
            {
                aceImage.sprite = Medals[I(MedalEnum.Ace)];
                goldImage.sprite = Medals[I(MedalEnum.Gold)];
                silverImage.sprite = Medals[I(MedalEnum.Silver)];

                __instance.devTime.SetText(Helpers.FormatTime(communityTimes[medalEarned] / 1000, medalEarned != I(MedalEnum.Dev) || ShowMS.extended.Value, '.', true));
                __instance.devTime.color = AdjustedColor(Colors[medalEarned]);
                if (medalEarned < I(MedalEnum.Sapphire))
                {
                    TextMeshProUGUI nextTime = FindOrCreateNextTime(__instance);
                    nextTime.SetText(Helpers.FormatTime(communityTimes[medalEarned + 1] / 1000, true, '.', true));
                    nextTime.color = AdjustedColor(Colors[medalEarned + 1]);
                    nextTime.enabled = !hideOld.Value;
                }
                else
                    DestroyNextTime(__instance);

                if (level.isSidequest)
                {
                    __instance._medalInfoHolder.SetActive(true);
                    __instance.devStamp.SetActive(true);
                }
            }
            else
            {
                if (level.isSidequest)
                {
                    aceImage.sprite = Crystals[I(MedalEnum.Sapphire)];
                    goldImage.sprite = Crystals[I(MedalEnum.Amethyst)];
                    silverImage.sprite = Crystals[I(MedalEnum.Emerald)];
                    aceImage.preserveAspect = true;
                    goldImage.preserveAspect = true;
                    silverImage.preserveAspect = true;

                    __instance._medalInfoHolder.SetActive(true);
                    __instance._emptyFrameFiller.SetActive(false);
                }
                else
                {
                    aceImage.sprite = Medals[I(MedalEnum.Sapphire)];
                    goldImage.sprite = Medals[I(MedalEnum.Amethyst)];
                    silverImage.sprite = Medals[I(MedalEnum.Emerald)];
                }

                __instance._aceMedalBG.SetActive(medalEarned >= I(MedalEnum.Sapphire));
                __instance._goldMedalBG.SetActive(medalEarned >= I(MedalEnum.Amethyst));
                __instance._silverMedalBG.SetActive(medalEarned >= I(MedalEnum.Emerald));

                __instance._aceMedalTime.text = (string)styleTime.Invoke(__instance, [
                    Helpers.FormatTime(communityTimes[I(MedalEnum.Sapphire)] / 1000, true, '.', true),
                    medalEarned >= (int)MedalEnum.Sapphire]);
                __instance._goldMedalTime.text = (string)styleTime.Invoke(__instance, [
                    Helpers.FormatTime(communityTimes[I(MedalEnum.Amethyst)] / 1000, true, '.', true),
                    medalEarned >= (int)MedalEnum.Amethyst]);
                __instance._silverMedalTime.text = (string)styleTime.Invoke(__instance, [
                    Helpers.FormatTime(communityTimes[I(MedalEnum.Emerald)] / 1000, true, '.', true),
                    medalEarned >= (int)MedalEnum.Emerald]);

                if (medalEarned >= (int)MedalEnum.Plus)
                {
                    __instance.devStamp.SetActive(true);
                    __instance.devTime.text = Helpers.FormatTime(communityTimes[I(MedalEnum.Plus)] / 1000, true, '.', true);
                    __instance.devTime.color = AdjustedColor(Colors[medalEarned]);
                }
                else
                    __instance.devStamp.SetActive(false);

                if (hideOld.Value)
                {
                    String hiddenTime = "?:??.???";
                    if (medalEarned < I(MedalEnum.Sapphire))
                        __instance._aceMedalTime.text = hiddenTime;
                    if (medalEarned < I(MedalEnum.Amethyst))
                        __instance._goldMedalTime.text = hiddenTime;
                    if (medalEarned < I(MedalEnum.Emerald))
                        __instance._silverMedalTime.text = hiddenTime;
                }
            }
        }

        static TextMeshProUGUI FindOrCreateNextTime(LevelInfo levelInfo)
        {
            Transform nextTime = levelInfo.devTime.transform.parent.Find("NextTimeGoalText");
            if (nextTime == null)
            {
                nextTime =
                    UnityEngine.Object.Instantiate(levelInfo.devTime.gameObject, levelInfo.devTime.transform.parent).transform;
                nextTime.name = "NextTimeGoalText";
                //nextTimeGameObject.transform.position += new Vector3(1.18f, -0.1f);
                nextTime.localPosition += new Vector3(254.88f, -21.6f);
                var rectTransform = nextTime as RectTransform;
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 45);
                rectTransform.rotation = Quaternion.identity;
            }

            return nextTime.GetComponent<TextMeshProUGUI>();
        }

        static void DestroyNextTime(LevelInfo levelInfo)
        {
            Transform nextTime = levelInfo.devTime.transform.parent.Find("NextTimeGoalText");
            if (nextTime)
                UnityEngine.Object.Destroy(nextTime.gameObject);
        }

        static void PostSetLevelData(MenuButtonLevel __instance, LevelData ld)
        {
            if (!Ready || !medalTimes.ContainsKey(ld.levelID))
                return;

            AdjustMaterial(__instance._medal);
            if (ld.isSidequest)
                AdjustMaterial(__instance._imageLoreFilled);

            int medalEarned = GetMedalIndex(ld.levelID);

            if (medalEarned < I(MedalEnum.Dev))
                return;

            __instance._medal.sprite = Medals[medalEarned];
            __instance._imageLoreBacking.enabled = !ld.isSidequest;

            if (ld.isSidequest)
                __instance._imageLoreFilled.sprite = Crystals[medalEarned];
        }

        static readonly FieldInfo currentLevelData = Helpers.Field(typeof(Leaderboards), "currentLevelData");
        static void PostSetScore(LeaderboardScore __instance, ref ScoreData newData, bool globalNeonRankings)
        {
            if (!Ready || globalNeonRankings) return;

            Leaderboards leaderboard = __instance.GetComponentInParent<Leaderboards>();
            if (leaderboard == null) return; // somehow??
            LevelData levelData = (LevelData)currentLevelData.GetValue(leaderboard);
            if (levelData == null || !medalTimes.ContainsKey(levelData.levelID)) return;

            int medalEarned = GetMedalIndex(levelData.levelID, newData._scoreValueMilliseconds * 1000);
            AdjustMaterial(__instance._medal);

            if (hideLeaderboard.Value && medalEarned >= I(MedalEnum.Dev))
            {
                int userMedal = GetMedalIndex(levelData.levelID); // medal the user has on this level
                if (medalEarned > userMedal)
                    medalEarned = Math.Max(userMedal, I(MedalEnum.Ace)); // ensure the medal to be displayed is only as high as the user's, but only as low as ace
            }

            if (!levelData.isSidequest)
            {
                __instance._medal.sprite = Medals[Math.Min(medalEarned, I(MedalEnum.Sapphire))];
                __instance._medal.gameObject.SetActive(true);
            }
            else if (medalEarned > (int)MedalEnum.Dev)
            {
                __instance._medal.preserveAspect = true;
                __instance._medal.sprite = Crystals[Math.Min(medalEarned, I(MedalEnum.Sapphire))];
                __instance._medal.gameObject.SetActive(true);
            }
        }

        static long lastBest;
        static void PreOnWin() => lastBest = NeonLite.Game.GetGameData().GetLevelStats(NeonLite.Game.GetCurrentLevel().levelID)._timeBestMicroseconds;
        static void PostSetMedal(MenuScreenResults __instance, int medalEarned, int oldInsightLevel, int previousMedal, ref int ____medalEarned)
        {
            if (!Ready)
                return;

            NeonLite.Logger.DebugMsg($"{medalEarned} {oldInsightLevel} {previousMedal}");

            var level = NeonLite.Game.GetCurrentLevel();
            GameData gameData = NeonLite.Game.GetGameData();
            LevelStats levelStats = gameData.GetLevelStats(level.levelID);

            if (!medalTimes.ContainsKey(level.levelID))
                return;

            var modded = GetMedalIndex(level.levelID);
            __instance._levelCompleteMedalImage.sprite = Medals[modded];
            AdjustMaterial(__instance._levelCompleteMedalImage);

            if (!(medalEarned == 4 || (medalEarned == 0 && previousMedal == 4) || levelStats.IsNewBest()) || modded == GetMedalIndex(level.levelID, lastBest))
                return;
            if (oldInsightLevel == 4)
            {
                __instance._pityEarned_Localized.SetKey(""); // disable that, we're at max
                __instance._insightEarned_Localized.SetKey(""); // disable this too, we're at max
            }
            else if (modded >= I(MedalEnum.Emerald))
                __instance._insightEarned_Localized.SetKey("NeonLite/RESULTS_MEDAL_MODDED_INSIGHT");
            if (modded <= I(MedalEnum.Dev) || modded == I(MedalEnum.Plus)) // don't do anything else on dev and under
                return;

            string locKey = E(modded) switch
            {
                MedalEnum.Emerald => "NeonLite/RESULTS_MEDAL_EMERALD",
                MedalEnum.Amethyst => "NeonLite/RESULTS_MEDAL_AMETHYST",
                MedalEnum.Sapphire => "NeonLite/RESULTS_MEDAL_SAPPHIRE",
                _ => ""
            };

            __instance._levelCompleteMedalText_Localized.SetKey(locKey);

            ____medalEarned = 4;
        }

        static void PostSetVisible(MenuScreenResults __instance)
        {
            if (LevelRush.IsLevelRush())
                return;
            var split = __instance.levelComplete_Localized.textMeshProUGUI.text.Split();
            var level = NeonLite.Game.GetCurrentLevel();
            var name = LocalizationManager.GetTranslation(level.GetLevelDisplayName());
            if (string.IsNullOrEmpty(name))
                name = level.levelDisplayName;
            if (split.Length > 1)
                name = "  " + name;
            var list = split.ToList();
            var str = $"<nobr><alpha=#AA><size=40%><noparse>{name}</noparse></size></nobr><alpha=#FF><br>";
            if (split.Length < 2)
                list.Insert(list.Count - 1, str);
            else
                list[list.Count - 2] += str;
            __instance.levelComplete_Localized.textMeshProUGUI.text = string.Join("", list);
        }

#if !XBOX
        // file spec:
        // - byte: version
        // - byte[121]: level status (-1 for incomplete medal index otherwise)
        // - byte[bronze through amethyst]: count per medal
        // - bytebool: any further medals past saph
        // - byte: saph or saph+ count
        // if any further medals past saph: // it's arranged this way to make reading easier
        //   - byte: real saph count
        //   - for each further medal:
        //     - byte index (to match with level status table)
        //     - RRGGBB 3 byte color
        //     - byte count
        // a very innefficient filespec alignment wise but sizewise very compressed


        static string OnSteamLBWrite(BinaryWriter writer, SteamLBFiles.LBType type, bool _)
        {
            if (type != SteamLBFiles.LBType.Global || !uploadGlobal.Value)
                return null;
            writer.Write((byte)1); // VERSION

            Dictionary<int, byte> medalCounts = [];

            // print out all levels
            foreach (CampaignData campaign in NeonLite.Game.GetGameData().campaigns)
            {
                if (!Enum.IsDefined(typeof(CampaignData.CampaignType), campaign.campaignType))
                    continue;
                foreach (MissionData mission in campaign.missionData)
                {
                    if (!Enum.IsDefined(typeof(MissionData.MissionType), mission.missionType))
                        continue;
                    if (mission.missionID.Contains("GREEN")) // ignore that shit
                        continue;
                    foreach (LevelData level in mission.levels)
                    {
                        var m = GetMedalIndex(level.levelID);
                        if (!medalCounts.TryGetValue(m, out var c))
                            c = 0;
                        medalCounts[m] = ++c;

                        writer.Write((byte)m);
                    }
                }
            }

            // write all except pre-saph
            for (int i = -1; i < I(MedalEnum.Sapphire); ++i)
            {
                if (!medalCounts.TryGetValue(i, out var c))
                    c = 0;

                NeonLite.Logger.BetaMsg($"Medal UGC: Write {E(i)} {(int)c}");
                writer.Write(c);
            }

            // handle saph+ special
            bool anyOver = medalCounts.Any(kv => kv.Key > I(MedalEnum.Sapphire));
            writer.Write(anyOver);

            var saphpl = medalCounts.Where(kv => kv.Key >= I(MedalEnum.Sapphire)).Sum(kv => kv.Value);
            NeonLite.Logger.BetaMsg($"Medal UGC: Sapphire+ {saphpl}");

            writer.Write((byte)saphpl);

            if (anyOver)
            {
                // if we have any medals over saph, here's wherw we handle

                // write the ones that are ACTUALLY just saph
                if (!medalCounts.TryGetValue(I(MedalEnum.Sapphire), out var c))
                    c = 0;

                NeonLite.Logger.BetaMsg($"Medal UGC: Just Sapphire {(int)c}");

                writer.Write(c);

                // write bonus medals
                for (int i = I(MedalEnum.Sapphire) + 1; i <= medalCounts.Max(kv => kv.Key); ++i)
                {
                    if (!medalCounts.ContainsKey(i))
                        continue;
                    writer.Write((byte)i); // write the index for future use

                    // write they color
                    var col = Colors[i]; // this better be in there

                    writer.Write((byte)(col.r * 255));
                    writer.Write((byte)(col.g * 255));
                    writer.Write((byte)(col.b * 255));

                    writer.Write(medalCounts[i]);
                }
            }

            return LB_FILE;
        }

        [Flags]
        public enum LBDisplay
        {
            None = 0,
            Bronze = 1 << 0,
            Silver = 1 << 1,
            Gold = 1 << 2,
            Ace = 1 << 3,
            Dev = 1 << 4,
            Emerald = 1 << 5,
            Amethyst = 1 << 6,
            Sapphire = 1 << 7,
            Extended = 1 << 8,
        }

        static void OnSteamLBRead(BinaryReader reader, int length, LeaderboardScore score)
        {
            var ver = reader.ReadByte();

            List<(Color, int)> medals = [];

            var medalshow = showGlobalMedals.Value;

            switch (ver)
            {
                default:
                    NeonLite.Logger.Error($"Unknown community medal UGC version {ver}");
                    return;
                case 1:
                    {
                        const int LEVEL_COUNT = 121;
                        // that's right we're gonna cheat (do nothing w this, there for other mods)
                        reader.ReadBytes(LEVEL_COUNT);

                        // first, read any we haven't completed
                        reader.ReadByte(); // i haven't decided if im doing anything with this value

                        // read nonsaphs
                        for (int i = 0; i < I(MedalEnum.Sapphire); ++i)
                        {
                            var count = reader.ReadByte();
                            if (medalshow.HasFlag((LBDisplay)(1 << i)) && count != 0)
                                medals.Add((Colors[i], count));
                        }

                        var anyOver = reader.ReadBoolean();

                        if (anyOver && medalshow.HasFlag(LBDisplay.Extended))
                        {
                            // alright we got the CrAAAAYZ shit
                            // skip the combined saph byte
                            reader.ReadByte();

                            // read the solo saph byte
                            var count = reader.ReadByte();
                            if (medalshow.HasFlag(LBDisplay.Sapphire) && count != 0)
                                medals.Add((Colors[I(MedalEnum.Sapphire)], count));
                            NeonLite.Logger.BetaMsg($"Medal UGC: Read just saph {count}");

                            while (reader.BaseStream.Position < length)
                            {
                                var index = reader.ReadByte(); //index, we do nothing with
                                NeonLite.Logger.BetaMsg($"Medal UGC: Read index {index}");

                                var col = new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), 0xFF);
                                NeonLite.Logger.BetaMsg($"Color? {col}");

                                count = reader.ReadByte();
                                NeonLite.Logger.BetaMsg($"Count? {count}");

                                if (count != 0)
                                    medals.Add((col, count));
                            }
                        }
                        else
                        {
                            // read the combined byte and we're done
                            var count = reader.ReadByte();
                            if (medalshow.HasFlag(LBDisplay.Sapphire) && count != 0)
                                medals.Add((Colors[I(MedalEnum.Sapphire)], count));
                        }


                        break;
                    }
            }

            StringBuilder builder = new(); // we make the demon now
            const string COLORED = "<size=155%><voffset=-0.09em>\u2022</voffset></size><size=30%> </size>";
            const int MARGIN = 12;

            medals.Reverse();
            foreach ((var color, var count) in medals)
            {
                Color.RGBToHSV(color, out var h, out var s, out var v);
                h -= hueShift.Value;
                while (h < 0)
                    h += 1;

                s -= 0.1f;
                v += 0.15f;
                if (v > 1)
                    v = 1;

                var cstr = ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(h, s, v));

                builder.Append($"<color=#{cstr}>{COLORED}</color>{count}<size=80%> </size>");
            }

            var tmp = Utils.InstantiateUI(score._scoreValue.gameObject, "MedalCount", score.transform).GetComponent<TextMeshProUGUI>();

            tmp.rectTransform.pivot = new(1, 0.5f);
            tmp.enableAutoSizing = false;
            tmp.alignment = TextAlignmentOptions.MidlineRight;
            tmp.richText = true;
            tmp.text = builder.ToString();
            tmp.fontSize = 16;
            tmp.margin = new(0, 0, MARGIN, 0);

            var username = score._username.rectTransform;
            tmp.rectTransform.position = username.TransformPoint(new(username.rect.xMax, username.rect.center.y));

            var csf = tmp.GetOrAddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutRebuilder.ForceRebuildLayoutImmediate(tmp.rectTransform);

            var tomove = tmp.rectTransform.rect.width + MARGIN;
            username.ResizeWithPivot(new Vector2(-tomove, 0));
        }
#endif
    }
}
