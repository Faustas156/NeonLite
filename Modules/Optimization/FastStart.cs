using ClockStone;
using HarmonyLib;
using MelonLoader;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonLite.Modules.Optimization
{
    enum GameInit
    {
        Uninitialized,
        Start,
#if XBOX
		PlatformLoading,
		PlatformLoaded,
        PrefsLoading,
#endif
        PrefsLoaded,
        MenuHolderLoading,
        MenuHolderLoaded,
        SaveDataLoading,
        SaveDataLoaded,
        AdditionalScenesLoading,
        AdditionalScenesLoaded,
        Complete
    }

    [HarmonyPatch]
    internal class FastStart : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static bool wasSetup = false;
        static bool gameDataLoaded = false;

        internal static AsyncOperation audioPreload;
        internal static AsyncOperation menuPreload;
        internal static AsyncOperation enemyPreload;
        static bool enemiesLoaded = false;

        static readonly Stopwatch stopwatch = new();

        internal static void Setup()
        {
            if (wasSetup)
                return;

            wasSetup = true;
            var setting = Settings.Add(Settings.h, "Optimization", "fastStart", "Fast Startup", "Preloads essential scenes before the game even initializes to speed up the menu load.", true);
            active = setting.SetupForModule(Activate, (_, after) => after);
            if (active)
            {
                GS.savingAllowed = false;
                Preload();
                SceneManager.activeSceneChanged += OnFirstLoad;
            }
        }

        static IEnumerator ActivatePriority()
        {
            yield return null;
            NeonLite.ActivatePriority();
        }

        static void Activate(bool activate) => active = activate;

        static void Preload()
        {
            NeonLite.Logger.Msg("Started scene preload, please wait...!");
            stopwatch.Start();

            audioPreload = SceneManager.LoadSceneAsync("Audio", LoadSceneMode.Additive);
            audioPreload.completed += _ => PreloadDone();
            NeonLite.LoadAssetBundle();
        }

        static bool menuActivate = false;
        static bool audioActivate = false;

        static IEnumerator LoadScenes()
        {
            //menuPreload = SceneManager.LoadSceneAsync("MenuHolder", LoadSceneMode.Additive);
            enemyPreload = SceneManager.LoadSceneAsync("Enemies", LoadSceneMode.Additive);
            audioPreload = SceneManager.LoadSceneAsync("Audio", LoadSceneMode.Additive);
            do
            {
                //NeonLite.Logger.Warning($"{menuActivate} {audioActivate}");
                //menuPreload.allowSceneActivation = menuActivate;
                audioPreload.allowSceneActivation = audioActivate;
                yield return null;
            }
            while (!menuActivate || !audioActivate);
        }

        private static void OnFirstLoad(Scene _1, Scene _2)
        {
            SceneManager.activeSceneChanged -= OnFirstLoad;

            Localization.Activate(true);

            MelonCoroutines.Start(ActivatePriority());
            MelonCoroutines.Start(LoadScenes());
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), "SetInitializationState")]
        static bool SetInitState(Game __instance, GameInit initializationState, ref GameInit ____initializationState)
        {
            if (!active)
                return true;

#if DEBUG
            NeonLite.Logger.DebugMsg($"SetInitState {initializationState}");

            NeonLite.Logger.DebugMsg(new StackFrame(2));
#endif

            if (initializationState == GameInit.PrefsLoaded)
            {
                Singleton<GameInput>.Instance.Initialize();
                ____initializationState = GameInit.MenuHolderLoading;
                menuPreload = SceneManager.LoadSceneAsync("MenuHolder", LoadSceneMode.Additive);
                menuPreload.completed += _ => Helpers.Method(typeof(Game), "SetInitializationState").Invoke(__instance, [(int)GameInit.MenuHolderLoaded]);
                menuActivate = true;
                return false;
            }

            NeonLite.Logger.DebugMsg($"Called with {initializationState} now {____initializationState}");
            NeonLite.Logger.DebugMsg($"Change? {initializationState > ____initializationState}");

            return initializationState > ____initializationState;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), "OnGameDataLoaded")]
        static bool UnloadScenesRewrite(Game __instance, ref GameInit ____initializationState)
        {
            NeonLite.Logger.DebugMsg("OnGameDataLoaded");

            if (!active)
                return true;
            if (gameDataLoaded)
                return false;
            gameDataLoaded = true;

#if XBOX
            GameDataManager.ApplyShadowPrefs();
#endif
            audioPreload.completed += _ => Helpers.Method(typeof(Game), "SetInitializationState").Invoke(__instance, [(int)GameInit.AdditionalScenesLoaded]);
            audioActivate = true;
            GS.savingAllowed = true;

            return false;
        }
        static void PreloadDone()
        {
            stopwatch.Stop();
            NeonLite.Logger.Msg($"Preload done in {stopwatch.ElapsedMilliseconds}ms.");
            //SceneManager.UnloadSceneAsync("Audio");
        }

        // AUDIO OPTIMIZATIONS
        static MethodInfo itemInit = Helpers.Method(typeof(AudioItem), "_Initialize");
        static MethodInfo validate = Helpers.Method(typeof(AudioController), "_ValidateAudioObjectPrefab");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AudioController), "InitializeAudioItems")]
        static bool RewriteAudioInit(AudioController __instance, ref Dictionary<string, AudioItem> ____audioItems, List<AudioController> ____additionalAudioControllers)
        {
            // this func isn't *slow* but it can be better
            // it makes too many reallocations while so much is already going on
            // so we can help it out a little and be a little more dangerous
            NeonLite.Logger.DebugMsg($"CALLED BY {__instance.name} {__instance.isAdditionalAudioController} {____additionalAudioControllers}");

            if (__instance.isAdditionalAudioController || ____additionalAudioControllers == null)
                return false;

            // initialize all audio items
            int count = 0;
            List<AudioController> controllers = [__instance, .. ____additionalAudioControllers];
            foreach (var controller in controllers)
            {
                NeonLite.Logger.DebugMsg($"AUDIOCONTROLLER {controller.name} {controller.AudioCategories.Length}");
                foreach (var category in controller.AudioCategories)
                {
                    NeonLite.Logger.DebugMsg($"AUDIOCATEGORY {category.Name} {category.AudioObjectPrefab} {category.AudioItems.Length}");
                    category.audioController = controller;

                    foreach (var item in category.AudioItems)
                    {
                        count++;
                        itemInit.Invoke(item, [category]);
                    }
                    if (category.AudioObjectPrefab)
                        validate.Invoke(controller, [category.AudioObjectPrefab]);
                }
            }

            Dictionary<string, AudioItem> res = new(count);
            
            foreach (var item in controllers.SelectMany(x => x.AudioCategories).SelectMany(x => x.AudioItems))
            {
                if (item == null || res.ContainsKey(item.Name))
                    continue;
                res.Add(item.Name, item);
            }

            ____audioItems = res;
                
            return false;
        }

#if !XBOX
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameDataManager), "OnReadPowerPrefsComplete")]
        static void CheckOldAnticheat(PowerUserPrefs data)
        {
            // due to faststart now starting faster since 3.0.10, some anticheats activate too fast and get overwritten
            // all this does is check the current and set it back if needed
            if (GameDataManager.powerPrefs.dontUploadToLeaderboard)
                data.dontUploadToLeaderboard = true;
        }
#endif
    }
}

