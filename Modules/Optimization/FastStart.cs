using HarmonyLib;
using MelonLoader;
using System.Collections;
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

        static bool preload = false;
        static bool gameDataLoaded = false;

        internal static AsyncOperation audioPreload;
        internal static AsyncOperation menuPreload;
        internal static AsyncOperation enemyPreload;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "fastStart", "Fast Startup", "Preloads essential scenes before the game even initializes to speed up the menu load.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;

            //MelonCoroutines.Start(PreloadCoroutine());
        }

        static IEnumerator PreloadCoroutine()
        {
            yield return null;
            NeonLite.ActivatePriority();
        }

        /*
        static readonly MethodInfo loadon = AccessTools.Method(typeof(Setup), "Start");
        static readonly MethodInfo ogstate = AccessTools.Method(typeof(Game), "SetInitializationState");
        static readonly MethodInfo ogdata = AccessTools.Method(typeof(Game), "OnGameDataLoaded");//*/

        static void Activate(bool activate)
        {
            /*
            if (activate)
            {
                Patching.AddPatch(loadon, Preload, Patching.PatchTarget.Prefix);
                Patching.AddPatch(ogstate, SetInitState, Patching.PatchTarget.Prefix);
                Patching.AddPatch(ogdata, UnloadScenesRewrite, Patching.PatchTarget.Prefix);
            }
            else
            {
                Patching.RemovePatch(loadon, Preload);
                Patching.RemovePatch(ogstate, SetInitState);
                Patching.RemovePatch(ogdata, UnloadScenesRewrite);
            }//*/

            active = activate;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Setup), "Start")]
        static void Preload()
        {
            if (!active || preload) 
                return;
            preload = true;
            NeonLite.Logger.Msg("Started scene preload, please wait...!");
            menuPreload = SceneManager.LoadSceneAsync("MenuHolder", LoadSceneMode.Additive);
            enemyPreload = SceneManager.LoadSceneAsync("Enemies", LoadSceneMode.Additive);
            audioPreload = SceneManager.LoadSceneAsync("Audio", LoadSceneMode.Additive);
            NeonLite.LoadAssetBundle();
            MelonCoroutines.Start(PreloadCoroutine());
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), "SetInitializationState")]
        static bool SetInitState(Game __instance, GameInit initializationState, ref GameInit ____initializationState)
        {
            if (!active)
                return true;

            if (NeonLite.DEBUG)
                NeonLite.Logger.Msg($"SetInitState {initializationState}");

            if (initializationState == GameInit.PrefsLoaded)
            {
                Singleton<GameInput>.Instance.Initialize();
                ____initializationState = GameInit.MenuHolderLoading;
                if (menuPreload.isDone)
                    ____initializationState = GameInit.MenuHolderLoaded;
                else
                    menuPreload.completed += _ => AccessTools.Field(typeof(Game), "_initializationState").SetValue(__instance, (int)GameInit.MenuHolderLoaded);
                return false;
            }
            return initializationState > ____initializationState;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), "OnGameDataLoaded")]
        static bool UnloadScenesRewrite(Game __instance, ref GameInit ____initializationState)
        {
            if (!active)
                return true;
            if (gameDataLoaded)
                return false;
            gameDataLoaded = true;

#if XBOX
            GameDataManager.ApplyShadowPrefs();
#endif
            if (audioPreload.isDone)
                ____initializationState = GameInit.AdditionalScenesLoaded;
            else
                audioPreload.completed += _ => AccessTools.Field(typeof(Game), "_initializationState").SetValue(__instance, (int)GameInit.AdditionalScenesLoaded);
            return false;
        }
    }
}

