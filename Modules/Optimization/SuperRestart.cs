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
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NeonLite.Modules.Optimization
{
    internal class SuperRestart : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        public static MelonPreferences_Entry<bool> setting;
        static MelonPreferences_Entry<bool> noStaging;
        static MelonPreferences_Entry<bool> pauseStaging;

        static void Setup()
        {
            setting = Settings.Add(Settings.h, "Misc", "superRestart", "Quick Restart", "Completely overrides the level loading routine on restart to be faster.", true);
            noStaging = Settings.Add(Settings.h, "Misc", "noStaging", "Skip Staging Screen", "Skips the staging screen while using Quick Restart.", true);
            pauseStaging = Settings.Add(Settings.h, "Misc", "pauseStaging", "Pause Restarts to Staging", null, true);
            pauseStaging.IsHidden = true;
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo oglvlsetup = AccessTools.Method(typeof(Game), "LevelSetupRoutine");
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



        static void Activate(bool activate)
        {
            if (activate)
            {
                MultiLoad.setting.Value = false;
                NeonLite.Harmony.Patch(oglvlsetup, postfix: Helpers.HM(OverridePlayLevel));
                NeonLite.Harmony.Patch(ogpthrukill, prefix: Helpers.HM(Never));

                NeonLite.Harmony.Patch(ogobjmspwn, postfix: Helpers.HM(MarkForDestroy));
                NeonLite.Harmony.Patch(ogenemyspwn, postfix: Helpers.HM(MarkForDestroyEnemy));
                NeonLite.Harmony.Patch(ogobjpspwn, postfix: Helpers.HM(MarkForDestroy));
                NeonLite.Harmony.Patch(ogcardspwnc, postfix: Helpers.HM(MarkForDestroyCard));
                NeonLite.Harmony.Patch(ogcardspwn, postfix: Helpers.HM(MarkForDestroyCard2));
                NeonLite.Harmony.Patch(ogtripdie, postfix: Helpers.HM(MarkForDestroyTripwire));
                NeonLite.Harmony.Patch(ogmimicatk, postfix: Helpers.HM(MarkForDestroyMimic));
                NeonLite.Harmony.Patch(ogfracexpl, postfix: Helpers.HM(MarkForDestroyFracture));
                NeonLite.Harmony.Patch(ogdmgtxts, postfix: Helpers.HM(MarkForDestroyDamageText));
                NeonLite.Harmony.Patch(ogghplstrt, postfix: Helpers.HM(MarkForDestroyGhost));
                NeonLite.Harmony.Patch(ogutprlres, transpiler: Helpers.HM(MarkForDestroyPreload));

                NeonLite.Harmony.Patch(ogmenuload, postfix: Helpers.HM(PostMenuLoad));

                NeonLite.Harmony.Patch(ogobjpdspwn, postfix: Helpers.HM(UnmarkForDestroyPool));

                NeonLite.Harmony.Patch(ogmmupd, prefix: Helpers.HM(PreMMUpdate));
                NeonLite.Harmony.Patch(ogmmupd, postfix: Helpers.HM(PostMMUpdate));
                NeonLite.Harmony.Patch(ogmmpause, prefix: Helpers.HM(MMPausing));
            }
            else
            {
                destroy.Clear();
                reserveList.Clear();

                NeonLite.Harmony.Unpatch(oglvlsetup, Helpers.MI(OverridePlayLevel));
                NeonLite.Harmony.Unpatch(ogpthrukill, Helpers.MI(Never));

                NeonLite.Harmony.Unpatch(ogobjmspwn, Helpers.MI(MarkForDestroy));
                NeonLite.Harmony.Unpatch(ogenemyspwn, Helpers.MI(MarkForDestroyEnemy));
                NeonLite.Harmony.Unpatch(ogobjpspwn, Helpers.MI(MarkForDestroy));
                NeonLite.Harmony.Unpatch(ogcardspwnc, Helpers.MI(MarkForDestroyCard));
                NeonLite.Harmony.Unpatch(ogcardspwn, Helpers.MI(MarkForDestroyCard2));
                NeonLite.Harmony.Unpatch(ogtripdie, Helpers.MI(MarkForDestroyTripwire));
                NeonLite.Harmony.Unpatch(ogmimicatk, Helpers.MI(MarkForDestroyMimic));
                NeonLite.Harmony.Unpatch(ogfracexpl, Helpers.MI(MarkForDestroyFracture));
                NeonLite.Harmony.Unpatch(ogdmgtxts, Helpers.MI(MarkForDestroyDamageText));
                NeonLite.Harmony.Unpatch(ogghplstrt, Helpers.MI(MarkForDestroyGhost));

                NeonLite.Harmony.Unpatch(ogmenuload, Helpers.MI(PostMenuLoad));

                NeonLite.Harmony.Unpatch(ogobjpdspwn, Helpers.MI(UnmarkForDestroyPool));

                NeonLite.Harmony.Unpatch(ogmmupd, Helpers.MI(PreMMUpdate));
                NeonLite.Harmony.Unpatch(ogmmupd, Helpers.MI(PostMMUpdate));
                NeonLite.Harmony.Unpatch(ogmmpause, Helpers.MI(MMPausing));
            }

            active = activate;
        }

        static bool Never() => false;

        static IEnumerator OverridePlayLevel(IEnumerator __result, Game __instance, LevelData newLevel, LevelData ____currentLevel)
        {
            if (newLevel == ____currentLevel && destroy.Count > 0)
            {
                MainMenu.Instance().SetState(MainMenu.State.Loading, true, true, true, false);
                yield return QuickLevelSetup(__instance, newLevel, LevelRush.IsLevelRush() || mmUpdating || !noStaging.Value);
            }
            else
            {
                /*if (newLevel.type == LevelData.LevelType.Level)
                {
                    yield return TrimmedLevelSetup(__instance, newLevel);
                    yield break;
                }//*/

                destroy.Clear();
                reserveList.Clear();
                while (__result.MoveNext())
                    yield return __result.Current;
            }
        }

        static readonly HashSet<GameObject> destroy = [];
        static void MarkForDestroy(GameObject __result) => destroy.Add(__result);
        static void MarkForDestroyEnemy(GameObject ____enemyObj) => destroy.Add(____enemyObj);

        static void MarkForDestroyCard(GameObject obj) => destroy.Add(obj);
        static void MarkForDestroyCard2(ref CardPickup pickup)
        {
            if (pickup != null)
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
            public Component old;
            public Type componentType;
            public GameObject oldObj;

            public Transform parent;
            public int sibling;

            public GameObject reserved;
            public GameObject newObj;
        }

        static readonly List<ReserveInfo> reserveList = [];
        static readonly Type[] manualTypes = [];// [typeof(DFlattener), typeof(MoveTransform)];

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
            var s = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.GetSceneAt(SceneManager.sceneCount - 1));
            var cur = SceneManager.GetActiveScene();
            
            var reserve = new GameObject()
            {
                name = "Reserve"
            }.transform;
            reserve.gameObject.SetActive(false);

            foreach (var damageable in Object.FindObjectsOfType<BaseDamageable>(true))
            {
                if (damageable.gameObject.scene == cur && !ParentsAreMarked(damageable.gameObject))
                {
                    destroy.Add(damageable.gameObject);
                    var obj = Object.Instantiate(damageable.gameObject, reserve);
                    reserveList.Add(new ReserveInfo
                    {
                        old = damageable,
                        componentType = damageable.GetType(),
                        oldObj = damageable.gameObject,

                        parent = damageable.transform.parent,
                        sibling = damageable.transform.GetSiblingIndex(),

                        reserved = obj
                    });
                }
            }

            foreach (var type in manualTypes)
            {
                foreach (var comp in Object.FindObjectsOfType(type, true).Cast<Component>())
                {
                    if (!destroy.Contains(comp.gameObject))
                    {
                        destroy.Add(comp.gameObject);
                        var obj = Object.Instantiate(comp.gameObject, reserve);
                        reserveList.Add(new ReserveInfo
                        {
                            old = comp,
                            componentType = comp.GetType(),
                            oldObj = comp.gameObject,

                            parent = comp.transform.parent,
                            sibling = comp.transform.GetSiblingIndex(),

                            reserved = obj
                        });
                    }
                }
            }
            SceneManager.SetActiveScene(s);
        }


        static void UnmarkForDestroyPool(GameObject obj)
        {
            if (obj == null)
                destroy.Remove(obj);
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

        static IEnumerable<T> SearchInAllScenes<T>(bool includeInactive = false) where T : MonoBehaviour
        {
            foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll<T>())
            {
                if (obj != null && !(obj.hideFlags == HideFlags.NotEditable || obj.hideFlags == HideFlags.HideAndDontSave) && (includeInactive || obj.isActiveAndEnabled))
                    yield return obj;
            }
        }
        static IEnumerator QuickLevelSetup(Game game, LevelData level, bool staging = true)
        {
            RM.time.SetTargetTimescale(0, true);

            RM.Pointer.Visible = false;
            RM.acceptInput = false;
            RM.acceptInputPauseMenu = false;

            game.SetWaitForStaging(true);
            var playthru = (LevelPlaythrough)currentPlaythrough.GetValue(game);
            playthru.Reset();

            TripwireWeapon.CancelTripRoutines();
            foreach (var beam in SearchInAllScenes<BeamWeapon>())
                beam.CancelBeamTrackingRoutine();

            var audioObjects = AudioController.GetPlayingAudioObjects(true);
            foreach (var audio in audioObjects)
            {
                if (audio.transform.parent != MainMenu.Instance().transform)
                    audio.Stop(0);
            }
            AudioObjectSplineMover.ReleaseAudioObjects();

            yield return null;

            foreach (var spawner in SearchInAllScenes<CardPickupSpawner>())
                spawner.StopAllCoroutines();
            foreach (var spawner in SearchInAllScenes<ObjectSpawner>())
                spawner.StopAllCoroutines();
            foreach (var spawner in SearchInAllScenes<EnemySpawner>())
                spawner.StopAllCoroutines();
            foreach (var spawner in SearchInAllScenes<EnemyWaveSpecificObject>())
            {
                if (spawner.holder)
                    spawner.holder.SetActive(false);
            }
            foreach (var wave in SearchInAllScenes<EnemyWave>())
            {
                enemydict.SetValue(wave, new Dictionary<Enemy, bool>());
                enemycount.SetValue(wave, 0);
            }
            foreach (var obj in destroy)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }
            foreach (var particle in Object.FindObjectsOfType<ParticleSystem>())
            {
                if (!particle.main.loop)
                    particle.Stop();
            }
            foreach (var hint in SearchInAllScenes<GhostHintOriginVFX>())
            {
                if ((bool)hintActive.GetValue(hint))
                {
                    hintSetActive.Invoke(hint, [false]);
                    AudioController.Stop("HINT_RESET", 0);
                }
            }
            foreach (var ghost in SearchInAllScenes<GhostPlayback>())
            {
                if (ghost.gameObject.scene.name == "Player")
                    GhostPlaybackLord.i.ghostPlaybacks.Remove(ghost);
                else
                    ghost.ResetTimer();
            }


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


            foreach (var region in SearchInAllScenes<OnRegion>())
            {
                region.enabled = false;
                for (int i = 0; i < region.monoBehavioursToEnable.Length; ++i)
                {
                    var comp = region.monoBehavioursToEnable[i];
                    comp.enabled = false;
                    if (comp is RestartStart rs)
                    {
                        HandleSpecialComponent(rs.behaviour);
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
                        if (comp is RestartStart rs)
                        {
                            HandleSpecialComponent(rs.behaviour);
                            continue;
                        }

                        comp.enabled = false;
                        HandleSpecialComponent(comp);
                        var rss = comp.GetComponents<RestartStart>().FirstOrDefault(x => x.behaviour == comp) ?? comp.gameObject.AddComponent<RestartStart>().Setup(comp, true);
                    }
                }
            }

            foreach (var trigger in SearchInAllScenes<LevelTrigger>())
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
            game.SetActiveScene(!string.IsNullOrEmpty(level.environmentSceneAlt?.SceneName) ? 1 : 0);

            SceneManager.UnloadSceneAsync("Player");
            var op = SceneManager.LoadSceneAsync("Player", LoadSceneMode.Additive);
            do yield return null;
            while (!op.isDone);
            playthru.Reset();

            for (int i = 0; i < reserveList.Count; ++i)
            {
                var reserved = reserveList[i];
                reserved.newObj = Object.Instantiate(reserved.reserved, reserved.parent);
                reserved.newObj.transform.SetSiblingIndex(reserved.sibling);
                destroy.Add(reserved.newObj);
                reserveList[i] = reserved;
            }
            foreach (var region in Object.FindObjectsOfType<OnRegion>(true))
                region.enabled = true;
            foreach (var spawner in Object.FindObjectsOfType<CardPickupSpawner>())
            {
                if (spawner.spawnOnStart)
                    spawner.SpawnCard();
            }
            foreach (var spawner in Object.FindObjectsOfType<ObjectSpawner>())
            {
                if (spawner.spawnOnStart)
                    spawner.Spawn();
            }

            foreach (var encounter in Object.FindObjectsOfType<EnemyEncounter>())
                encounter.Setup();

            RM.Pointer.Visible = staging;
            RM.time.SetTargetTimescale(0, true);

            Object.FindObjectOfType<Setup>().ApplyHeightFogMat();
            yield return null;
            LoadManager.HandleLoads(game.GetCurrentLevel());
            if (staging)
            {
                MainMenu.Instance().SetState(MainMenu.State.Staging, true, true, true, false);
                while ((bool)waitForStaging.GetValue(game) || MainMenu.Instance().GetCurrentState() != MainMenu.State.Staging)
                    yield return null;
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

        static readonly FieldInfo flattenStart = AccessTools.Field(typeof(DFlattener), "m_startPos");
        static readonly FieldInfo flattenLoop = AccessTools.Field(typeof(DFlattener), "m_loop");

        static readonly FieldInfo moveStart = AccessTools.Field(typeof(MoveTransform), "m_startPosition");
        static readonly FieldInfo moveLoop = AccessTools.Field(typeof(MoveTransform), "m_loop");

        static void HandleSpecialComponent(MonoBehaviour comp)
        {
            if (comp is DFlattener flattener)
            {
                var loop = (AudioObject)flattenLoop.GetValue(flattener);
                if (loop is not null)
                {
                    if (loop != null)
                        loop.Stop(); // null prop doesn't work???
                    flattener.transform.position = (Vector3)flattenStart.GetValue(flattener);
                }
            }
            else if (comp is MoveTransform move)
            {
                var loop = (AudioObject)moveLoop.GetValue(move);
                if (loop is not null)
                {
                    if (loop != null)
                        loop.Stop();
                    move.transform.position = (Vector3)moveStart.GetValue(move);
                }
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


        static bool mmUpdating = false;
        static void PreMMUpdate() => mmUpdating = true;
        static void PostMMUpdate() => mmUpdating = false;
        static bool MMPausing(MainMenu __instance, bool setPause)
        {
            if (pauseStaging.Value && setPause && mmUpdating && noStaging.Value && __instance.GetCurrentState() != MainMenu.State.Staging)
            {
                RM.mechController.Die(true, true);
                return false;
            }
            return true;
        }
    }

    class RestartStart : MonoBehaviour
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
}
