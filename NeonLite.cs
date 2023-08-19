using HarmonyLib.Tools;
using MelonLoader;
using NeonLite.GameObjects;
using NeonLite.Modules;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Module = NeonLite.Modules.Module;

namespace NeonLite
{
    public class NeonLite : MelonMod
    {
        public static Game Game { get; private set; }

        public static Module[] Modules { get; private set; }
        private static readonly DataContractJsonSerializerSettings _jsonSettings = new() { UseSimpleDictionaryFormat = true };

        public static readonly BindingFlags s_privateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        public static readonly BindingFlags s_privateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        public static readonly BindingFlags s_publicStatic = BindingFlags.Public | BindingFlags.Static;

        public static new HarmonyLib.Harmony Harmony { get; private set; }

        #region EntryDefinitions

        public static MelonPreferences_Category neonLite_config;
        public static MelonPreferences_Entry<bool> CommunityMedals_enable;
        public static MelonPreferences_Entry<bool> SessionTimer_display;
        public static MelonPreferences_Entry<bool> LevelTimer_display;
        public static MelonPreferences_Entry<bool> GreenHP_display;
        public static MelonPreferences_Entry<bool> ambience_disabled;
        public static MelonPreferences_Entry<bool> restarts_total;
        public static MelonPreferences_Entry<bool> restarts_session;

        public static MelonPreferences_Category neonLite_visuals;
        public static MelonPreferences_Entry<bool> playerUIportrait_display;
        public static MelonPreferences_Entry<bool> backstory_display;
        //public static MelonPreferences_Entry<bool> bottombar_display;
        //public static MelonPreferences_Entry<bool> damageOverlay_display;
        //public static MelonPreferences_Entry<bool> boostOverlay_display;
        //public static MelonPreferences_Entry<bool> shockerOverlay_display;
        //public static MelonPreferences_Entry<bool> telefragOverlay_display;

        #endregion

        public override void OnApplicationStart()
        {
            neonLite_config = MelonPreferences.CreateCategory("NeonLite Settings");
            GreenHP_display = neonLite_config.CreateEntry("Enable Neon Green HP", true, description: "Displays the HP of Neon Green in Text Form.");
            ambience_disabled = neonLite_config.CreateEntry("Ambience Remover", false, description: "Is the game too LOUD while muted ? This will remove the ambience from the game.");
            SessionTimer_display = neonLite_config.CreateEntry("Display Session Timer", true, description: "Tracks your current play session time. (REQUIRES RESTART)");
            LevelTimer_display = neonLite_config.CreateEntry("Display Level Timer", true, description: "Tracks the time you've spent on the current level you're playing.");
            restarts_total = neonLite_config.CreateEntry("Show total Restarts", true, description: "Shows the total amout of restarts for a level.");
            restarts_session = neonLite_config.CreateEntry("Show session restarts", true, description: "Shows the amout of restarts for a level during the current session.");

            CommunityMedals_enable = neonLite_config.CreateEntry("Enable Community Medals", true, description: "Enables Custom Community Medals that change sprites in the game.");


            neonLite_visuals = MelonPreferences.CreateCategory("NeonLite Visual Settings");

            playerUIportrait_display = neonLite_visuals.CreateEntry("Disable the Player portrait", false);
            backstory_display = neonLite_visuals.CreateEntry("Disable backstory", false);


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
            HarmonyFileLog.Enabled = true;

            IEnumerable<Type> types = Assembly.GetAssembly(typeof(Module)).GetTypes().Where(t => t.IsSubclassOf(typeof(Module)) && !t.IsAbstract && t.IsClass);
            Modules = new Module[types.Count()];
            for (int i = 0; i < types.Count(); i++)
                Modules[i] = (Module)Activator.CreateInstance(types.ElementAt(i));

            GameObject modObject = new("Neon Lite");
            UnityEngine.Object.DontDestroyOnLoad(modObject);

            modObject.AddComponent<SessionTimer>();
            return;

            //LevelRushHelper.Initialize(); //TODO
            CommunityMedals.Initialize();
            modObject.AddComponent<CommunityMedals>();
        }

        private void OnLevelLoadComplete()
        {
            if (SceneManager.GetActiveScene().name.Equals("Heaven_Environment"))
                return;

            GreenHP.Initialize();
            HUDManager.Initialize();
            LevelTimer.Initialize();
            LevelTimer.Initialize();
            RestartCounter.Initialize();
        }

        public static void SaveToFile<T>(string path, string filename, object data)
        {
            Task.Run(() =>
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                Stream stream = File.Open(path + "/" + filename, FileMode.Create);
                XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  ");
                DataContractJsonSerializer serializer = new(typeof(T), _jsonSettings);
                serializer.WriteObject(writer, data);
                writer.Flush();
                stream.Close();
            });
        }

        public static T ReadFile<T>(string path, string filename)
        {
            string data = new StreamReader(path + "/" + filename, Encoding.UTF8).ReadToEnd();
            DataContractJsonSerializer deserializer = new(typeof(T), _jsonSettings);
            return (T)deserializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(data)));
        }

        public static T ReadFile<T>(byte[] ressource)
        {
            DataContractJsonSerializer deserializer = new(typeof(T), _jsonSettings);
            return (T)deserializer.ReadObject(new MemoryStream(ressource));
        }

        internal static void DownloadRessource<T>(string url, Action<DownloadResult> callback)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.SendWebRequest().completed +=
                result =>
                {
                    if (webRequest.result != UnityWebRequest.Result.Success)
                        callback(new DownloadResult() { success = false });
                    else
                    {
                        T data;
                        try {
                            data = ReadFile<T>(webRequest.downloadHandler.data);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                            return;
                        }
                        callback(new DownloadResult() { success = true, data = data });
                    }
                };
        }

        internal struct DownloadResult
        {
            public bool success;
            public object data;
        }

        //Dev debug features
        public override void OnFixedUpdate()
        {
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
                CommunityMedals.emeraldMedal = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), 100f);
            }
        }
    }
}
