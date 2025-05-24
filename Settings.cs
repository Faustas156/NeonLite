using I2.Loc;
using MelonLoader;
using MelonLoader.TinyJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NeonLite
{
    public static class Settings
    {
        internal static readonly int VERSION = 3001003; // 1223344
        internal static MelonPreferences_Category mainCategory;

        static readonly Dictionary<MelonPreferences_Entry, (string, string)> entryLoc = [];
        static readonly Dictionary<MelonPreferences_Category, string> catLoc = [];

        static readonly Dictionary<string, Dictionary<string, MelonPreferences_Category>> catHolders = [];

        internal const string h = "NeonLite";

#if DEBUG
        static ProxyObject locJSON = [];
        static string locPath;
#endif

        static int readVersion;
        internal static void Setup()
        {
            mainCategory = MelonPreferences.CreateCategory("NeonLite");
            var ver = mainCategory.CreateEntry("VERSION", VERSION, "VERSION (DO NOT CHANGE)", null, true);
            readVersion = ver.Value; 
            ver.Value = VERSION;
            if (NeonLite.DEBUG)
                mainCategory.CreateEntry("DEBUG", true, is_hidden: true);
            AddHolder(h);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static void AddHolder(string name) => catHolders.Add(name, []);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static MelonPreferences_Category CreateCategory(string holder, string name)
        {
            var c = MelonPreferences.CreateCategory($"{holder}/{name}");
            catHolders[holder].Add(name ?? "", c);
            if (string.IsNullOrEmpty(name))
                c.DisplayName = holder;
            return c;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MelonPreferences_Category FindCategory(string holder, string name) => (catHolders.TryGetValue(holder, out var val) ? (val.TryGetValue(name, out var c) ? c : null) : null) ?? CreateCategory(holder, name);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MelonPreferences_Entry<T> Find<T>(string holder, string category, string id, bool hide = false)
        {
            var c = FindCategory(holder, category).GetEntry<T>(id) ?? Add<T>(holder, category, id, null, null, default);
            if (hide)
                c.IsHidden = hide;
            return c;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MelonPreferences_Entry<T> Add<T>(string holder, string category, string id, string display, string description, T defaultVal, MelonLoader.Preferences.ValueValidator validator = null) =>
            Add(holder, category, id, display, description, defaultVal, false, validator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MelonPreferences_Entry<T> Add<T>(string holder, string category, string id, string display, string description, T defaultVal, bool hide, MelonLoader.Preferences.ValueValidator validator = null)
        {
            var cat = FindCategory(holder, category);
            var ret = cat.CreateEntry(id, defaultVal, display, description: description, validator: validator, is_hidden: hide);
            string catloc = new(category.Where(char.IsLetter).Select(char.ToUpper).ToArray());
            if (!catLoc.ContainsKey(cat))
                catLoc.Add(cat, $"SETTING_{catloc}");
            entryLoc.Add(ret, ($"SETTING_{catloc}_{id.ToUpper()}_T", description != null ? $"SETTING_{catloc}_{id.ToUpper()}_D" : null));
#if DEBUG
            if (!locJSON.Keys.Contains(holder))
                locJSON[holder] = new ProxyObject();
            ProxyObject jshold = locJSON[holder] as ProxyObject;
            jshold[$"SETTING_{catloc}"] = new ProxyString(category);
            jshold[$"SETTING_{catloc}_{id.ToUpper()}_T"] = new ProxyString(display);
            if (description != null)
                jshold[$"SETTING_{catloc}_{id.ToUpper()}_D"] = new ProxyString(description.Replace("\n", "<br>"));
#endif
            return ret;
        }
        public static bool SetupForModule<T>(this MelonPreferences_Entry<T> setting, Action<bool> activate, Func<T, T, bool> pred)
        {
            setting.OnEntryValueChanged.Subscribe((before, after) =>
            {
                var a = pred(before, after);
                NeonLite.Logger.DebugMsg($"{activate.Method.DeclaringType} {before} {after} {a}");
                activate(a);
                if (a && Patching.firstPass)
                    Patching.RunPatches(false);
            });
            return pred(setting.DefaultValue, setting.Value);
        }

        internal static void Localize()
        {
            foreach (var k in catLoc.Keys.ToList())
            {
                var holder = k.DisplayName.Split('/')[0];
                var trans = catLoc[k]; // yay!!
                NeonLite.Logger.DebugMsg($"try {holder}/{trans}");
                if (LocalizationManager.TryGetTranslation($"{holder}/{trans}", out var t))
                    k.DisplayName = $"{holder}/{t}";
                else
                    NeonLite.Logger.DebugMsg($"fail");
            }

            foreach (var k in entryLoc.Keys.ToList())
            {
                var pair = entryLoc[k];

                var holder = k.Category.DisplayName.Split('/')[0];

                NeonLite.Logger.DebugMsg($"try {holder}/{pair.Item1}");
                if (LocalizationManager.TryGetTranslation($"{holder}/{pair.Item1}", out var t))
                    k.DisplayName = t;
                else
                    NeonLite.Logger.DebugMsg($"fail");
                if (pair.Item2 != null && LocalizationManager.TryGetTranslation($"{holder}/{pair.Item2}", out var d))
                    k.Description = d.Replace("<br>", "\n");
            }
        }

        static MelonPreferences_Entry<T> CreateOrFind<T>(this MelonPreferences_Category category, string name, T defaultVal, bool hide = false)
        {
            try
            {
                return category.CreateEntry(name, defaultVal, dont_save_default: true, is_hidden: hide);
            }
            catch
            {
                var c = category.GetEntry<T>(name);
                c.IsHidden = hide;
                return c;
            }
        }

        internal static void Migrate()
        {
            NeonLite.Logger.DebugMsg("checking for settings migration");

#if DEBUG
            locPath = Path.Combine(Helpers.GetSaveDirectory(), "NeonLite", "localization.json");
            Helpers.CreateDirectories(locPath);
            ProxyObject loaded = [];
            if (File.Exists(locPath))
                loaded = JSON.Load(File.ReadAllText(locPath)) as ProxyObject;

            foreach (var kv in locJSON)
            {
                if (!loaded.Keys.Contains(kv.Key))
                    loaded[kv.Key] = new ProxyObject();
                foreach (var kv2 in kv.Value as ProxyObject)
                    loaded[kv.Key][kv2.Key] = kv2.Value;
            }

            File.WriteAllText(locPath, JSON.Dump(loaded, EncodeOptions.PrettyPrint));
#endif

            var category = MelonPreferences.CreateCategory("NeonLite Settings");
            var migrated = category.CreateEntry("MIGRATED", false, is_hidden: true).Value;
            if (!migrated)
            {
                category.GetEntry<bool>("MIGRATED").Value = true;

                // use community medals as a way of checking to be absolutely sure we have to migrate
                // this fails when comm medals is false already but most people should have it enabled
                var entry = category.CreateOrFind("Enable Community Medals", false, true);
                if (entry.Value)
                {
                    NeonLite.Logger.Msg("Pre 3.0.0 NeonLite settings found! Attempting migration...");

                    Find<bool>(h, "UI/In-game", "deltatime").Value = category.CreateOrFind("Deltatime", true, true).Value;
                    Find<bool>(h, "UI/In-game", "dnf").Value = category.CreateOrFind("DNF", true, true).Value;
                    Find<Color>(h, "UI/In-game", "timerColor").Value = category.CreateOrFind("In-game Timer Color", Color.white, true).Value;
                    Find<string>(h, "UI/In-game", "portrait").Value = category.CreateOrFind("Custom Portrait", "", true).Value;
                    Find<bool>(h, "UI/In-game", "greenHP").Value = category.CreateOrFind("Enable Neon Green HP", true, true).Value;

                    bool infoEnable = false;
                    infoEnable |= Find<bool>(h, "UI/Info", "seshTimer").Value = category.CreateOrFind("Display Session Timer", true, true).Value;
                    infoEnable |= Find<bool>(h, "UI/Info", "levelTimer").Value = category.CreateOrFind("Display Level Timer", true, true).Value;
                    infoEnable |= Find<bool>(h, "UI/Info", "totalAttempts").Value = category.CreateOrFind("Show total Restarts", true, true).Value;
                    infoEnable |= Find<bool>(h, "UI/Info", "seshAttempts").Value = category.CreateOrFind("Show session Restarts", true, true).Value;
                    infoEnable |= Find<bool>(h, "UI/Info", "seshPB").Value = category.CreateOrFind("SessionPB", true, true).Value;
                    Find<bool>(h, "UI/Info", "enabled").Value = infoEnable;
                    Find<float>(h, "UI/Info", "scale").Value = 1920f / Screen.width; // set the default scale so it doesn't look any different right away

                    Find<bool>(h, "UI", "showMS").Value = category.CreateOrFind("Display in-depth in-game timer", true, true).Value;
                    Find<bool>(h, "UI", "noMission").Value = category.CreateOrFind("Remove Start Mission button in Job Archive", true, true).Value;
                    Find<bool>(h, "UI", "ghostDir").Value = category.CreateOrFind("Open Ghost Directory Button", true, true).Value;
                    Find<bool>(h, "UI", "noGreen").Value = category.CreateOrFind("Begone Apocalypse", true, true).Value;

                    Find<bool>(h, "Medals", "comMedals").Value = category.CreateOrFind("Enable Community Medals", true, true).Value;

                    Find<bool>(h, "Misc", "cheaters").Value = category.CreateOrFind("Enable Cheater Banlist", true, true).Value;
                    Find<bool>(h, "Misc", "bossGhosts").Value = category.CreateOrFind("Boss Recorder", true, true).Value;
                    Find<bool>(h, "Misc", "noIntro").Value = category.CreateOrFind("Disable Intro", true, true).Value;
                    Find<bool>(h, "Misc", "ambienceOff").Value = category.CreateOrFind("Ambience Remover", false, true).Value;
                    //Find<bool>(h, "Misc", "insightOff").Value = category.CreateOrFind("Insight Screen Remover", true).Value;
                    Find<bool>(h, "Misc", "insightOff").Value = true;

                    category = MelonPreferences.CreateCategory("NeonLite Visual Settings");

                    Find<bool>(h, "UI/In-game", "noPortrait").Value = category.CreateOrFind("Disable the Player portrait", false, true).Value;
                    Find<bool>(h, "UI/In-game", "noBackstory").Value = category.CreateOrFind("Disable backstory", false, true).Value;
                    Find<bool>(h, "UI/In-game", "noFlames").Value = category.CreateOrFind("Disable bottom bar", false, true).Value;
                    Find<bool>(h, "UI/In-game", "noWarning").Value = category.CreateOrFind("Disable low HP overlay", false, true).Value;
                    Find<bool>(h, "VFX", "noShocker").Value = category.CreateOrFind("Disable shocker overlay", false, true).Value;
                    Find<bool>(h, "VFX", "noBoof").Value = category.CreateOrFind("Disable book of life overlay", false, true).Value;

                    category = MelonPreferences.CreateCategory("NeonLite Discord Integration");

                    Find<bool>(h, "Discord", "enabled").Value = category.CreateOrFind("Discord Activity", false, true).Value;
                    Find<string>(h, "Discord", "menuTitle").Value = category.CreateOrFind("Headline in menu", "In menu", true).Value;
                    Find<string>(h, "Discord", "menuDesc").Value = category.CreateOrFind("Description in menu", "Sleeping", true).Value;
                    Find<string>(h, "Discord", "rushTitle").Value = category.CreateOrFind("Headline in level rush", "%t", true).Value;
                    Find<string>(h, "Discord", "rushDesc").Value = category.CreateOrFind("Description in level rush", "%l %i/%r", true).Value;
                    Find<string>(h, "Discord", "levelTitle").Value = category.CreateOrFind("Headline in level", "%l", true).Value;
                    Find<string>(h, "Discord", "levelDesc").Value = category.CreateOrFind("Description in level", "%l %i/%r", true).Value;
                    Find<bool>(h, "Discord", "seshTimer").Value = category.CreateOrFind("Show session timer", true, true).Value;
                }
            }

            category = MelonPreferences.CreateCategory("PowerPrefs adjustments");
            migrated = category.CreateEntry("MIGRATED", false, is_hidden: true).Value;
            if (!migrated)
            {
                category.GetEntry<bool>("MIGRATED").Value = true;

                // use chapter rush timer as a way of checking to be absolutely sure we have to migrate
                // this fails when rush tmer is false already but most people should have it disabled
                var entry = MelonPreferences.CreateCategory("Chapter Timer config").CreateOrFind("Chapter timer display enabled", true);
                if (!entry.Value)
                {
                    NeonLite.Logger.Msg("PuppyPowertools settings found! Attempting migration...");

                    Find<int>(h, "Misc", "rushSeed").Value = category.CreateOrFind("Level Rush Seed (negative is random)", -1, true).Value;

                    category = MelonPreferences.CreateCategory("Speedometer Config");

                    Find<bool>(h, "Speedometer", "enabled").Value = category.CreateOrFind("Speedometer Enabled", false, true).Value;
                    Find<float>(h, "Speedometer", "x").Value = (float)category.CreateOrFind("X Offset", 30, true).Value / Screen.width;
                    Find<float>(h, "Speedometer", "y").Value = (float)category.CreateOrFind("Y Offset", 30, true).Value / Screen.height;
                    Find<float>(h, "Speedometer", "scale").Value = category.CreateOrFind("Font Size", 20, true).Value / 20f;

                    Find<Color>(h, "Speedometer", "flatColor").Value = category.CreateOrFind("Text color (Default)", Color.yellow, true).Value;
                    Find<Color>(h, "Speedometer", "dashColor").Value = category.CreateOrFind("Text color (Dashing)", Color.blue, true).Value;
                    Find<Color>(h, "Speedometer", "fastColor").Value = category.CreateOrFind("Text color (Fast)", Color.green, true).Value;
                    Find<Color>(h, "Speedometer", "slowColor").Value = category.CreateOrFind("Text color (Slow)", Color.red, true).Value;

                    var verbose = category.CreateOrFind("Verbose Info", false, true).Value;
                    Find<bool>(h, "Speedometer", "lateral").Value = true;
                    Find<bool>(h, "Speedometer", "yVel").Value = true;
                    if (verbose)
                    {
                        Find<bool>(h, "Speedometer", "position").Value = true;
                        Find<bool>(h, "Speedometer", "rotation").Value = true;
                        Find<bool>(h, "Speedometer", "rawVel").Value = true;
                        Find<bool>(h, "Speedometer", "swapTimer").Value = true;
                        Find<bool>(h, "Speedometer", "jumpTimer").Value = true;
                    }

                    category = MelonPreferences.CreateCategory("VFX Toggles");

                    Find<bool>(h, "VFX", "noSun").Value = category.CreateOrFind("Disable sun [requires restart to re-enable :3]", false, true).Value;
                    Find<bool>(h, "VFX", "noBloom").Value = category.CreateOrFind("Disable bloom", false, true).Value;
                    Find<bool>(h, "VFX", "noReflections").Value = category.CreateOrFind("Disable reflection flares", false, true).Value;
                    Find<bool>(h, "VFX", "noFireball").Value = category.CreateOrFind("Disable fireball screen effect", false, true).Value;
                    Find<bool>(h, "VFX", "noStomp").Value = category.CreateOrFind("Disable stomp splashbang", false, true).Value;

                    Find<bool>(h, "UI", "noCRT").Value = category.CreateOrFind("Disable CRT effect", false, true).Value;

                    category = MelonPreferences.CreateCategory("Card Customizations");
                    Find<bool>(h, "Cards", "enabled").Value = category.CreateOrFind("Enable card customizations [changes require level restart]", false, true).Value;
                    Find<string>(h, "Cards", "elevate").Value = category.CreateOrFind("Elevate Text", "Elevate", true).Value;
                    Find<string>(h, "Cards", "purify").Value = category.CreateOrFind("Purify Text", "Purify", true).Value;
                    Find<string>(h, "Cards", "godspeed").Value = category.CreateOrFind("Godspeed Text", "Godspeed", true).Value;
                    Find<string>(h, "Cards", "stomp").Value = category.CreateOrFind("Stomp Text", "Stomp", true).Value;
                    Find<string>(h, "Cards", "fireball").Value = category.CreateOrFind("Fireball Text", "Fireball", true).Value;
                    Find<string>(h, "Cards", "dominion").Value = category.CreateOrFind("Dominion Text", "Dominion", true).Value;
                    Find<string>(h, "Cards", "boof").Value = category.CreateOrFind("Boof Text", "Book of Life", true).Value;
                    Find<string>(h, "Cards", "ammo").Value = category.CreateOrFind("Ammo Text", "Ammo", true).Value;
                    Find<string>(h, "Cards", "health").Value = category.CreateOrFind("Health Text", "Health", true).Value;
                }
            }

            category = MelonPreferences.CreateCategory("Crosshair");
            migrated = category.CreateEntry("MIGRATED", false, is_hidden: true).Value;
            if (!migrated)
            {
                category.GetEntry<bool>("MIGRATED").Value = true;

                var entry = category.CreateOrFind("Enabled", false, true);
                if (entry.Value)
                {
                    Find<bool>(h, "Crosshair", "enabled").Value = entry.Value;

                    Find<Color>(h, "Crosshair", "main").Value = category.CreateOrFind("Main Crosshair", Color.grey, true).Value;
                    Find<Color>(h, "Crosshair", "zipInner").Value = category.CreateOrFind("Zipline Inner", Color.grey, true).Value;
                    Find<Color>(h, "Crosshair", "zipOuter").Value = category.CreateOrFind("Zipline Outer", Color.white, true).Value;
                    Find<Color>(h, "Crosshair", "zipLocked").Value = category.CreateOrFind("Zipline Active", Color.green, true).Value;
                    Find<Color>(h, "Crosshair", "teleOn").Value = category.CreateOrFind("Boof Active", new Color(1f, 0f, 0.4308f, 1f), true).Value;
                    Find<Color>(h, "Crosshair", "teleOff").Value = category.CreateOrFind("Boof Inactive", Color.grey, true).Value;
                    Find<Color>(h, "Crosshair", "overheat").Value = category.CreateOrFind("Overheat", Color.grey, true).Value;
                }
            }

            category = MelonPreferences.CreateCategory("FullSync");
            migrated = category.CreateEntry("MIGRATED", false, is_hidden: true).Value;
            if (!migrated)
            {
                category.GetEntry<bool>("MIGRATED").Value = true;

                Find<bool>(h, "Optimization", "fullSync").Value = category.CreateOrFind("Apply Patch", true, true).Value;
            }

            while (readVersion < VERSION)
            {
                switch (readVersion)
                {
                    case 300:
                        Find<bool>(h, "Optimization", "cacheGhosts").Value = Find<bool>(h, "Misc", "cacheGhosts", true).Value;
                        Find<bool>(h, "Optimization", "fakeLoading").Value = Find<bool>(h, "Misc", "fakeLoading", true).Value;
                        Find<bool>(h, "Optimization", "fastStart").Value = Find<bool>(h, "Misc", "fastStart", true).Value;
                        Find<bool>(h, "Optimization", "updateGlobal").Value = Find<bool>(h, "Misc", "updateGlobal", true).Value;
                        Find<bool>(h, "Optimization", "updateGlobalP").Value = Find<bool>(h, "Misc", "updateGlobalP", true).Value;
                        Find<string>(h, "UI", "endingImage").Value = Find<string>(h, "UI/In-game", "ending", true).Value;
                        readVersion = 309;
                        break;
                    case 309:
                        Find<bool>(h, "VFX", "noStompSplash").Value = Find<bool>(h, "VFX", "noStomp").Value;
                        readVersion = 3001003;
                        break;
                }

                MelonPreferences.Save();
            }
        }
    }
}
