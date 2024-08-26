using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MelonLoader;
using NeonLite.Modules;
using UnityEngine;

namespace NeonLite
{
    public class NeonLite : MelonMod
    {
        // The main Harmony instance to use for patching functions.
        internal static new HarmonyLib.Harmony Harmony { get; private set; }
        // The main Logger instance for use with debug.
        internal static MelonLogger.Instance Logger { get; private set; }
        internal static Game Game { get { return Singleton<Game>.Instance; } }

#if DEBUG
        internal static bool DEBUG { get; private set; } = true;
#else
        internal const bool DEBUG = false;
#endif

        // The generic holder for everything that doesn't have to be in the main menu. **Initializes in time for low priority.**
        internal static GameObject holder;
        // The holder for everything that does have to be in the main menu. **Initializes in time for low priority.**
        internal static GameObject mmHolder;

        // An automatically populated list of all modules in NeonLite.
        internal static List<Type> modules = [];

        internal static AssetBundleCreateRequest bundleLoading;
        internal static AssetBundle bundle;
        internal static event Action<AssetBundle> OnBundleLoad;

        static bool setupCalled;
        static bool activateEarly;
        static bool activateLate;

        public override void OnInitializeMelon()
        {
            Settings.Setup();
#if DEBUG
            Settings.mainCategory.GetEntry<bool>("DEBUG").OnEntryValueChanged.Subscribe((_, a) => DEBUG = a);
            DEBUG = Settings.mainCategory.GetEntry<bool>("DEBUG").Value;
#endif
            Harmony = HarmonyInstance;
            Logger = LoggerInstance;

            LoadModules(MelonAssembly);

            // preform early inits
            foreach (var module in modules)
            {
                if (DEBUG)
                    Logger.Msg($"{module} Setup");

                try
                {
                    AccessTools.Method(module, "Setup").Invoke(null, null);
                }
                catch (Exception e)
                {
                    Logger.Error($"error in {module} Setup:");
                    Logger.Error(e);
                    continue;
                }
            }
            setupCalled = true;
        }

        internal static void ActivatePriority()
        {
            if (activateEarly)
                return;

            foreach (var module in modules.Where(t => (bool)AccessTools.Field(t, "priority").GetValue(null) && (bool)AccessTools.Field(t, "active").GetValue(null)))
            {
                if (DEBUG)
                    Logger.Msg($"{module} Activate");

                try
                {
                    AccessTools.Method(module, "Activate").Invoke(null, [true]);
                }
                catch (Exception e)
                {
                    Logger.Error($"error in {module} Activate:");
                    Logger.Error(e);
                    continue;
                }
            }
            activateEarly = true;
        }

        internal static void LoadAssetBundle()
        {
            if (bundleLoading != null)
                return;
            bundleLoading = AssetBundle.LoadFromMemoryAsync(Resources.r.bundle);
            bundleLoading.completed += _ =>
            {
                Logger.Msg("AssetBundle loading done!");
                bundle = bundleLoading.assetBundle;
                OnBundleLoad.Invoke(bundle);
            };
        }

        public override void OnLateInitializeMelon()
        {
            ActivatePriority();
            LoadAssetBundle();
            Singleton<Game>.Instance.OnInitializationComplete += OnInitComplete;
            Settings.Localize();
        }

        void OnInitComplete()
        {
            // mainmenu is now ready!
            Singleton<Game>.Instance.OnInitializationComplete -= OnInitComplete;

            holder = new GameObject("NeonLite");
            UnityEngine.Object.DontDestroyOnLoad(holder);

            mmHolder = new GameObject("NeonLite");
            mmHolder.transform.SetParent(MainMenu.Instance().transform.Find("Canvas"), false);
            mmHolder.transform.localScale = Vector3.one;

            // perform the later inits
            foreach (var module in modules.Where(t => !(bool)AccessTools.Field(t, "priority").GetValue(null) && (bool)AccessTools.Field(t, "active").GetValue(null)))
            {
                if (DEBUG)
                    Logger.Msg($"{module} Activate");

                try
                {
                    AccessTools.Method(module, "Activate").Invoke(null, [true]);
                }
                catch (Exception e)
                {
                    Logger.Error($"error in {module} Activate:");
                    Logger.Error(e);
                    continue;
                }
            }
            activateLate = true;

            Settings.Migrate();

            // force it to fetch even if it's off
            CommunityMedals.OnLevelLoad(null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadModules(MelonAssembly assembly)
        {
            var addedModules = assembly.Assembly.GetTypes().Where(t => typeof(IModule).IsAssignableFrom(t) && t != typeof(IModule) && !modules.Contains(t));
            if (setupCalled)
            {
                foreach (var module in addedModules)
                {
                    if (DEBUG)
                        Logger.Msg($"{module} Setup");

                    try
                    {
                        AccessTools.Method(module, "Setup").Invoke(null, null);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"error in {module} Setup:");
                        Logger.Error(e);
                        continue;
                    }
                }
            }

            if (activateEarly)
            {
                foreach (var module in addedModules.Where(t => (bool)AccessTools.Field(t, "priority").GetValue(null) && (bool)AccessTools.Field(t, "active").GetValue(null)))
                {
                    if (DEBUG)
                        Logger.Msg($"{module} Activate");

                    try
                    {
                        AccessTools.Method(module, "Activate").Invoke(null, [true]);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"error in {module} Activate:");
                        Logger.Error(e);
                        continue;
                    }
                }
            }

            if (activateLate)
            {
                foreach (var module in addedModules.Where(t => !(bool)AccessTools.Field(t, "priority").GetValue(null) && (bool)AccessTools.Field(t, "active").GetValue(null)))
                {
                    if (DEBUG)
                        Logger.Msg($"{module} Activate");

                    try
                    {
                        AccessTools.Method(module, "Activate").Invoke(null, [true]);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"error in {module} Activate:");
                        Logger.Error(e);
                        continue;
                    }
                }
            }

            //modules.UnionWith(addedModules);
            modules.AddRange(addedModules);
        }
    }
}
