using ClockStone;
using Guirao.UltimateTextDamage;
using HarmonyLib;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NeonLite.Modules.Optimization
{
    public class SuperRestart : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;
        static bool ready = false;

        internal static MelonPreferences_Entry<bool> setting;
        static MelonPreferences_Entry<bool> noStaging;
        static MelonPreferences_Entry<bool> pauseStaging;
        static MelonPreferences_Entry<int> gcTimer;
        static MelonPreferences_Entry<int> memTimer;
        static void Setup()
        {
            setting = Settings.Add(Settings.h, "Optimization", "superRestart", "Quick Restart", "Completely overrides the level loading routine on restart to be faster.", true);
            noStaging = Settings.Add(Settings.h, "Optimization", "noStagingSR", "Skip Staging Screen", "Skips the staging screen while using Quick Restart.", false);
            pauseStaging = Settings.Add(Settings.h, "Optimization", "pauseStagingSR", "Pause Restarts to Staging", null, true, true);
            gcTimer = Settings.Add(Settings.h, "Optimization", "gcTimerSR", "Restarts to call GC", null, 50, true);
            memTimer = Settings.Add(Settings.h, "Optimization", "memTimerSR", "Restarts to call Resource cleanup", null, 10, true);
            active = setting.SetupForModule(Activate, (_, after) => after);

            var useScreenshot = Settings.Add(Settings.h, "Optimization", "useScreenshotSR", "Minimize Flashing", "Use a screenshot of the stage to minimize flashing.\n**Requires Quick Restart.**", true);
            LoadingScreenshot.active = useScreenshot.SetupForModule(LoadingScreenshot.Activate, (_, after) => after);
        }

        static readonly List<MethodInfo> prefixToRegister = [
            Helpers.Method(typeof(OnRegion), "Start", null),
            Helpers.Method(typeof(EnemyEncounter), "Setup", null),
            Helpers.Method(typeof(LevelGate), "Start", null),
            Helpers.Method(typeof(ParticleSystem), "Play", null),
            Helpers.Method(typeof(GhostHintOriginVFX), "OnTriggerEnter", null),
            Helpers.Method(typeof(GhostPlayback), "ProcessTriggers", null),
            Helpers.Method(typeof(GhostPlayback), "ResetTimer", null),
            Helpers.Method(typeof(CardPickupSpawner), "SpawnCard", []),
            Helpers.Method(typeof(CardPickupSpawner), "SpawnCard", [typeof(float)]),
            Helpers.Method(typeof(BeamWeapon), "StartBeamTrackingRoutine", null),
            Helpers.Method(typeof(ObjectSpawner), "Spawn", []),
            Helpers.Method(typeof(ObjectSpawner), "Spawn", [typeof(float)]),
            Helpers.Method(typeof(EnemySpawner), "SpawnDelay", null),
            Helpers.Method(typeof(EnemyWaveSpecificObject), "Spawn", null),
            Helpers.Method(typeof(EnemyWave), "SpawnWave", null),
            Helpers.Method(typeof(RememberTransform), "Start", null),
            Helpers.Method(typeof(RememberTransform), "Apply", null),
            Helpers.Method(typeof(LevelTrigger), "OnTriggered", null),
            Helpers.Method(typeof(TripwireWeapon), "OnTripped", null),
            Helpers.Method(typeof(FailStateDetector_Deluxe), "Start", null),
        ];

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(Game), "LevelSetupRoutine", OverridePlayLevel, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(Game), "SetActiveScene", SetActiveScene, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(LevelPlaythrough), "OnEnemyKill", Never, Patching.PatchTarget.Prefix);

            Patching.TogglePatch(activate, typeof(ObjectSpawner), "SpawnObject", MarkForDestroy, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(EnemySpawner), "InstantiateEnemy", MarkForDestroyEnemy, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(ObjectPool), "Spawn", MarkForDestroy, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(CardPickupSpawner), "ProcessObject", MarkForDestroyCard, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(CardPickup), "Spawn", MarkForDestroyCard2, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(EnemyTripwire), "Die", MarkForDestroyTripwire, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(EnemyMimic), "Attack", MarkForDestroyMimic, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(BreakableFractureFX), "Explode", MarkForDestroyFracture, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(UltimateTextDamageManager), "Start", MarkForDestroyDamageText, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(GhostPlayback), "Start", MarkForDestroyGhost, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(Utils), "PreloadFromResources", MarkForDestroyPreload, Patching.PatchTarget.Transpiler);

            Patching.TogglePatch(activate, typeof(MoveTransform), "Start", RememberMoveTransformSpeed, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(Enemy), "SearchForTarget", FixTargetLog, Patching.PatchTarget.Prefix);

            Patching.TogglePatch(activate, typeof(MechController), "Die", OverrideDie, Patching.PatchTarget.Prefix);

            Patching.TogglePatch(activate, typeof(MenuScreenLoading), "LoadScene", PostMenuLoad, Patching.PatchTarget.Postfix);

            Patching.TogglePatch(activate, typeof(ObjectPool), "Despawn", UnmarkForDestroyPool, Patching.PatchTarget.Postfix);

            Patching.TogglePatch(activate, typeof(MainMenu), "Update", PreMMUpdate, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(MainMenu), "Update", PostMMUpdate, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(MainMenu), "PauseGame", MMPausing, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(MainMenu), "OnDialogueEnd", Reset, Patching.PatchTarget.Prefix);

            if (activate)
            {
                foreach (var func in prefixToRegister)
                {
                    if (!registry.ContainsKey(func.DeclaringType))
                        registry[func.DeclaringType] = new(new(30), new(30));

                    var manual = Helpers.Method(typeof(SuperRestart), "AddToRegistry", generics: [func.DeclaringType]);
                    Patching.AddPatch(func, manual.ToNewHarmonyMethod(), Patching.PatchTarget.Prefix);
                }
            }
            else
            {
                Reset();
                if (LoadingScreenshot.i)
                    LoadingScreenshot.i.Stop();

                GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;

                foreach (var func in prefixToRegister)
                {
                    var manual = Helpers.Method(typeof(SuperRestart), "AddToRegistry", generics: [func.DeclaringType]);

                    Patching.RemovePatch(func, manual);
                }
            }

            active = activate;
        }

        static void OnLevelLoad(LevelData level)
        {
            if (level == null)
                Reset();
        }

        static bool Never() => false;

        public static void Reset()
        {
            ready = false;
            activeScene = 0;
            restartCountGC = 0;
            restartCountRC = 0;
            destroy.Clear();
            reserveList.Clear();
            rememberSpeed.Clear();
            forceStaging = false;
            ClearRegistry();
            if (dontSave)
                GS.savingAllowed = true;
            dontSave = false;
        }

        public static readonly HashSet<string> blacklisted = [
            "HUB_HEAVEN",
            "TUT_ORIGIN",
            "SIDEQUEST_GREEN_MEMORY",
            "SIDEQUEST_GREEN_MEMORY_2",
            "SIDEQUEST_GREEN_MEMORY_3",
            "SIDEQUEST_GREEN_MEMORY_4",
            "GRID_BOSS_RAPTURE" // TEMP
        ];

        static int activeScene;
        static int restartCountGC;
        static int restartCountRC;
        static bool dontSave;
        static IEnumerator OverridePlayLevel(IEnumerator __result, Game __instance, LevelData newLevel, LevelData ____currentLevel)
        {
            if (dontSave)
                GS.savingAllowed = true;
            dontSave = false;

            if (newLevel == ____currentLevel && ready && !blacklisted.Contains(newLevel.levelID))
            {
                if (LoadingScreenshot.i)
                    LoadingScreenshot.i.Screenshot();
                if (gcTimer.Value != -1 && ++restartCountGC >= gcTimer.Value)
                {
                    restartCountGC = 0;
                    GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
                }
                if (memTimer.Value != -1 && ++restartCountRC >= memTimer.Value)
                    restartCountRC = 0;

                MainMenu.Instance().SetState(MainMenu.State.Loading, true, true, true, false);
                yield return QuickLevelSetup(__instance, newLevel, StagingHappens() || mmUpdating);

                if (restartCountGC == 0)
                    GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
            }
            else
            {
                /*if (newLevel.type == LevelData.LevelType.Level)
                {
                    yield return TrimmedLevelSetup(__instance, newLevel);
                    yield break;
                }//*/
                GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;

                if (LoadingScreenshot.i)
                    LoadingScreenshot.i.Stop();

                Reset();

                while (__result.MoveNext())
                    yield return __result.Current;

                if (newLevel && newLevel.type != LevelData.LevelType.Hub)
                {
                    GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
                    ready = true;
                }
            }
        }
        static bool OverrideDie(MechController __instance, bool restartImmediately, bool playRestartSound, AudioObject ___audioBoostLoop)
        {
            if (!restartImmediately || !playRestartSound)
                return true;
            var g = Singleton<Game>.Instance;
            if (LevelRush.IsLevelRush() && g.GetCurrentLevelTimerMicroseconds() > 0)
                LevelRush.UpdateLevelRushTimerMicroseconds(g.GetCurrentLevelTimerMicroseconds());
            if (___audioBoostLoop)
                    ___audioBoostLoop.Stop();
            if (RM.alertManager)
                RM.alertManager.ClearAlert(false);
            AudioController.Play("UI_LEVEL_RESET", MainMenu.Instance().transform);
            dontSave = true;
            GS.savingAllowed = false;
            g.PlayLevel(g.GetCurrentLevel(), g.IsLevelPlayedFromArchive(), true);
            return false;
        }

        static void SetActiveScene(int activeSceneIndex) => activeScene = activeSceneIndex;

        static readonly HashSet<GameObject> destroy = [];
        static void MarkForDestroy(GameObject __result) => destroy.Add(__result);
        static void MarkForDestroyEnemy(GameObject ____enemyObj) => destroy.Add(____enemyObj);

        static void MarkForDestroyCard(GameObject obj) => destroy.Add(obj);
        static void MarkForDestroyCard2(ref CardPickup pickup)
        {
            if (pickup)
                destroy.Add(pickup.gameObject);
        }
        static void MarkForDestroyTripwire(EnemyTripwire __instance) => destroy.Add(__instance.CurrentWeapon.gameObject);
        static void MarkForDestroyMimic(EnemyMimic __instance) => destroy.Add(__instance.weapon.gameObject);
        static void MarkForDestroyFracture(BreakableFractureFX __instance)
        {
            if (__instance.transform.parent == null)
                destroy.Add(__instance.gameObject);
        }
        static readonly FieldInfo tempObjs = Helpers.Field(typeof(UltimateTextDamageManager), "m_tempObjects");
        static void MarkForDestroyDamageText(UltimateTextDamageManager __instance)
        {
            var objs = tempObjs.GetValue<List<GameObject>>(__instance);
            foreach (var obj in objs)
            {
                obj.name = "UltimateTextDamageManager Temp";
                destroy.Add(obj);
            }
        }
        static void MarkForDestroyGhost(GhostPlayback __instance, GameObject ___m_ghostObject, GhostBullet ___m_cloneBullet)
        {
            if (__instance.ghostType != GhostUtils.GhostType.PersonalGhost)
                return;
            destroy.Add(___m_ghostObject);
            destroy.Add(___m_cloneBullet.gameObject);
        }

        static readonly Dictionary<MoveTransform, float> rememberSpeed = [];
        static void RememberMoveTransformSpeed(MoveTransform __instance)
        {
            if (__instance.speedUpAfterSeconds >= 0 && !rememberSpeed.ContainsKey(__instance))
                rememberSpeed.Add(__instance, __instance.speed);
        }

        static IEnumerable<CodeInstruction> MarkForDestroyPreload(IEnumerable<CodeInstruction> instructions)
        {
            var enqueue = Helpers.Method(typeof(Queue<>).MakeGenericType(typeof(GameObject)), "Enqueue");
            foreach (var code in instructions)
            {
                if (code.Calls(enqueue))
                {
                    yield return new(OpCodes.Dup);
                    yield return CodeInstruction.Call(typeof(SuperRestart), "MarkForDestroy");
                }
                yield return code;
            }
        }

        static bool FixTargetLog() => RM.mechController;
        struct ReserveInfo
        {
            public Transform parent;
            public int sibling;

            public GameObject reserved;
            public string name;
            public GameObject newObj;
        }

        static readonly List<ReserveInfo> reserveList = new(50);
        static readonly Type[] rememberTransformTypes = [typeof(DFlattener), typeof(MoveTransform), typeof(RotateTransform), typeof(RotateAroundAxis), typeof(ShakePosition)];

        static bool ParentsAreMarked(GameObject obj)
        {
            var transform = obj.transform;
            while (transform)
            {
                if (destroy.Contains(transform.gameObject))
                    return true;
                transform = transform.parent;
            }
            return false;
        }

        static IEnumerator PostMenuLoad(IEnumerator __result, string sceneName)
        {
            while (__result.MoveNext())
                yield return __result.Current;
            NeonLite.Logger.DebugMsg($"PostMenuLoad {sceneName}");
            if (!LoadManager.currentLevel || blacklisted.Contains(Singleton<Game>.Instance.GetCurrentLevel()?.levelID))
                yield break;
            var s = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.GetSceneAt(SceneManager.sceneCount - 1));
            var cur = SceneManager.GetActiveScene();

            IEnumerable<GameObject> iter = [];
            if (sceneName != "Player")
            {
                var reserve = new GameObject()
                {
                    name = "Reserve"
                }.transform;
                reserve.gameObject.SetActive(false);

                iter = Object.FindObjectsOfType<BaseDamageable>(true).Select(x => x.gameObject);

                foreach (var comp in iter)
                {
                    if (comp.scene == cur && !ParentsAreMarked(comp) && comp != reserve.gameObject)
                    {
                        destroy.Add(comp);
                        var obj = Object.Instantiate(comp, reserve);
                        reserveList.Add(new ReserveInfo
                        {
                            parent = comp.transform.parent,
                            sibling = comp.transform.GetSiblingIndex(),

                            reserved = obj,
                            name = comp.name
                        });
                    }
                }

                foreach (var type in rememberTransformTypes)
                {
                    foreach (var comp in Object.FindObjectsOfType(type, true).Cast<Component>())
                        comp.GetOrAddComponent<RememberTransform>();
                }
            }

            SceneManager.SetActiveScene(s);
        }


        static void UnmarkForDestroyPool(GameObject obj)
        {
            if (obj == null)
                destroy.Remove(obj);
        }

        static readonly Dictionary<Type, Tuple<List<Object>, HashSet<Object>>> registry = new(prefixToRegister.Count);
        static void AddToRegistry<T>(T __instance) where T : Object
        {
            if (!registry.ContainsKey(typeof(T)))
                registry[typeof(T)] = new([], []);
            var regList = registry[typeof(T)];
            if (!regList.Item2.Add(__instance))
                return;
            regList.Item1.Add(__instance);
        }

        static IEnumerable<T> InRegistry<T>(bool clear = false, bool sanitize = true) where T : Object
        {
            if (!registry.ContainsKey(typeof(T)))
                yield break;
            var regList = registry[typeof(T)];
            var iterCopy = regList.Item1.ToArray();
            if (clear)
            {
                regList.Item1.Clear();
                regList.Item2.Clear();
            }
            for (int i = 0; i < iterCopy.Length; i++)
            {
                if (!sanitize || iterCopy[i])
                    yield return iterCopy[i] as T;
            }
        }
        static void ClearRegistry()
        {
            foreach (var kv in registry)
            {
                kv.Value.Item1.Clear();
                kv.Value.Item2.Clear();
            }
        }


        static readonly FieldInfo waitForStaging = Helpers.Field(typeof(Game), "_waitForStaging");
        static readonly FieldInfo currentPlaythrough = Helpers.Field(typeof(Game), "_currentPlaythrough");
        static readonly FieldInfo onlvlload = Helpers.Field(typeof(Game), "OnLevelLoadComplete");

        static readonly FieldInfo enemycount = Helpers.Field(typeof(EnemyWave), "enemiesRemaining");
        static readonly FieldInfo enemydict = Helpers.Field(typeof(EnemyWave), "enemyDict");
        static readonly FieldInfo spawnerList = Helpers.Field(typeof(EnemyWave), "_enemySpawner");

        static readonly FieldInfo ghostTriggers = Helpers.Field(typeof(GhostPlayback), "m_ghostTriggers");
        static readonly FieldInfo hintActive = Helpers.Field(typeof(GhostHintOriginVFX), "_activated");
        static readonly MethodInfo hintSetActive = Helpers.Method(typeof(GhostHintOriginVFX), "SetActivated");

        static readonly FieldInfo bossTimers = Helpers.Field(typeof(BossEncounter), "_stateTimers");
        static readonly FieldInfo bossState = Helpers.Field(typeof(BossEncounter), "_currentState");
        static readonly FieldInfo bossPlaying = Helpers.Field(typeof(BossEncounter), "_isPlaying");
        static readonly MethodInfo bossTransition = Helpers.Method(typeof(BossEncounter), "Transition");
        static readonly FieldInfo bossIntroFX = Helpers.Field(typeof(BossEncounter), "_playedIntroTeleportFX");

        static readonly FieldInfo bossCList = Helpers.Field(typeof(BossEncounter), "_crystals");
        //static readonly FieldInfo bossCState = Helpers.Field(typeof(BossEncounter), "_currentState");
        //static readonly FieldInfo bossCPlaying = Helpers.Field(typeof(BossEncounter), "_isPlaying");
        //static readonly FieldInfo bossCTimers = Helpers.Field(typeof(BossEncounter), "_stateTimers");
        //static readonly FieldInfo bossCState = Helpers.Field(typeof(BossEncounter), "_currentState");
        //static readonly FieldInfo bossCPlaying = Helpers.Field(typeof(BossEncounter), "_isPlaying");

        static readonly FieldInfo ltrigTriggered = Helpers.Field(typeof(LevelTrigger), "_triggered");

        static IEnumerator QuickLevelSetup(Game game, LevelData level, bool staging = true)
        {
            AsyncOperation cop = null;
            if (restartCountRC == 0)
                cop = UnityEngine.Resources.UnloadUnusedAssets();
            RM.time.SetTargetTimescale(0, true);

            RM.Pointer.Visible = NeonLite.DEBUG;
            RM.acceptInput = false;
            RM.acceptInputPauseMenu = false;

            SceneManager.UnloadSceneAsync("Player");
            yield return new WaitForEndOfFrame();
            if (restartCountGC == 0)
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false);

            yield return null;
            var op = SceneManager.LoadSceneAsync("Player", LoadSceneMode.Additive);
            op.allowSceneActivation = false;

            int target = !string.IsNullOrEmpty(level.environmentSceneAlt?.SceneName) ? 1 : 0;
            if (target != activeScene)
            {
                Helpers.StartProfiling("SetActiveScene");
                game.SetActiveScene(target);
                Helpers.EndProfiling();
            }

            game.SetWaitForStaging(staging);

            foreach (var tripwire in InRegistry<TripwireWeapon>(true).ProfileLoop("Tripwire Cancels"))
                tripwire.CancelTripRoutine();
            foreach (var beam in InRegistry<BeamWeapon>(true).ProfileLoop("Beam Weapon Cancels"))
                beam.CancelBeamTrackingRoutine();

            var audioObjects = AudioController.GetPlayingAudioObjects(true);
            foreach (var audio in audioObjects.ProfileLoop("Audio Object Stops"))
            {
                if (audio.transform.parent != MainMenu.Instance().transform)
                    audio.Stop(0);
            }
            AudioObjectSplineMover.ReleaseAudioObjects();

            foreach (var ghost in InRegistry<GhostPlayback>(true, false).ProfileLoop("GhostPlayback Resets"))
            {
                if (!ghost || ghost.gameObject.scene.name == "Player")
                    GhostPlaybackLord.i.ghostPlaybacks.Remove(ghost);
                else if (ghostTriggers.GetValue(ghost) != null)
                    ghost.ResetTimer();
            }

            //yield return null;

            foreach (var encounter in InRegistry<EnemyEncounter>().ProfileLoop("Enemy Encounter Stops"))
                encounter.StopAllCoroutines();

            foreach (var spawner in InRegistry<CardPickupSpawner>().ProfileLoop("Card Pickup Stops"))
                spawner.StopAllCoroutines();
            foreach (var spawner in InRegistry<ObjectSpawner>().ProfileLoop("Object Spawner Stops"))
                spawner.StopAllCoroutines();
            foreach (var spawner in InRegistry<EnemySpawner>(true).ProfileLoop("Enemy Spawner Stops"))
                spawner.StopAllCoroutines();
            foreach (var spawner in InRegistry<EnemyWaveSpecificObject>(true).ProfileLoop("Wave Specific Object Disables"))
            {
                if (spawner.holder)
                    spawner.holder.SetActive(false);
            }

            foreach (var obj in destroy.ProfileLoop("Destroys"))
            {
                if (obj)
                {
                    foreach (var mat in obj.GetComponentsInParent<Renderer>().SelectMany(x => x.materials))
                        Object.Destroy(mat);
                    Object.Destroy(obj);
                }
            }
            foreach (var particle in InRegistry<ParticleSystem>(true).ProfileLoop("Particle Stops"))
            {
                if (!particle.main.loop)
                {
                    particle.Stop();
                    particle.Clear();
                }
            }
            foreach (var hint in InRegistry<GhostHintOriginVFX>(true).ProfileLoop("Hint Resets"))
            {
                if (hintActive.GetValue<bool>(hint))
                {
                    hintSetActive.Invoke(hint, [false]);
                    AudioController.Stop("HINT_RESET", 0);
                }
            }

            Helpers.StartProfiling("Boss Encounter");
            if (RM.bossEncounter)
            {
                bossCList.GetValue<IList>(RM.bossEncounter).Clear();
                bossIntroFX.SetValue(RM.bossEncounter, false);
                bossPlaying.SetValue(RM.bossEncounter, false);
                bossState.SetValue(RM.bossEncounter, 0);
                bossTransition.Invoke(RM.bossEncounter, [0]);
                var timedict = bossTimers.GetValue<Dictionary<BossEncounter.State, float>>(RM.bossEncounter);
                foreach (var k in timedict.Keys.ToArray())
                    timedict[k] = 0;
            }
            Helpers.EndProfiling();

            foreach (var remember in InRegistry<RememberTransform>(true).ProfileLoop("Apply RememberTransform"))
                remember.Apply();

            foreach (var region in InRegistry<OnRegion>().ProfileLoop("Handle OnRegions"))
            {
                region.enabled = false;
                for (int i = 0; i < region.monoBehavioursToEnable.Length; ++i)
                {
                    var comp = region.monoBehavioursToEnable[i];
                    comp.enabled = false;
                    if (comp is ISkipRSS)
                    {
                        if (comp is RestartStart rs)
                        {
                            HandleSpecialComponent(rs.behaviour);
                            rs.behaviour.enabled = false;
                        }
                        continue;
                    }
                    HandleSpecialComponent(comp);

                    var rss = comp.GetComponents<RestartStart>().FirstOrDefault(x => x.behaviour == comp) ?? comp.gameObject.AddComponent<RestartStart>().Setup(comp);
                    region.monoBehavioursToEnable[i] = rss;
                }
                foreach (var obj in region.objectsToEnable)
                {
                    obj.SetActive(false);
                    foreach (var comp in obj.GetComponents<MonoBehaviour>())
                    {
                        if (comp is ISkipRSS)
                        {
                            if (comp is RestartStart rs)
                            {
                                HandleSpecialComponent(rs.behaviour);
                                rs.behaviour.enabled = false;
                            }
                            continue;
                        }

                        comp.enabled = false;
                        HandleSpecialComponent(comp);
                        var rss = comp.GetComponents<RestartStart>().FirstOrDefault(x => x.behaviour == comp) ?? comp.gameObject.AddComponent<RestartStart>().Setup(comp, true);
                    }
                }
            }

            foreach (var trigger in InRegistry<LevelTrigger>(true).ProfileLoop("Reset LevelTriggers"))
            {
                if (trigger.triggerAction == LevelTrigger.TriggerAction.EnableObjects)
                    foreach (var obj in trigger.gameObjects)
                        obj?.SetActive(false);
                else if (trigger.triggerAction == LevelTrigger.TriggerAction.DisableObjects)
                    foreach (var obj in trigger.gameObjects)
                        obj?.SetActive(true);
                ltrigTriggered.SetValue(trigger, false);
            }

            ObjectManager.Instance.Reset();
            Utils.ClearPreloadedObjectsFromResources();
            destroy.Clear();
            RM.time.SetTargetTimescale(1, true);

            if (restartCountGC == 0)
                GC.WaitForPendingFinalizers();

            op.allowSceneActivation = true;
            while (!op.isDone)
                yield return null;

            Helpers.StartProfiling($"Handle Reserves - {reserveList.Count}");
            var s = SceneManager.GetActiveScene();
            var cur = s;

            for (int i = 0; i < reserveList.Count; ++i)
            {
                var reserved = reserveList[i];
                var rScene = reserved.reserved.gameObject.scene;
                if (cur != rScene)
                {
                    SceneManager.SetActiveScene(rScene);
                    cur = rScene;
                }

                reserved.newObj = Object.Instantiate(reserved.reserved, reserved.parent);
                reserved.newObj.name = reserved.name;
                reserved.newObj.transform.SetSiblingIndex(reserved.sibling);
                destroy.Add(reserved.newObj);
                reserveList[i] = reserved;
            }
            SceneManager.SetActiveScene(s);
            Helpers.EndProfiling();
            foreach (var region in InRegistry<OnRegion>(true))
            {
                AddToRegistry(region);
                region.enabled = true;
            }
            foreach (var spawner in InRegistry<CardPickupSpawner>(true).ProfileLoop("Respawn Cards"))
            {
                if (spawner.spawnOnStart)
                    spawner.SpawnCard();
            }
            foreach (var spawner in InRegistry<ObjectSpawner>(true).ProfileLoop("Respawn ObjectSpawners"))
            {
                if (spawner.spawnOnStart)
                    spawner.Spawn();
            }

            foreach (var wave in InRegistry<EnemyWave>(true).ProfileLoop("Reset EnemyWaves"))
            {
                enemydict.GetValue<IDictionary>(wave).Clear();
                enemycount.SetValue(wave, 0);
            }

            foreach (var encounter in InRegistry<EnemyEncounter>(true).ProfileLoop("Reset Encounter"))
                encounter.Setup();

            RM.time.SetTargetTimescale(0, true);
            var playthru = currentPlaythrough.GetValue<LevelPlaythrough>(game);
            playthru.Reset();
            if (LevelRush.IsLevelRush())
                playthru.SetLevelRushTimeMicroseconds(LevelRush.GetCurrentLevelRushTimerMicroseconds());

            Object.FindObjectOfType<Setup>().ApplyHeightFogMat();
            // do this b4 so it doesn't feel "laggy"
            var onLoad = onlvlload.GetValue<MulticastDelegate>(game);

            do yield return new WaitForEndOfFrame();
            while (!cop?.isDone ?? false);

            foreach (var gate in InRegistry<LevelGate>(true).ProfileLoop("Gate Particles"))
            {
                AddToRegistry(gate);
                if (!gate.Unlocked)
                {
                    gate.teleportParticles.Stop();
                    gate.teleportParticles.Clear();
                }
            } // this is super late but come on

            yield return LoadManager.HandleLoads();
            RM.Pointer.Visible = staging;
            if (LoadingScreenshot.i)
                LoadingScreenshot.i.Stop();

            if (staging)
            {
                MainMenu.Instance().SetState(MainMenu.State.Staging, true, true, true, false);
                while (MainMenu.Instance().GetCurrentState() != MainMenu.State.Staging || waitForStaging.GetValue<bool>(game))
                    yield return null;
            }
            //else
            //    RM.drifter.SetWaitForJumpRelease(true);

            yield return RM.mechController.ForceSetup();

            AudioController.Play("MECH_ENTER");

            RM.time.SetTargetTimescale(1, true);
            MainMenu.Instance().SetState(MainMenu.State.None, true, true, true, false);
            RM.acceptInput = true;
            RM.acceptInputPauseMenu = true;
            foreach (Delegate dlg in onLoad?.GetInvocationList() ?? [])
                dlg.DynamicInvoke();
        }

        static readonly MethodInfo drifterStart = Helpers.Method(typeof(FirstPersonDrifter), "Start");

        static void ResetPlayer()
        {
            RM.ui.Setup();
            drifterStart.Invoke(RM.drifter, []);
            RM.drifter._zipline.SetZipline(false, false, Vector3.zero, Vector3.zero);
            RM.drifter.CancelTelefrag();
            RM.drifter.OnPlayerDie();
        }

        static readonly FieldInfo flattenLoop = Helpers.Field(typeof(DFlattener), "m_loop");

        static readonly FieldInfo moveScaledT = Helpers.Field(typeof(MoveTransform), "m_t");
        static readonly FieldInfo moveTime = Helpers.Field(typeof(MoveTransform), "m_timer");
        static readonly FieldInfo moveLoop = Helpers.Field(typeof(MoveTransform), "m_loop");

        static void HandleSpecialComponent(MonoBehaviour comp)
        {
            if (comp is DFlattener flattener)
            {
                var loop = (AudioObject)flattenLoop.GetValue(flattener);
                if (loop)
                    loop.Stop(); // null prop doesn't work???
            }
            else if (comp is MoveTransform move)
            {
                if (rememberSpeed.ContainsKey(move))
                    move.speed = rememberSpeed[move];
                moveScaledT.SetValue(move, 0);
                moveTime.SetValue(move, 0);
                var loop = (AudioObject)moveLoop.GetValue(move);
                if (loop)
                    loop.Stop();
            }
        }


        static readonly MethodInfo loadadd = Helpers.Method(typeof(Game), "LoadSceneAdditive");

        static IEnumerator TrimmedLevelSetup(Game game, LevelData level)
        {
            Singleton<EnvironmentManager>.Instance.OnLoadLevel();
            AudioObjectSplineMover.ReleaseAudioObjects();

            yield return game.StartCoroutine(game.LoadScene(level.levelScene.SceneName, false, true, false, null, false, true));

            yield return game.StartCoroutine((IEnumerator)loadadd.Invoke(game, [level.environmentScene.SceneName, false, false, 0f, true]));
            Object.FindObjectOfType<Setup>()?.SetSceneContents(true);
            DirectionalLightOverride directionalLightOverride = Object.FindObjectOfType<DirectionalLightOverride>();
            if (directionalLightOverride != null)
            {
                directionalLightOverride.SunLight._canonicalRotation = directionalLightOverride.transform.rotation;
                directionalLightOverride.SunLight.SetCanonicalRotation();
            }
            if (level.environmentSceneAlt != null && level.environmentSceneAlt.SceneName != "")
            {
                yield return game.StartCoroutine((IEnumerator)loadadd.Invoke(game, [level.environmentSceneAlt.SceneName, false, false, 0f, true]));
                game.SetActiveScene(1);
            }

            ObjectManager.Instance.Reset();

            yield return game.StartCoroutine((IEnumerator)loadadd.Invoke(game, ["Player", true, false, 0f, true]));

            //SceneManager.UnloadSceneAsync("Player");
            //var op = SceneManager.LoadSceneAsync("Player", LoadSceneMode.Additive);
            //do yield return null;
            //while (!op.isDone);

            yield return RM.mechController.ForceSetup();
            AudioController.Play("MECH_ENTER");
            RM.time.SetTargetTimescale(1f, true);
            MainMenu.Instance().SetState(MainMenu.State.None, true, true, true, false);
        }

        static bool forceStaging = false;
        public static void ForceStagingNextRestart() => forceStaging = true;

        static bool StagingHappens() => forceStaging || !noStaging.Value || LevelRush.IsLevelRush();
        static bool mmUpdating = false;
        static void PreMMUpdate() => mmUpdating = true;
        static void PostMMUpdate() => mmUpdating = false;
        static bool MMPausing(MainMenu __instance, bool setPause)
        {
            if (pauseStaging.Value && setPause && mmUpdating && !StagingHappens() && __instance.GetCurrentState() == MainMenu.State.None)
            {
                RM.mechController.Die(true, true);
                return false;
            }
            return true;
        }
    }

    interface ISkipRSS { };

    class RestartStart : MonoBehaviour, ISkipRSS
    {
        public MonoBehaviour behaviour;
        public Type behaviorType;

        void OnEnable()
        {
            if (!behaviour)
                return;
            Helpers.Method(behaviorType, "Start").Invoke(behaviour, []);
            behaviour.enabled = true;
        }
        void OnDisable() => behaviour.enabled = false;
        public RestartStart Setup(MonoBehaviour behaviour, bool enable = false)
        {
            this.behaviour = behaviour;
            behaviorType = behaviour.GetType();
            enabled = enable;
            return this;
        }
    }

    [DefaultExecutionOrder(-1000)]
    class RememberTransform : MonoBehaviour, ISkipRSS
    {
        public bool remembered;
        public Vector3 position;
        public Quaternion rotation;

        void Start()
        {
            if (remembered)
                return;
            remembered = true;
            position = transform.position;
            rotation = transform.rotation;
        }

        public void Apply()
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }

    class LoadingScreenshot : MonoBehaviour, IModule
    {
        const bool priority = false;
        internal static bool active = true;
        static bool tried = false;

        static GameObject prefab;

        internal static LoadingScreenshot i;

        static void Setup()
        {
            NeonLite.OnBundleLoad += bundle =>
            {
                prefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/LoadingRawImage.prefab");
                if (tried)
                    Activate(true);
            };
        }

        internal static void Activate(bool activate)
        {
            if (activate)
            {
                tried = true;
                if (prefab)
                    Utils.InstantiateUI(prefab, "Loading Screenshot", MainMenu.Instance()._screenLoading.background.transform).AddComponent<LoadingScreenshot>();
            }
            else
            {
                i.Stop();
                Destroy(i.gameObject);
            }
        }

        RawImage rawImage;
        Resolution res;
        RenderTexture renderTexture;
        Texture2D texture;

        Image bg;
        bool inUse = false;

        void Awake()
        {
            i = this;
            rawImage = GetComponent<RawImage>();
            rawImage.color = rawImage.color.Alpha(0);
            SetResolution(Screen.currentResolution);
            bg = transform.parent.GetComponent<Image>();
        }

        void SetResolution(Resolution resolution)
        {
            res = resolution;
            Destroy(renderTexture);
            Destroy(texture);
            renderTexture = new(res.width, res.height, 1, RenderTextureFormat.ARGB32);
            texture = new(res.width, res.height, TextureFormat.ARGB32, false);
            texture.Apply(false, true);
        }

        void Update()
        {
            var curRes = Screen.currentResolution;
            if (curRes.width != res.width || curRes.height != res.height)
                SetResolution(curRes);
        }

        internal void Screenshot()
        {
            inUse = true;

            var cam = Camera.main;
            var current = cam.targetTexture;

            cam.targetTexture = renderTexture;
            cam.Render();

            cam.targetTexture = current;

            Graphics.CopyTexture(renderTexture, texture);
            rawImage.texture = texture;

            rawImage.color = rawImage.color.Alpha(1);
        }

        internal void Stop()
        {
            inUse = false;
            rawImage.color = rawImage.color.Alpha(0);
        }
    }

}
