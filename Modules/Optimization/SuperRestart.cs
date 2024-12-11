using ClockStone;
using Guirao.UltimateTextDamage;
using HarmonyLib;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        internal static MelonPreferences_Entry<bool> setting;
        static MelonPreferences_Entry<bool> noStaging;
        static MelonPreferences_Entry<bool> pauseStaging;
        static MelonPreferences_Entry<int> gcTimer;
        static void Setup()
        {
            setting = Settings.Add(Settings.h, "Misc", "superRestart", "Quick Restart", "Completely overrides the level loading routine on restart to be faster.", true);
            noStaging = Settings.Add(Settings.h, "Misc", "noStaging", "Skip Staging Screen", "Skips the staging screen while using Quick Restart.", true);
            pauseStaging = Settings.Add(Settings.h, "Misc", "pauseStaging", "Pause Restarts to Staging", null, true);
            pauseStaging.IsHidden = true;
            gcTimer = Settings.Add(Settings.h, "Misc", "gcTimer", "Restarts to call GC", null, 100);
            gcTimer.IsHidden = true;
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;

            var useScreenshot = Settings.Add(Settings.h, "Misc", "useScreenshot", "Minimize Flashing", "Use a screenshot of the stage to minimize flashing.", true);
            useScreenshot.OnEntryValueChanged.Subscribe((_, after) => LoadingScreenshot.Activate(after));
            LoadingScreenshot.active = useScreenshot.Value;
        }

        static readonly MethodInfo oglvlsetup = AccessTools.Method(typeof(Game), "LevelSetupRoutine");
        static readonly MethodInfo ogsetact = AccessTools.Method(typeof(Game), "SetActiveScene");
        static readonly MethodInfo ogpthrukill = AccessTools.Method(typeof(LevelPlaythrough), "OnEnemyKill");

        static readonly MethodInfo ogobjmspwn = AccessTools.Method(typeof(ObjectSpawner), "SpawnObject");
        static readonly MethodInfo ogenemyspwn = AccessTools.Method(typeof(EnemySpawner), "InstantiateEnemy");
        static readonly MethodInfo ogobjpspwn = AccessTools.Method(typeof(ObjectPool), "Spawn");
        static readonly MethodInfo ogcardspwnc = AccessTools.Method(typeof(CardPickupSpawner), "ProcessObject");
        static readonly MethodInfo ogcardspwn = AccessTools.Method(typeof(CardPickup), "Spawn");
        static readonly MethodInfo ogtripdie = AccessTools.Method(typeof(EnemyTripwire), "Die");
        static readonly MethodInfo ogmimicatk = AccessTools.Method(typeof(EnemyMimic), "Attack");
        static readonly MethodInfo ogfracexpl = AccessTools.Method(typeof(BreakableFractureFX), "Explode");
        static readonly MethodInfo ogdmgtxts = AccessTools.Method(typeof(UltimateTextDamageManager), "Start");
        static readonly MethodInfo ogghplstrt = AccessTools.Method(typeof(GhostPlayback), "Start");
        static readonly MethodInfo ogutprlres = AccessTools.Method(typeof(Utils), "PreloadFromResources");

        static readonly MethodInfo ogmenuload = AccessTools.Method(typeof(MenuScreenLoading), "LoadScene");

        static readonly MethodInfo ogobjpdspwn = AccessTools.Method(typeof(ObjectPool), "Despawn");

        static readonly MethodInfo ogmmupd = AccessTools.Method(typeof(MainMenu), "Update");
        static readonly MethodInfo ogmmpause = AccessTools.Method(typeof(MainMenu), "PauseGame");

        static readonly List<Tuple<Type, string, Type[]>> prefixToRegister = [
            new(typeof(OnRegion), "Start", null),
            new(typeof(EnemyEncounter), "Setup", null),
            new(typeof(LevelGate), "Start", null),
            new(typeof(ParticleSystem), "Play", null),
            new(typeof(GhostHintOriginVFX), "OnTriggerEnter", null),
            new(typeof(GhostPlayback), "ProcessTriggers", null),
            new(typeof(CardPickupSpawner), "SpawnCard", []),
            new(typeof(CardPickupSpawner), "SpawnCard", [typeof(float)]),
            new(typeof(BeamWeapon), "StartBeamTrackingRoutine", null),
            new(typeof(ObjectSpawner), "Spawn", []),
            new(typeof(ObjectSpawner), "Spawn", [typeof(float)]),
            new(typeof(EnemySpawner), "SpawnDelay", null),
            new(typeof(EnemyWaveSpecificObject), "Spawn", null),
            new(typeof(EnemyWave), "SpawnWave", null),
            new(typeof(RememberTransform), "Start", null),
            new(typeof(RememberTransform), "Apply", null),
            new(typeof(LevelTrigger), "OnTriggered", null),
            new(typeof(TripwireWeapon), "OnTripped", null),
        ];

        static void Activate(bool activate)
        {
            if (activate)
            {
                Patching.AddPatch(oglvlsetup, OverridePlayLevel, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogsetact, SetActiveScene, Patching.PatchTarget.Prefix);
                Patching.AddPatch(ogpthrukill, Never, Patching.PatchTarget.Prefix);

                Patching.AddPatch(ogobjmspwn, MarkForDestroy, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogenemyspwn, MarkForDestroyEnemy, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogobjpspwn, MarkForDestroy, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogcardspwnc, MarkForDestroyCard, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogcardspwn, MarkForDestroyCard2, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogtripdie, MarkForDestroyTripwire, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogmimicatk, MarkForDestroyMimic, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogfracexpl, MarkForDestroyFracture, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogdmgtxts, MarkForDestroyDamageText, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogghplstrt, MarkForDestroyGhost, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogutprlres, MarkForDestroyPreload, Patching.PatchTarget.Transpiler);

                Patching.AddPatch(ogmenuload, PostMenuLoad, Patching.PatchTarget.Postfix);

                Patching.AddPatch(ogobjpdspwn, UnmarkForDestroyPool, Patching.PatchTarget.Postfix);

                Patching.AddPatch(ogmmupd, PreMMUpdate, Patching.PatchTarget.Prefix);
                Patching.AddPatch(ogmmupd, PostMMUpdate, Patching.PatchTarget.Postfix);
                Patching.AddPatch(ogmmpause, MMPausing, Patching.PatchTarget.Prefix);

                foreach ((var type, var name, var args) in prefixToRegister)
                {
                    if (!registry.ContainsKey(type))
                        registry[type] = new(new(30), new(30));

                    var func = AccessTools.Method(type, name, args);
                    var manual = AccessTools.Method(typeof(SuperRestart), "AddToRegistry", generics: [type]);
                    Patching.AddPatch(func, manual.ToNewHarmonyMethod(), Patching.PatchTarget.Prefix);
                }
            }
            else
            {
                destroy.Clear();
                reserveList.Clear();
                forceStaging = false;
                ClearRegistry();
                if (LoadingScreenshot.i)
                    LoadingScreenshot.i.Stop();

                GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;

                Patching.RemovePatch(oglvlsetup, OverridePlayLevel);
                Patching.RemovePatch(ogsetact, SetActiveScene);
                Patching.RemovePatch(ogpthrukill, Never);
                Patching.RemovePatch(ogobjmspwn, MarkForDestroy);
                Patching.RemovePatch(ogenemyspwn, MarkForDestroyEnemy);
                Patching.RemovePatch(ogobjpspwn, MarkForDestroy);
                Patching.RemovePatch(ogcardspwnc, MarkForDestroyCard);
                Patching.RemovePatch(ogcardspwn, MarkForDestroyCard2);
                Patching.RemovePatch(ogtripdie, MarkForDestroyTripwire);
                Patching.RemovePatch(ogmimicatk, MarkForDestroyMimic);
                Patching.RemovePatch(ogfracexpl, MarkForDestroyFracture);
                Patching.RemovePatch(ogdmgtxts, MarkForDestroyDamageText);
                Patching.RemovePatch(ogghplstrt, MarkForDestroyGhost);
                Patching.RemovePatch(ogutprlres, MarkForDestroyPreload);
                Patching.RemovePatch(ogmenuload, PostMenuLoad);
                Patching.RemovePatch(ogobjpdspwn, UnmarkForDestroyPool);
                Patching.RemovePatch(ogmmupd, PreMMUpdate);
                Patching.RemovePatch(ogmmupd, PostMMUpdate);
                Patching.RemovePatch(ogmmpause, MMPausing);

                foreach ((var type, var name, var args) in prefixToRegister)
                {
                    var func = AccessTools.Method(type, name, args);
                    var manual = AccessTools.Method(typeof(SuperRestart), "AddToRegistry", generics: [type]);

                    Patching.RemovePatch(func, manual);
                }
            }

            active = activate;
        }

        static bool Never() => false;

        static readonly HashSet<string> blacklisted = [
            "HUB_HEAVEN",
            "TUT_ORIGIN"
        ];


        static int activeScene;
        static int restartCount;
        static IEnumerator OverridePlayLevel(IEnumerator __result, Game __instance, LevelData newLevel, LevelData ____currentLevel)
        {
            if (newLevel == ____currentLevel && destroy.Count > 0 && !blacklisted.Contains(newLevel.levelID))
            {
                if (LoadingScreenshot.i)
                    LoadingScreenshot.i.Screenshot();
                if (gcTimer.Value != -1 && ++restartCount >= gcTimer.Value)
                {
                    restartCount = 0;
                    GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
                }

                MainMenu.Instance().SetState(MainMenu.State.Loading, true, true, true, false);
                yield return QuickLevelSetup(__instance, newLevel, StagingHappens() || mmUpdating);

                if (restartCount == 0)
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

                activeScene = 0;
                restartCount = 0;
                destroy.Clear();
                reserveList.Clear();
                forceStaging = false;
                ClearRegistry();
                while (__result.MoveNext())
                    yield return __result.Current;

                if (newLevel && newLevel.type != LevelData.LevelType.Hub)
                    GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
            }
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
        static readonly FieldInfo tempObjs = AccessTools.Field(typeof(UltimateTextDamageManager), "m_tempObjects");
        static void MarkForDestroyDamageText(UltimateTextDamageManager __instance)
        {
            var objs = (List<GameObject>)tempObjs.GetValue(__instance);
            foreach (var obj in objs)
            {
                obj.name = "UltimateTextDamageManager Temp";
                destroy.Add(obj);
            }
        }
        static readonly FieldInfo ghostObject = AccessTools.Field(typeof(GhostPlayback), "m_ghostObject");
        static readonly FieldInfo cloneBullet = AccessTools.Field(typeof(GhostPlayback), "m_cloneBullet");
        static void MarkForDestroyGhost(GhostPlayback __instance)
        {
            if (__instance.ghostType != GhostUtils.GhostType.PersonalGhost)
                return;
            destroy.Add((GameObject)ghostObject.GetValue(__instance));
            destroy.Add(((GhostBullet)cloneBullet.GetValue(__instance)).gameObject);
        }

        static IEnumerable<CodeInstruction> MarkForDestroyPreload(IEnumerable<CodeInstruction> instructions)
        {
            var enqueue = AccessTools.Method(typeof(Queue<>).MakeGenericType(typeof(GameObject)), "Enqueue");
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

        struct ReserveInfo
        {
            public Transform parent;
            public int sibling;

            public GameObject reserved;
            public string name;
            public GameObject newObj;
        }

        static readonly List<ReserveInfo> reserveList = [];
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
            if (!LoadManager.currentLevel || blacklisted.Contains(Singleton<Game>.Instance.GetCurrentLevel().levelID))
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

        static readonly Dictionary<Type, Tuple<List<Object>, HashSet<Object>>> registry = [];
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


        static readonly FieldInfo waitForStaging = AccessTools.Field(typeof(Game), "_waitForStaging");
        static readonly FieldInfo currentPlaythrough = AccessTools.Field(typeof(Game), "_currentPlaythrough");
        static readonly FieldInfo onlvlload = AccessTools.Field(typeof(Game), "OnLevelLoadComplete");

        static readonly FieldInfo enemycount = AccessTools.Field(typeof(EnemyWave), "enemiesRemaining");
        static readonly FieldInfo enemydict = AccessTools.Field(typeof(EnemyWave), "enemyDict");
        static readonly FieldInfo spawnerList = AccessTools.Field(typeof(EnemyWave), "_enemySpawner");

        static readonly FieldInfo hintActive = AccessTools.Field(typeof(GhostHintOriginVFX), "_activated");
        static readonly MethodInfo hintSetActive = AccessTools.Method(typeof(GhostHintOriginVFX), "SetActivated");

        static readonly FieldInfo bossTimers = AccessTools.Field(typeof(BossEncounter), "_stateTimers");
        static readonly FieldInfo bossState = AccessTools.Field(typeof(BossEncounter), "_currentState");
        static readonly FieldInfo bossPlaying = AccessTools.Field(typeof(BossEncounter), "_isPlaying");
        static readonly MethodInfo bossTransition = AccessTools.Method(typeof(BossEncounter), "Transition");
        static readonly FieldInfo bossIntroFX = AccessTools.Field(typeof(BossEncounter), "_playedIntroTeleportFX");

        static readonly FieldInfo bossCList = AccessTools.Field(typeof(BossEncounter), "_crystals");
        //static readonly FieldInfo bossCState = AccessTools.Field(typeof(BossEncounter), "_currentState");
        //static readonly FieldInfo bossCPlaying = AccessTools.Field(typeof(BossEncounter), "_isPlaying");
        //static readonly FieldInfo bossCTimers = AccessTools.Field(typeof(BossEncounter), "_stateTimers");
        //static readonly FieldInfo bossCState = AccessTools.Field(typeof(BossEncounter), "_currentState");
        //static readonly FieldInfo bossCPlaying = AccessTools.Field(typeof(BossEncounter), "_isPlaying");

        static readonly FieldInfo ltrigTriggered = AccessTools.Field(typeof(LevelTrigger), "_triggered");

        static IEnumerator QuickLevelSetup(Game game, LevelData level, bool staging = true)
        {
            RM.time.SetTargetTimescale(0, true);

            RM.Pointer.Visible = false;
            RM.acceptInput = false;
            RM.acceptInputPauseMenu = false;

            int target = !string.IsNullOrEmpty(level.environmentSceneAlt?.SceneName) ? 1 : 0;
            if (target != activeScene)
            {
                Helpers.StartProfiling("SetActiveScene");
                game.SetActiveScene(target);
                Helpers.EndProfiling();
            }

            game.SetWaitForStaging(staging);
            var playthru = (LevelPlaythrough)currentPlaythrough.GetValue(game);
            playthru.Reset();

            foreach (var tripwire in InRegistry<TripwireWeapon>(true).ProfileLoop("Tripwire Cancels"))
                TripwireWeapon.CancelTripRoutines();
            foreach (var beam in InRegistry<BeamWeapon>(true).ProfileLoop("Beam Weapon Cancels"))
                beam.CancelBeamTrackingRoutine();

            var audioObjects = AudioController.GetPlayingAudioObjects(true);
            foreach (var audio in audioObjects.ProfileLoop("Audio Object Stops"))
            {
                if (audio.transform.parent != MainMenu.Instance().transform)
                    audio.Stop(0);
            }
            AudioObjectSplineMover.ReleaseAudioObjects();

            foreach (var ghost in InRegistry<GhostPlayback>(true).ProfileLoop("GhostPlayback Resets"))
            {
                if (ghost.gameObject.scene.name == "Player")
                    GhostPlaybackLord.i.ghostPlaybacks.Remove(ghost);
                else
                    ghost.ResetTimer();
            }

            SceneManager.UnloadSceneAsync("Player");
            var op = SceneManager.LoadSceneAsync("Player", LoadSceneMode.Additive);

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
                    Object.Destroy(obj);
            }
            foreach (var particle in InRegistry<ParticleSystem>(true).ProfileLoop("Particle Stops"))
            {
                if (!particle.main.loop)
                    particle.Stop();
            }
            foreach (var hint in InRegistry<GhostHintOriginVFX>(true).ProfileLoop("Hint Resets"))
            {
                if ((bool)hintActive.GetValue(hint))
                {
                    hintSetActive.Invoke(hint, [false]);
                    AudioController.Stop("HINT_RESET", 0);
                }
            }

            Helpers.StartProfiling("Boss Encounter");
            if (RM.bossEncounter)
            {
                ((IList)bossCList.GetValue(RM.bossEncounter)).Clear();
                bossIntroFX.SetValue(RM.bossEncounter, false);
                bossPlaying.SetValue(RM.bossEncounter, false);
                bossState.SetValue(RM.bossEncounter, 0);
                bossTransition.Invoke(RM.bossEncounter, [0]);
                var timedict = (Dictionary<BossEncounter.State, float>)bossTimers.GetValue(RM.bossEncounter);
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

            Utils.ClearPreloadedObjectsFromResources();
            destroy.Clear();
            RM.time.SetTargetTimescale(1, true);

            if (restartCount == 0)
                GC.Collect();
            while (!op.isDone)
            {
                //GarbageCollector.CollectIncremental(nanosecond);
                yield return null;
            }
            //ResetPlayer();

            playthru.Reset();

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
                enemydict.SetValue(wave, new Dictionary<Enemy, bool>());
                enemycount.SetValue(wave, 0);
            }

            foreach (var encounter in InRegistry<EnemyEncounter>(true).ProfileLoop("Reset Encounter"))
                encounter.Setup();

            RM.Pointer.Visible = staging;
            RM.time.SetTargetTimescale(0, true);

            Object.FindObjectOfType<Setup>().ApplyHeightFogMat();
            yield return null;

            foreach (var gate in InRegistry<LevelGate>(true).ProfileLoop("Gate Particles"))
            {
                AddToRegistry(gate);
                if (!gate.Unlocked)
                {
                    gate.teleportParticles.Stop();
                    gate.teleportParticles.Clear();
                }
            } // this is super late but come on

            yield return LoadManager.HandleLoads(game.GetCurrentLevel());
            if (staging)
            {
                MainMenu.Instance().SetState(MainMenu.State.Staging, true, true, true, false);
                while ((bool)waitForStaging.GetValue(game) || MainMenu.Instance().GetCurrentState() != MainMenu.State.Staging)
                {
                    //GarbageCollector.CollectIncremental(nanosecond);
                    yield return null;
                }
            }

            yield return RM.mechController.ForceSetup();

            AudioController.Play("MECH_ENTER");

            RM.time.SetTargetTimescale(1, true);
            MainMenu.Instance().SetState(MainMenu.State.None, true, true, true, false);
            playthru.Reset();
            if (LevelRush.IsLevelRush())
                playthru.SetLevelRushTimeMicroseconds(LevelRush.GetCurrentLevelRushTimerMicroseconds());
            RM.acceptInput = true;
            RM.acceptInputPauseMenu = true;
            var onLoad = (MulticastDelegate)onlvlload.GetValue(game);
            foreach (Delegate dlg in onLoad.GetInvocationList())
                dlg.DynamicInvoke();
        }

        static readonly MethodInfo drifterStart = AccessTools.Method(typeof(FirstPersonDrifter), "Start");

        static void ResetPlayer()
        {
            RM.ui.Setup();
            drifterStart.Invoke(RM.drifter, []);
            RM.drifter._zipline.SetZipline(false, false, Vector3.zero, Vector3.zero);
            RM.drifter.CancelTelefrag();
            RM.drifter.OnPlayerDie();
        }

        static readonly FieldInfo flattenLoop = AccessTools.Field(typeof(DFlattener), "m_loop");

        static readonly FieldInfo moveScaledT = AccessTools.Field(typeof(MoveTransform), "m_t");
        static readonly FieldInfo moveTime = AccessTools.Field(typeof(MoveTransform), "m_timer");
        static readonly FieldInfo moveLoop = AccessTools.Field(typeof(MoveTransform), "m_loop");

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
                moveScaledT.SetValue(move, 0);
                moveTime.SetValue(move, 0);
                var loop = (AudioObject)moveLoop.GetValue(move);
                if (loop)
                    loop.Stop();
            }
        }


        static readonly MethodInfo loadadd = AccessTools.Method(typeof(Game), "LoadSceneAdditive");

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
            AccessTools.Method(behaviorType, "Start").Invoke(behaviour, []);
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
