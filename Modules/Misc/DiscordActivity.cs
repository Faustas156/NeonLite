using Discord;
using I2.Loc;
using MelonLoader;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NeonLite.Modules.Misc
{
    internal class DiscordActivity : MonoBehaviour, IModule
    {
        static DiscordActivity instance;
#pragma warning disable CS0414
        const bool priority = false;
        static bool active = false;

        static MelonPreferences_Entry<string> menuTitle;
        static MelonPreferences_Entry<string> menuDesc;
        static MelonPreferences_Entry<string> rushTitle;
        static MelonPreferences_Entry<string> rushDesc;
        static MelonPreferences_Entry<string> levelTitle;
        static MelonPreferences_Entry<string> levelDesc;
        static MelonPreferences_Entry<bool> seshTimer;

        static Discord.Discord DiscordInstance;
        static Activity activity = new();

        static LevelData lastLevel;

        static DateTime timeRecorded;
        static long startup;
        static bool wasRushing;

        static readonly string[] locKeys = [
            "Interface/LEVELRUSH_14_WHITE_TITLE_HEAVEN",
            "Interface/LEVELRUSH_10_RED_TITLE_HEAVEN",
            "Interface/LEVELRUSH_22_VIOLET_TITLE_HEAVEN",
            "Interface/LEVELRUSH_18_YELLOW_TITLE_HEAVEN",
            "Interface/LEVELRUSH_26_MIKEY_TITLE_HEAVEN",

            "Interface/LEVELRUSH_13_WHITE_TITLE_HELL",
            "Interface/LEVELRUSH_09_RED_TITLE_HELL",
            "Interface/LEVELRUSH_21_VIOLET_TITLE_HELL",
            "Interface/LEVELRUSH_17_YELLOW_TITLE_HELL",
            "Interface/LEVELRUSH_25_MIKEY_TITLE_HELL"
        ];

        static readonly string[] rushAsset = [
            "whiteg",
            "redg",
            "violetg",
            "yellowg",
            "mikeyg",

            "whiteb",
            "redb",
            "violetb",
            "yellowb",
            "mikeyb",
        ];


        static int GetIndex(LevelRush.LevelRushType rushType = LevelRush.LevelRushType.None)
        {
            if (rushType == LevelRush.LevelRushType.None)
                rushType = LevelRush.GetCurrentLevelRushType();
            return LevelRush.GetIndexFromRushType(rushType) + (LevelRush.IsHellRush() ? ((int)LevelRush.LevelRushType.Count - 1): 0);
        }

        static void Setup()
        {
            if (Environment.GetCommandLineArgs().Contains("-neonlite_disable_discord"))
            {
                NeonLite.Logger.Msg("-neonlite_disable_discord found. Disabling Discord module.");
                return;
            }

            startup = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

            var setting = Settings.Add(Settings.h, "Discord", "enabled", "Discord Rich Presence", "Enables the use of Discord Rich Presence.", false);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;

            menuTitle = Settings.Add(Settings.h, "Discord", "menuTitle", "Headline in menu", null, "In menu");
            menuDesc = Settings.Add(Settings.h, "Discord", "menuDesc", "Description in menu", null, "Sleeping");
            rushTitle = Settings.Add(Settings.h, "Discord", "rushTitle", "Headline in level rush", "%t = Rush type, %i = Current level, %r = Remaining levels, %l = Level name", "%t");
            rushDesc = Settings.Add(Settings.h, "Discord", "rushDesc", "Description in level rush", "Same as above.", "%l (%i/%r)");
            levelTitle = Settings.Add(Settings.h, "Discord", "levelTitle", "Headline in level", "%l = Level name, %p = Personal best, %r = Session attempts, %t = Total attempts", "%l");
            levelDesc = Settings.Add(Settings.h, "Discord", "levelDesc", "Description in level", "Same as above.", "PB: %p");
            seshTimer = Settings.Add(Settings.h, "Discord", "seshTimer", "Show session timer", "Show a total session timer instead of a level/rush session timer.", true);

            string file = Directory.GetCurrentDirectory() + "/discord_game_sdk.dll";
            if (File.Exists(file))
                return;
            File.WriteAllBytes(file, Resources.r.DiscordDLL);
        }

        static void Activate(bool activate)
        {
            if (activate)
            {
                try
                {
                    DiscordInstance = new(1268765228167073802L, (ulong)CreateFlags.NoRequireDiscord);
                    NeonLite.holder.AddComponent<DiscordActivity>();
                    NeonLite.Game.winAction += LevelWin;
                }
                catch (ResultException e)
                {
                    NeonLite.Logger.Error("Failed to initialize Discord:");
                    NeonLite.Logger.Error(e.Message);
                    active = false;
                    return;
                }
            }
            else
            {
                NeonLite.Game.winAction -= LevelWin;
                Destroy(instance);
                DiscordInstance.GetActivityManager().ClearActivity(_ => { });
                DiscordInstance.Dispose();
            }

            active = activate;
        }

        static void LevelWin()
        {
            if (GameDataManager.levelStats[lastLevel.levelID].IsNewBest())
                OnLevelLoad(lastLevel); // so it shows a pb if configued :3
        }

        static readonly StringBuilder returnText = new();
        static string rushTCache = null;
        static string levelLCache = null;
        static string GetText(LevelData level, bool levelRush, string text)
        {
            returnText.Clear();
            char[] replacementChars = levelRush switch
            {
                true => ['t', 'i', 'r', 'l'],
                false => ['l', 'p', 'r', 't'],
            };

            var ri = GameInfo.RestartManager.restarts[level.levelID];

            bool percent = false;
            foreach (char symbol in text.ToCharArray())
            {
                if (symbol == '%')
                {
                    percent = true;
                    continue;
                }
                else if (percent && replacementChars.Contains(symbol))
                {
                    percent = false;
                    switch (symbol)
                    {
                        case 't':
                            if (levelRush)
                            {
                                if (rushTCache == null || wasRushing == false)
                                    rushTCache = LocalizationManager.GetTranslation(locKeys[GetIndex()]);
                                returnText.Append(rushTCache);
                            }
                            else
                                returnText.Append(ri.total + ri.queued);
                            break;
                        case 'i': // rush only
                            returnText.Append(LevelRush.GetCurrentLevelRushLevelIndex() + 1);
                            break;
                        case 'r':
                            if (levelRush)
                                returnText.Append(LevelRush.GetCurrentLevelRush().randomizedIndex.Length);
                            else
                                returnText.Append(ri.session);
                            break;
                        case 'l':
                            if (levelLCache == null || level != lastLevel)
                            {
                                levelLCache = LocalizationManager.GetTranslation(level.GetLevelDisplayName());
                                if (string.IsNullOrEmpty(levelLCache))
                                    levelLCache = level.levelDisplayName;
                            }
                            returnText.Append(levelLCache);
                            break;
                        case 'p':
                            returnText.Append(Helpers.FormatTime(GameDataManager.levelStats[level.levelID].GetTimeBestMicroseconds() / 1000, null, '.', true));
                            break;
                    }
                }
                else
                {
                    if (percent)
                        returnText.Append('%');
                    returnText.Append(symbol);
                }
                percent = false;
            }
            return returnText.ToString();
        }

        void Awake() => instance = this;

        static LevelData level;
        static bool dirty = false;
        void LateUpdate()
        {
            if (!dirty)
                return;

            // we do setting in here to make sure restart is synced

            bool levelRush = LevelRush.IsLevelRush();
            DateTime lastTimeRec = timeRecorded;
            if (!level || level.type == LevelData.LevelType.Hub)
            {
                activity.Assets.LargeImage = "locationother";
                activity.Details = menuTitle.Value;
                activity.State = menuDesc.Value;
                levelRush = false; // just incase
            }
            else
            {
                activity.Assets.LargeImage = level.GetPreviewImage()?.name.ToLower() ?? "locationother";
                activity.Details = GetText(level, levelRush, (levelRush ? rushTitle : levelTitle).Value);
                activity.State = GetText(level, levelRush, (levelRush ? rushDesc : levelDesc).Value);
            }

            if (levelRush)
            {
                if (!wasRushing)
                {
                    timeRecorded = DateTime.Now;
                    activity.Assets.SmallImage = rushAsset[GetIndex()];
                }
            }
            else
            {
                activity.Assets.SmallImage = "";
                if (level != lastLevel)
                    timeRecorded = DateTime.Now;
            }

            if (seshTimer.Value)
                activity.Timestamps.Start = startup;
            else if (timeRecorded != lastTimeRec)
            {
                var dt = DateTime.Now;
                activity.Timestamps.Start = (long)(((DateTimeOffset)dt).ToUnixTimeSeconds() - dt.Subtract(timeRecorded).TotalSeconds);
            }

            if (NeonLite.DEBUG)
            {
                NeonLite.Logger.Msg("DISCORD STATUS");
                NeonLite.Logger.Msg(activity.Details);
                NeonLite.Logger.Msg(activity.State);
                NeonLite.Logger.Msg(activity.Assets.SmallImage);
                NeonLite.Logger.Msg(activity.Assets.LargeImage);
            }


            DiscordInstance.GetActivityManager().UpdateActivity(activity, result =>
            {
                if (result != Result.Ok)
                    NeonLite.Logger.Error($"Discord UpdateActivity returned {result}");
                else if (NeonLite.DEBUG)
                    NeonLite.Logger.Msg("Discord returned good");
            });

            wasRushing = levelRush;
            lastLevel = level;
            dirty = false;
        }

        static void OnLevelLoad(LevelData l)
        {
            level = l;
            dirty = true;
        }

        void Update()
        {
            try
            {
                DiscordInstance.RunCallbacks();
            }
            catch (ResultException re)
            {
                NeonLite.Logger.Error("Failed to run Discord callbacks: " + re.Message);
                Activate(false);
            }
        }
    }
}
