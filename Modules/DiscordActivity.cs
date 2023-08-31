using Discord;
using HarmonyLib;
using I2.Loc;
using MelonLoader;
using NeonLite.GameObjects;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class DiscordActivity : Module
    {
        public static Discord.Discord DiscordInstance { get; private set; }
        private string _lastEnvironmentName = "";
        public DateTime _lastUpdate = DateTime.MinValue;

        private readonly string[] rushNames = new string[]
        {
            "White's",
            "Red's",
            "Violet's",
            "Yellow's",
            "Mikey's"
        };


        #region Config Definition

        public static MelonPreferences_Category Config_Discord { get; private set; } = MelonPreferences.CreateCategory("NeonLite Discord Integration");
        private static MelonPreferences_Entry<bool> _setting_DiscordActivity;
        private static MelonPreferences_Entry<string> _setting_HeadlineMenu;
        private static MelonPreferences_Entry<string> _setting_StateMenu;
        private static MelonPreferences_Entry<string> _setting_HeadlineRush;
        private static MelonPreferences_Entry<string> _setting_StateRush;
        private static MelonPreferences_Entry<string> _setting_Headline;
        private static MelonPreferences_Entry<string> _setting_State;
        private static MelonPreferences_Entry<bool> _setting_SessionTime;

        #endregion Config Definition

        public DiscordActivity()
        {
            foreach (string arg in System.Environment.GetCommandLineArgs())
            {
                if (arg == "-neonlite_disable_discord")
                {
                    Debug.Log("-neonlite_disable_discord found. Disabling Discord Module");
                    return;
                }
            }

            PlaceDiscordDLL();
            
            if (Process.GetProcessesByName("Discord").Length == 0)
            {
                Debug.LogError("Discord is not running");
                return;
            }

            DiscordInstance = new(1143203433067843676L, (ulong)CreateFlags.NoRequireDiscord);
            if (DiscordInstance == null) return;

            _setting_DiscordActivity = Config_Discord.CreateEntry("Discord Activity", true, description: "Shows your current ingame state in Discord");

            _setting_HeadlineMenu = Config_Discord.CreateEntry("Headline in menu", "In menu");
            _setting_StateMenu = Config_Discord.CreateEntry("Description in menu", "Sleeping");

            _setting_HeadlineRush = Config_Discord.CreateEntry("Headline in level rush", "%t", description: "%t = Rush type, %i = Current level, %r = Remaining levels, %l = Level name");
            _setting_StateRush = Config_Discord.CreateEntry("Description in level rush", "%l %i/%r", description: "%t = Rush type, %i = Current level, %r = Remaining levels, %l = Level name");

            _setting_Headline = Config_Discord.CreateEntry("Headline in level", "%l", description: "%l = Level name, %p = Personal best, %r = Session retry, %t = Total retrys");
            _setting_State = Config_Discord.CreateEntry("Description in level", "PB: %p", description: "%l = Level name, %p = Personal best, %r = Session retry, %t = Total retrys");

            _setting_SessionTime = Config_Discord.CreateEntry("Show session timer", true);

            SceneManager.activeSceneChanged += UpdateActivity;
            NeonLite.Game.OnLevelLoadComplete += UpdateActivity;
        }

        private void UpdateActivity(Scene oldScene, Scene newScene)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (!_setting_DiscordActivity.Value || !(sceneName == "Menu" || sceneName == "Heaven_Environment") || _lastEnvironmentName == "Menu") return;

            Activity activity = new()
            {
                Details = _setting_HeadlineMenu.Value,
                State = _setting_StateMenu.Value,
                Assets =
                {
                    LargeText = "Central Heaven",
                    LargeImage = "glassport",
                    SmallImage = "mikey",
                    SmallText = "Neon White"
                }
            };
            _lastEnvironmentName = "Menu";
            UpdateActivity(activity);
        }

        private void UpdateActivity()
        {
            Activity activity = new();
            if (!_setting_DiscordActivity.Value || SceneManager.GetActiveScene().name == "Heaven_Environment" || !CanUpdate()) return;

            string details, state;
            if (LevelRush.IsLevelRush())
            {
                details = GetDisplayString(Environment.LevelRush, _setting_HeadlineRush.Value);
                state = GetDisplayString(Environment.LevelRush, _setting_StateRush.Value);
            }
            else
            {
                details = GetDisplayString(Environment.Level, _setting_Headline.Value);
                state = GetDisplayString(Environment.Level, _setting_State.Value);
            }
            string location = NeonLite.Game.GetCurrentLevel().environmentLocationData.locationID;
            activity.Details = details;
            activity.State = state;
            activity.Assets.LargeText = location;
            activity.Assets.LargeImage = location.ToLower();
            activity.Assets.SmallImage = "mikey";
            activity.Assets.SmallText = "Neon White";
            activity.Instance = true;

            _lastEnvironmentName = NeonLite.Game.GetCurrentLevel().levelID;
            UpdateActivity(activity);
        }

        private void UpdateActivity(Activity activity)
        {
            if (_setting_SessionTime.Value)
            {
                DateTime gameStartupTime = DateTime.Now.Subtract(new TimeSpan(0, 0, (int)Time.realtimeSinceStartup));
                activity.Timestamps.Start = ((DateTimeOffset)gameStartupTime).ToUnixTimeSeconds();
            }
            _lastUpdate = DateTime.Now;
            DiscordInstance.GetActivityManager().UpdateActivity(activity, result =>
            {
                if (result != Result.Ok)
                    Debug.Log(result);
            });
        }

        private bool CanUpdate()
        {
            if (_lastEnvironmentName == NeonLite.Game.GetCurrentLevel().levelID)
            {
                TimeSpan result = DateTime.Now - _lastUpdate;
                return result.TotalMilliseconds >= 4000;
            }
            return true;
        }

        private string GetDisplayString(Environment environment, string text)
        {
            if (environment == Environment.Menu) return text;

            char[] replacementChars = environment switch
            {
                Environment.LevelRush => new char[] { 't', 'i', 'r', 'l' },
                Environment.Level => new char[] { 'l', 'p', 'r', 't' },
                _ => throw new NotImplementedException()
            };

            StringBuilder returnText = new();

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
                            if (environment == Environment.LevelRush)
                                returnText.Append(rushNames[LevelRush.GetIndexFromRushType(LevelRush.GetCurrentLevelRushType())] + (LevelRush.IsHellRush() ? " Hell Rush" : " Heaven Rush"));
                            else
                                returnText.Append(RestartCounter.LevelRestarts[NeonLite.Game.GetCurrentLevel().levelID]);
                            break;
                        case 'i':
                            if (environment == Environment.LevelRush)
                                returnText.Append(LevelRush.GetCurrentLevelRush().currentLevelIndex + 1);
                            break;
                        case 'r':
                            if (environment == Environment.LevelRush)
                                returnText.Append(LevelRush.GetCurrentLevelRush().randomizedIndex.Length);
                            else
                                returnText.Append(RestartCounter.CurrentRestarts);
                            break;
                        case 'l':
                            returnText.Append(LocalizationManager.GetTranslation("Interface/LEVELNAME_" + NeonLite.Game.GetCurrentLevel().levelID));
                            break;
                        case 'p':
                            returnText.Append(Game.GetTimerFormatted(GameDataManager.levelStats[NeonLite.Game.GetCurrentLevel().levelID].GetTimeBestMicroseconds()));
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

        private void PlaceDiscordDLL()
        {
            string file = Directory.GetCurrentDirectory() + "\\discord_game_sdk.dll";
            if (File.Exists(file)) return;
            File.WriteAllBytes(file, Properties.Resources.DiscordDLL);
        }

        private enum Environment
        {
            Menu,
            LevelRush,
            Level
        }
    }
}
