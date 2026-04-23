using System.Runtime.CompilerServices;
using MelonLoader;
using NeonLite.Modules;
using NeonLite.Modules.Optimization;
using UnityEngine;

[assembly: MelonPriority(-1000)]

namespace NeonLite
{
    public class NeonLite : MelonMod
    {
        // The main Harmony instance to use for patching functions.
        internal static new HarmonyLib.Harmony Harmony { get; private set; }
        // The main Logger instance for use with debug.
        internal static MelonLogger.Instance Logger { get; private set; }
        static Game _gamecache = null;
        internal static Game Game { get { _gamecache ??= Singleton<Game>.Instance; return _gamecache; } }

#if DEBUG
        internal static bool DEBUG { get; private set; } = true;
#else
        internal const bool DEBUG = false;
#endif

        internal static NeonLite i;

        // The generic holder for everything that doesn't have to be in the main menu. **Initializes in time for low priority.**
        internal static GameObject holder;
        // The holder for everything that does have to be in the main menu. **Initializes in time for low priority.**
        internal static GameObject mmHolder;

        // An automatically populated list of all modules in NeonLite.
        internal static List<Type> modules = [];

        internal readonly static HashSet<MelonAssembly> expected = [];
        internal readonly static HashSet<MelonAssembly> loaded = [];

        internal static AssetBundleCreateRequest bundleLoading;
        internal static AssetBundle bundle;
        internal static event Action<AssetBundle> OnBundleLoad;

        internal static bool activateEarly;
        internal static bool activateLate;

        public override void OnEarlyInitializeMelon()
        {
            i = this;
            Logger = LoggerInstance;
            Settings.Setup();
            FastStart.Setup();
        }

        public override void OnInitializeMelon()
        {
            GCManager.DisableGC(GCManager.GCType.Initialization);

            VersionText.ver = Info.Version;
            Logger.Msg($"Version: {Info.Version}");
            UnityEngine.Debug.Log($"NeonLite Version: {Info.Version}");

#if DEBUG
            Settings.mainCategory.GetEntry<bool>("DEBUG").OnEntryValueChanged.Subscribe(static (_, a) => DEBUG = a);
            DEBUG = Settings.mainCategory.GetEntry<bool>("DEBUG").Value;

            Anticheat.Register(MelonAssembly);
            // Anticheat.EnableSaveRedirection("testma", true);
#endif
            Harmony = HarmonyInstance;

            LoadModules(MelonAssembly);

            foreach (var m in RegisteredMelons)
            {
                if (Helpers.HasModulesInAssembly(m.MelonAssembly.Assembly))
                    expected.Add(m.MelonAssembly);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ActivatePriority()
        {
            if (activateEarly)
                return;

            Helpers.StartProfiling("NeonLite Activate-priority Pass");

            foreach (var module in modules.Where(static t => Helpers.GetModulePrio(t) && (bool)Helpers.Field(t, "active").GetValue(null)))
            {
                Logger.DebugMsg($"{module} Activate");

                Helpers.StartProfiling($"{module}");

                try
                {
                    Helpers.Method(module, "Activate", [typeof(bool)])?.Invoke(null, [true]);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in {module} Activate:");
                    Logger.Error(e);
                    continue;
                }
                finally
                {
                    Helpers.EndProfiling();
                }
            }
            Helpers.EndProfiling();
            Patching.RunPatches(true);
            activateEarly = true;

        }

        internal static void LoadAssetBundle()
        {
            if (bundleLoading != null)
                return;
            bundleLoading = AssetBundle.LoadFromStreamAsync(Resources.neonlite.GetStream());
            bundleLoading.completed += static _ =>
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
            if (!FastStart.active)
                Game.OnInitializationComplete += OnInitComplete;
            Settings.Localize();
        }

        internal void OnInitComplete()
        {
            // mainmenu is now ready!
            Game.OnInitializationComplete -= OnInitComplete;

            holder = new GameObject("NeonLite");
            UnityEngine.Object.DontDestroyOnLoad(holder);

            mmHolder = new GameObject("NeonLite", typeof(CanvasGroup));
            mmHolder.transform.SetParent(MainMenu.Instance().transform.Find("Canvas"), false);
            mmHolder.transform.localScale = Vector3.one;
            var mmcg = mmHolder.GetComponent<CanvasGroup>();
            mmcg.alpha = 0;
            mmcg.blocksRaycasts = mmcg.interactable = false;

            // perform the later inits
            Helpers.StartProfiling("NeonLite Activate-nonpriority Pass");
            foreach (var module in modules.Where(static t => !Helpers.GetModulePrio(t) && (bool)Helpers.Field(t, "active").GetValue(null)))
            {
                Logger.DebugMsg($"{module} Activate");
                Helpers.StartProfiling($"{module}");

                try
                {
                    Helpers.Method(module, "Activate", [typeof(bool)])?.Invoke(null, [true]);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in {module} Activate:");
                    Logger.Error(e);
                    continue;
                }
                finally
                {
                    Helpers.EndProfiling();
                }
            }
            Helpers.EndProfiling();

            activateLate = true;
            // patching these in parallel would VERY rarely cause weird issues
            Patching.RunPatches(false);

            Settings.Migrate();

            // force it to fetch even if it's off
            CommunityMedals.OnLevelLoad(null);

            GCManager.EnableGC(GCManager.GCType.Initialization);
        }

        public static void LoadModules(MelonAssembly assembly)
        {
            var addedModulesL = Helpers.GetModulesInAssembly(assembly.Assembly, true);
            if (addedModulesL.Count == 0)
                return; // do not bother

            // sanity check
            List<Type> errors = [];

            foreach (var t in addedModulesL)
            {
                var p = Helpers.Field(t, "priority");
                var a = Helpers.Field(t, "active");

                if (p == null || a == null)
                {
                    Logger.Error($"Module {t} from {assembly.Assembly.GetName().Name} is incorrectly configured!!");
                    errors.Add(t);
                }
            }

            var addedModules = addedModulesL.Except(errors).ToArray();

            Logger.DebugMsg($"DoSetup {assembly.Assembly.GetName().Name}");
            {
                int iS = 0;
                int completedS = 0;

                Helpers.StartProfiling($"NeonLite Setup Pass - {assembly.Assembly.GetName().Name}");

                foreach (var module in addedModules)
                {
                    Logger.DebugMsg($"{module} Setup");

                    Helpers.StartProfiling($"{module}");

                    try
                    {
                        Helpers.Method(module, "Setup", [])?.Invoke(null, null);
                        ++completedS;
                    }
                    catch (Exception e)
                    {
                        Logger.Warning($"Error in {module} Setup:");
                        Logger.Error(e);
                        continue;
                    }
                    finally
                    {
                        ++iS;
                        Helpers.EndProfiling();
                    }
                }

                Logger.Msg($"Setup {completedS}/{iS} modules from {assembly.Assembly.GetName().Name}.");
                Helpers.EndProfiling();
            }

            int i = 0;
            int completed = 0;


            Logger.DebugMsg($"DoEarly {assembly.Assembly.GetName().Name}");
            if (activateEarly)
            {
                Helpers.StartProfiling($"NeonLite Activate-priority Pass - {assembly.Assembly.GetName().Name}");

                foreach (var module in addedModules.Where(static t => Helpers.GetModulePrio(t) && (bool)Helpers.Field(t, "active").GetValue(null)))
                {
                    Logger.DebugMsg($"{module} Activate");
                    Helpers.StartProfiling($"{module}");

                    try
                    {
                        Helpers.Method(module, "Activate", [typeof(bool)])?.Invoke(null, [true]);
                        ++completed;
                    }
                    catch (Exception e)
                    {
                        Logger.Warning($"Error in {module} Activate:");
                        Logger.Error(e);
                        continue;
                    }
                    finally
                    {
                        ++i;
                        Helpers.EndProfiling();
                    }
                }

                Helpers.EndProfiling();
            }

            Logger.DebugMsg($"DoLate {assembly.Assembly.GetName().Name}");
            if (activateLate)
            {
                Helpers.StartProfiling($"NeonLite Activate-nonpriority Pass - {assembly.Assembly.GetName().Name}");

                foreach (var module in addedModules.Where(static t => !Helpers.GetModulePrio(t) && (bool)Helpers.Field(t, "active").GetValue(null)))
                {
                    Logger.DebugMsg($"{module} Activate");
                    Helpers.StartProfiling($"{module}");

                    try
                    {
                        Helpers.Method(module, "Activate", [typeof(bool)])?.Invoke(null, [true]);
                        ++completed;
                    }
                    catch (Exception e)
                    {
                        Logger.Warning($"Error in {module} Activate:");
                        Logger.Error(e);
                        continue;
                    }
                    finally
                    {
                        ++i;
                        Helpers.EndProfiling();
                    }
                }

                Helpers.EndProfiling();
            }

            loaded.Add(assembly);
            modules.AddRange(addedModules);
            LoadManager.AddModules(addedModules);
            Verifier.AddModules(addedModules);

            Logger.DebugMsg($"Check {assembly.Assembly.GetName().Name}");

            if (activateEarly)
                Patching.RunPatches(true);
            else if (loaded.SetEquals(expected))
                ActivatePriority();
        }
    }
}
