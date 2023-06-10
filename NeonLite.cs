using MelonLoader;
using NeonWhiteQoL.Modules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NeonWhiteQoL
{
    public class NeonLite : MelonMod
    {
        public static new HarmonyLib.Harmony Harmony { get; private set; }

        [Obsolete]
        public override void OnApplicationLateStart()
        {
            Game game = Singleton<Game>.Instance;
            GameObject modObject = new GameObject();
            UnityEngine.Object.DontDestroyOnLoad(modObject);

            game.OnLevelLoadComplete += OnLevelLoadComplete;

            Harmony = new HarmonyLib.Harmony("NeonLite");

            PBtracker.Initialize();
            GreenHP.Initialize();
            SkipIntro.Initialize();
            RemoveMission.Initialize();
            LeaderboardFix.Initialize();
            //CommunityMedals.Initialize();
            ShowcaseBypass.Initialize();
            IGTimer.Initialize();
            BegoneApocalypse.Initialize();
            BossfightGhost.Initialize();
            HUDManager.Initialize();
            LevelRushHelper.Initialize();
            RestartCounter.Initialize();
            DnfTime.Initialize();
            //LevelRushHelper.Initialize();
            //GameObject text = new GameObject("Text", typeof(Text));
            _ = new GameObject("SessionTimer", typeof(SessionTimer));
            modObject.AddComponent<CheaterBanlist>();
            modObject.AddComponent<CommunityMedals>();

            Debug.Log("Initialization complete.");
        }

        private void OnLevelLoadComplete()
        {
            if (SceneManager.GetActiveScene().name.Equals("Heaven_Environment"))
                return;

            GameObject.Find("HUD").AddComponent<HUDManager>();
            //GameObject.Find("Main Menu").AddComponent<HUDManager>();
            new GameObject("RestartCounter").AddComponent<RestartCounter>();
        }

        //Load a custom medal - for testing new medals ;)
        public override void OnUpdate()
        {
            if (Keyboard.current.f7Key.wasPressedThisFrame)
                RM.acceptInput = !RM.acceptInput;

            //if (!Keyboard.current.hKey.wasPressedThisFrame) return;
            //Texture2D Tex2D;
            //byte[] FileData;
            //string FilePath = "C:\\Users\\faust\\Desktop\\medal testing\\medal.png";

            //if (File.Exists(FilePath))
            //{
            //    FileData = File.ReadAllBytes(FilePath);
            //    Tex2D = new Texture2D(2, 2);
            //    Tex2D.LoadImage(FileData);
            //    Texture2D SpriteTexture = Tex2D;
            //    CommunityMedals.emeraldMedal = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), 100f);
            //}
        }

        #region EntryDefinitions

        public static MelonPreferences_Category neonLite_config;
        public static MelonPreferences_Entry<bool> CommunityMedals_enable;
        public static MelonPreferences_Entry<bool> PBtracker_display;
        public static MelonPreferences_Entry<bool> SessionTimer_display;
        public static MelonPreferences_Entry<bool> LevelTimer_display;
        public static MelonPreferences_Entry<bool> IGTimer_display;
        public static MelonPreferences_Entry<Color> IGTimer_color;
        public static MelonPreferences_Entry<bool> RemoveMission_display;
        public static MelonPreferences_Entry<bool> GreenHP_display;
        public static MelonPreferences_Entry<bool> Apocalypse_display;
        public static MelonPreferences_Entry<bool> InsightScreen_enable;
        public static MelonPreferences_Entry<bool> BossGhost_recorder;
        public static MelonPreferences_Entry<bool> ambience_disabled;
        public static MelonPreferences_Entry<bool> skipintro_enabler;

        public static MelonPreferences_Category neonLite_visuals;
        public static MelonPreferences_Entry<bool> playerUIportrait_display;
        public static MelonPreferences_Entry<bool> backstory_display;
        public static MelonPreferences_Entry<bool> bottombar_display;
        public static MelonPreferences_Entry<bool> damageOverlay_display;
        public static MelonPreferences_Entry<bool> boostOverlay_display;
        public static MelonPreferences_Entry<bool> shockerOverlay_display;
        public static MelonPreferences_Entry<bool> telefragOverlay_display;
        public static MelonPreferences_Entry<bool> uiScreenFader_display;
        //public static MelonPreferences_Entry<bool> whiteResult_display;

        #endregion

        [Obsolete]
        public override void OnApplicationStart()
        {
            neonLite_config = MelonPreferences.CreateCategory("NeonLite Settings");
            CommunityMedals_enable = neonLite_config.CreateEntry("Enable Community Medals", true, description: "Enables Custom Community Medals that change sprites in the game.");
            PBtracker_display = neonLite_config.CreateEntry("Enable PB Tracker", true, description: "Displays a time based on whether or not you got a new personal best.");
            SessionTimer_display = neonLite_config.CreateEntry("Display Session Timer", true, description: "Tracks your current play session time.");
            LevelTimer_display = neonLite_config.CreateEntry("Display Level Timer", true, description: "Tracks the time you've spent on the current level you're playing.");
            IGTimer_display = neonLite_config.CreateEntry("Display in-depth in-game timer", true, description: "Allows the modification of the timer and lets you display milliseconds.");
            IGTimer_color = neonLite_config.CreateEntry("In-game Timer Color", Color.white, description: "Customization settings for the in-game timer, does not apply to result screen time.");
            GreenHP_display = neonLite_config.CreateEntry("Enable Neon Green HP", true, description: "Displays the HP of Neon Green in Text Form.");
            RemoveMission_display = neonLite_config.CreateEntry("Remove Start Mission button in Job Archive", false, description: "Sick and tired of the big, bulky \"Start Mission\" button that appears? Now you can get rid of it, forever!");
            InsightScreen_enable = neonLite_config.CreateEntry("Insight Screen Remover", false, description: "No longer displays the \"Insight Crystal Dust (Empty)\" screen after finishing a sidequest level.");
            Apocalypse_display = neonLite_config.CreateEntry("Begone Apocalypse", true, description: "Get rid of the Apocalyptic view and replace it with the blue skies.");
            BossGhost_recorder = neonLite_config.CreateEntry("Boss Recorder", true, description: "Allows you to record and playback a ghost for the boss levels.");
            ambience_disabled = neonLite_config.CreateEntry("Ambience Remover", false, description: "Is the game too LOUD while muted ? This will remove the ambience from the game.");
            skipintro_enabler = neonLite_config.CreateEntry("Disable Intro", true, description: "Never hear the fabled \"We're called neons.\" speech when you start up your game. (REQUIRES RESTART)");

            neonLite_visuals = MelonPreferences.CreateCategory("NeonLite Visual Settings");
            playerUIportrait_display = neonLite_visuals.CreateEntry("Disable the Player portrait", false);
            backstory_display = neonLite_visuals.CreateEntry("Disable backstory", false);
            bottombar_display = neonLite_visuals.CreateEntry("Disable bottom bar", false, description: "Removes the bottom black bar that appears.");
            damageOverlay_display = neonLite_visuals.CreateEntry("Disable low HP overlay", false, description: "Removes the overlay around your screen when you're at 1 hp.");
            boostOverlay_display = neonLite_visuals.CreateEntry("Disable boost overlay", false, description: "Removes the overlay around your screen when you are getting a speed boost.");
            shockerOverlay_display = neonLite_visuals.CreateEntry("Disable shocker overlay", false, description: "Removes the small white flash around your screen when using a shocker.");
            telefragOverlay_display = neonLite_visuals.CreateEntry("Disable book of life overlay", false, description: "Removes the overlay around your screen when using the book of life.");
            uiScreenFader_display = neonLite_visuals.CreateEntry("Disable white screen fader", false, description: "Use in combination with shocker/book of life overlay!");
            //whiteResult_display = neonLite_visuals.CreateEntry("Disable white on result screen", false, description: "Gets rid of Neon White during the level completion screen.");
        }
    }
}
