using MelonLoader;
using NeonLite.GameObjects;
using NeonLite.Modules;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Module = NeonLite.Modules.Module;

namespace NeonLite
{
    public class NeonLite : MelonMod
    {
        public static readonly bool BETABUILD = true;
        public static NeonLite Instance;
        public static Game Game { get; private set; }
        public static GameObject ModObject { get; private set; }
        public static Module[] Modules { get; private set; }
        public static new HarmonyLib.Harmony Harmony { get; private set; }

        public static readonly BindingFlags s_publicStatic = BindingFlags.Public | BindingFlags.Static;
        public static readonly BindingFlags s_privateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        public static readonly BindingFlags s_privateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        #region EntryDefinitions

        public static MelonPreferences_Category Config_NeonLite { get; private set; }
        public static MelonPreferences_Entry<bool> s_Setting_SessionTimer;
        public static MelonPreferences_Entry<bool> s_Setting_LevelTimer;
        public static MelonPreferences_Entry<bool> s_Setting_GreenHP;
        public static MelonPreferences_Entry<bool> s_Setting_DisableAmbiance;
        public static MelonPreferences_Entry<bool> s_Setting_RestartsTotal;
        public static MelonPreferences_Entry<bool> s_Setting_RestartsSession;
        public static MelonPreferences_Entry<float> s_Setting_CoyoteAssistant;

        public static MelonPreferences_Category Config_NeonLiteVisuals { get; private set; }
        public static MelonPreferences_Entry<bool> s_Setting_PlayerPortrait;
        public static MelonPreferences_Entry<bool> s_Setting_BackstoryDisplay;
        //public static MelonPreferences_Entry<bool> bottombar_display;
        //public static MelonPreferences_Entry<bool> damageOverlay_display;
        //public static MelonPreferences_Entry<bool> boostOverlay_display;
        //public static MelonPreferences_Entry<bool> shockerOverlay_display;
        //public static MelonPreferences_Entry<bool> telefragOverlay_display;

        #endregion


        public override void OnApplicationStart()
        {
            Instance = this;

            Config_NeonLite = MelonPreferences.CreateCategory("NeonLite Settings");
            s_Setting_GreenHP = Config_NeonLite.CreateEntry("Enable Neon Green HP", true, description: "Displays the HP of Neon Green in Text Form.");
            s_Setting_DisableAmbiance = Config_NeonLite.CreateEntry("Ambience Remover", false, description: "Is the game too LOUD while muted? This will remove the ambience from the game.");
            s_Setting_SessionTimer = Config_NeonLite.CreateEntry("Display Session Timer", true, description: "Tracks your current play session time. (REQUIRES RESTART)");
            s_Setting_LevelTimer = Config_NeonLite.CreateEntry("Display Level Timer", true, description: "Tracks the time you've spent on the current level you're playing.");
            s_Setting_RestartsTotal = Config_NeonLite.CreateEntry("Show total Restarts", true, description: "Shows the total amout of restarts for a level.");
            s_Setting_RestartsSession = Config_NeonLite.CreateEntry("Show session restarts", true, description: "Shows the amout of restarts for a level during the current session.");
            s_Setting_CoyoteAssistant = Config_NeonLite.CreateEntry("Coyote Assistant", 0.05f, description: "The bigger the value, the earlier Neon Lite tells you to jump. -1 means disabled.\n(You must copy paste values > 0.1)");

            Config_NeonLiteVisuals = MelonPreferences.CreateCategory("NeonLite Visual Settings");
            s_Setting_PlayerPortrait = Config_NeonLiteVisuals.CreateEntry("Disable the Player portrait", false);
            s_Setting_BackstoryDisplay = Config_NeonLiteVisuals.CreateEntry("Disable backstory", false);

            //bottombar_display = neonLite_visuals.CreateEntry("Disable bottom bar", false, description: "Removes the bottom black bar that appears.");
            //damageOverlay_display = neonLite_visuals.CreateEntry("Disable low HP overlay", false, description: "Removes the overlay around your screen when you're at 1 hp.");
            //boostOverlay_display = neonLite_visuals.CreateEntry("Disable boost overlay", false, description: "Removes the overlay around your screen when you are getting a speed boost.");
            //shockerOverlay_display = neonLite_visuals.CreateEntry("Disable shocker overlay", false, description: "Removes the small white flash around your screen when using a shocker.");
            //telefragOverlay_display = neonLite_visuals.CreateEntry("Disable book of life overlay", false, description: "Removes the overlay around your screen when using the book of life.");
        }

        public override void OnApplicationLateStart()
        {
            Game = Singleton<Game>.Instance;
            Game.OnLevelLoadComplete += OnLevelLoadComplete;

            Harmony = new HarmonyLib.Harmony("NeonLite");

            IEnumerable<Type> types = Assembly.GetAssembly(typeof(Module)).GetTypes().Where(t => t.IsSubclassOf(typeof(Module)) && !t.IsAbstract && t.IsClass);
            Modules = new Module[types.Count()];
            for (int i = 0; i < types.Count(); i++)
                Modules[i] = (Module)Activator.CreateInstance(types.ElementAt(i));

            ModObject = new("Neon Lite");
            UnityEngine.Object.DontDestroyOnLoad(ModObject);

            Canvas canvas = ModObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (BETABUILD)
                ModObject.AddComponent<Beta>();
            ModObject.AddComponent<SessionTimer>();
            ModObject.AddComponent<CoyoteAssistant>();

            //TODO Add self repair if a file corrupts
            //TODO OBS like input display
            //TODO LevelRush helper
            //TODO Reimplement the HUD manager stuff
        }

        private void OnLevelLoadComplete()
        {
            if (SceneManager.GetActiveScene().name.Equals("Heaven_Environment"))
                return;

            GreenHP.Initialize();
            HUDManager.Initialize();
            LevelTimer.Initialize();
            RestartCounter.Initialize();
        }

        public override void OnUpdate()
        {
            try
            {
                DiscordActivity.DiscordInstance?.RunCallbacks();
            }
            catch (Discord.ResultException resultException)
            {
                Debug.LogWarning("Failed to run Discord callbacks: " + resultException.Message);
                DiscordActivity.ClearInstance();
                foreach(Module module in Modules)
                {
                    if (module is DiscordActivity activity)
                        activity.ClearCallbacks();
                }
            }
        }

        //Dev debug features
        public override void OnFixedUpdate()
        {
            return;
            if (Keyboard.current.f7Key.wasPressedThisFrame)
                RM.acceptInput = !RM.acceptInput;

            if (!Keyboard.current.hKey.wasPressedThisFrame) return;

            string FilePath = "C:\\medals\\medal.png";
            if (File.Exists(FilePath))
            {
                Texture2D Tex2D;
                byte[] FileData;
                FileData = File.ReadAllBytes(FilePath);
                Tex2D = new Texture2D(2, 2);
                Tex2D.LoadImage(FileData);
                Texture2D SpriteTexture = Tex2D;
                for (int i = 0; i < CommunityMedals.Medals.Length; i++)
                {
                    CommunityMedals.Medals[i] = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), 100f);
                }
            }
        }
    }
}
