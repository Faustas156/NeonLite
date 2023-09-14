using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class CommunityMedals : Module
    {
        private const string FILENAME = "communitymedals.json";
        private const string URL = "https://raw.githubusercontent.com/MOPSKATER/NeonLite/main/Resources/communitymedals.json";
        private static MelonPreferences_Entry<bool> _setting_CommunityMedals_enable;

        private static Dictionary<string, long[]> _communityMedalTimes;
        public static Sprite DefaultStamp { get; private set; }
        public static Sprite[] Medals { get; private set; }
        public static Sprite[] Crystals { get; private set; }
        public static Sprite[] Stamps { get; private set; }
        public static Color[] TextColors { get; private set; }

        private static readonly FieldInfo leaderboardsRef = typeof(LeaderboardIntegrationSteam).GetField("leaderboardsRef", NeonLite.s_privateStatic);
        private static readonly FieldInfo currentLevelData = typeof(Leaderboards).GetField("currentLevelData", NeonLite.s_privateInstance);

        public CommunityMedals()
        {
            _setting_CommunityMedals_enable = NeonLite.Config_NeonLite.CreateEntry("Enable Community Medals", true, description: "Enables Custom Community Medals that change sprites in the game.");

            RessourcesUtils.DownloadRessource<Dictionary<string, long[]>>(URL, result =>
            {
                string path;
                if (result.success)
                {
                    _communityMedalTimes = (Dictionary<string, long[]>)result.data;

                    if (_communityMedalTimes == null || _communityMedalTimes.Count == 0)
                    {
                        Debug.LogWarning("List of banned IDs empty");
                        return;
                    }

                    //Download succeeded => create local file
                    if (RessourcesUtils.GetDirectoryPath(out path))
                        RessourcesUtils.SaveToFile<Dictionary<string, long[]>>(path, FILENAME, _communityMedalTimes);
                    return;
                }

                else if (RessourcesUtils.GetDirectoryPath(out path) && File.Exists(path + FILENAME))
                    //Download failed => find local file
                    _communityMedalTimes = RessourcesUtils.ReadFile<Dictionary<string, long[]>>(path, FILENAME);
                else
                    //Local file not found => read file from resources
                    _communityMedalTimes = RessourcesUtils.ReadFile<Dictionary<string, long[]>>(Properties.Resources.communitymedals);
            });

            DefaultStamp = LoadSprite(Properties.Resources.MikeyDefault);
            Medals = new Sprite[]
            {
                LoadSprite(Properties.Resources.MedalEmerald),
                LoadSprite(Properties.Resources.MedalAmethyst),
                LoadSprite(Properties.Resources.MedalSapphire)
            };

            Crystals = new Sprite[]
            {
                LoadSprite(Properties.Resources.CrystalEmerald),
                LoadSprite(Properties.Resources.CrystalAmethyst),
                LoadSprite(Properties.Resources.CrystalSapphire)
            };

            Stamps = new Sprite[]
            {
                LoadSprite(Properties.Resources.MikeyEmerald),
                LoadSprite(Properties.Resources.MikeyAmethyst),
                LoadSprite(Properties.Resources.MikeySapphire)
            };

            TextColors = new Color[]
            {
                new Color(0.388f, 0.8f, 0.388f),
                new Color(0.674f, 0.313f, 0.913f),
                new Color(0.043f, 0.317f, 0.901f)
            };
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelInfo), "SetLevel")]
        private static void PostSetLevel(LevelInfo __instance, ref LevelData level)
        {
            if (SceneManager.GetActiveScene().name == "CustomLevel") return;

            if (!_setting_CommunityMedals_enable.Value | _communityMedalTimes == null)
            {
                //Reset stamp time to red if feature is broken or disables
                __instance.devTime.color = new Color(0.420f, 0.015f, 0.043f);
                return;
            }

            GameData gameData = Singleton<Game>.Instance.GetGameData();
            LevelStats levelStats = gameData.GetLevelStats(level.levelID);

            if (!levelStats.GetCompleted()) return;

            long[] communityTimes = _communityMedalTimes[level.levelID];
            if (communityTimes == null) return;

            Image[] stamps;
            for (int i = Medals.Length - 1; i >= 0; i--)
            {
                long timeToCheck = communityTimes[i];

                if (levelStats._timeBestMicroseconds <= timeToCheck)
                {
                    __instance._levelMedal.sprite = Medals[i];
                    int nexIndex = Math.Min(2, i + 1);
                    __instance.devTime.SetText(Game.GetTimerFormattedMillisecond(communityTimes[nexIndex]));
                    __instance.devTime.color = TextColors[nexIndex];

                    stamps = __instance.devStamp.GetComponentsInChildren<Image>();
                    if (stamps.Length < 3) return;

                    stamps[1].sprite = Stamps[i];
                    stamps[2].sprite = Stamps[i];

                    if (level.isSidequest)
                    {
                        __instance._medalInfoHolder.SetActive(true);
                        __instance.devStamp.SetActive(true);
                        __instance._crystalHolderFilledImage.sprite = Crystals[i];
                    }
                    return;
                }
            }

            __instance.devTime.SetText(Game.GetTimerFormattedMillisecond(communityTimes[0]));
            __instance.devTime.color = TextColors[0];
            stamps = __instance.devStamp.GetComponentsInChildren<Image>();
            if (stamps.Length < 3) return;

            stamps[1].sprite = DefaultStamp;
            stamps[2].sprite = DefaultStamp;

            if (level.isSidequest && levelStats._timeBestMicroseconds < Utils.ConvertSeconds_FloatToMicroseconds(level.GetTimeDev()))
            {
                __instance._medalInfoHolder.SetActive(true);
                __instance.devStamp.SetActive(true);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuButtonLevel), "SetLevelData")]
        private static void PostSetLevelData(MenuButtonLevel __instance, ref LevelData ld)
        {
            if (!_setting_CommunityMedals_enable.Value || _communityMedalTimes == null || SceneManager.GetActiveScene().name == "CustomLevel") return;

            GameData GameDataRef = Singleton<Game>.Instance.GetGameData();

            LevelStats levelStats = GameDataRef.GetLevelStats(ld.levelID);
            long[] communityTimes = _communityMedalTimes[ld.levelID];

            for (int i = Medals.Length - 1; i >= 0; i--)
            {

                long timeToCheck = communityTimes[i];

                if (levelStats._timeBestMicroseconds <= timeToCheck)
                {
                    __instance._medal.sprite = Medals[i];
                    if (ld.isSidequest)
                        __instance._imageLoreFilled.sprite = Crystals[i];
                    return;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LeaderboardScore), "SetScore")]
        private static void PostSetScore(ref LeaderboardScore __instance, ref ScoreData newData, ref bool globalNeonRankings)
        {
            if (!_setting_CommunityMedals_enable.Value || globalNeonRankings) return;

            Leaderboards leaderboard = (Leaderboards)leaderboardsRef.GetValue(null);
            LevelData levelData = (LevelData)currentLevelData.GetValue(leaderboard);
            long[] communityTimes = _communityMedalTimes[levelData.levelID];

            for (int i = Medals.Length - 1; i >= 0; i--)
            {
                long timeToCheck = communityTimes[i];
                if (newData._scoreValueMilliseconds * 1000 <= timeToCheck)
                {
                    __instance._medal.sprite = Medals[i];
                    __instance._medal.gameObject.SetActive(true);
                    return;
                }
            }

            if (newData._scoreValueMilliseconds <= (long)(levelData.timeDev * 1000f))
                __instance._medal.sprite = NeonLite.Game.GetGameData().medalSprite_Dev;
            return;
        }

        private static Sprite LoadSprite(byte[] image)
        {
            Texture2D SpriteTexture = new(2, 2);
            SpriteTexture.LoadImage(image);

            return Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), 100f);
        }

        public static long[] GetMedalTimes(string level) =>
            (long[])_communityMedalTimes[level].Clone();
    }
}
