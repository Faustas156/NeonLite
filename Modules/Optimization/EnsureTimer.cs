using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeonLite.Modules.Optimization
{
    [HarmonyPatch]
    internal class EnsureTimer : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static bool processed = false;
        static bool locked = false;

        static Vector3 lastVel;
        static Vector3 lastPos;
        static long lastMS;

        static Vector3 currentVel;
        static Vector3 currentPos;
        static long currentMS;
        static bool extendedTrigger;
        static bool maybeET;

        internal static Collider cOverride;

        static long maxTime;

        static LevelPlaythrough currentPlaythrough;

        static long ghostTimer;

        static void Setup() { }

        static void Activate(bool activate) => NeonLite.Game.OnLevelLoadComplete += SetTrue;

        [HarmonyPatch(typeof(LevelPlaythrough), "Reset")]
        [HarmonyPatch(typeof(Game), "LevelSetupRoutine")]
        [HarmonyPrefix]
        static void SetTrue()
        {
            if (NeonLite.DEBUG)
                NeonLite.Logger.Msg("RESET");
            lastMS = 0;
            currentMS = 0;
            ghostTimer = 0;
            maxTime = 0;
            locked = false;
            processed = false;
            currentPlaythrough?.OverrideLevelTimerMicroseconds(Math.Min(currentMS, maxTime));
        }

        [HarmonyPatch(typeof(LevelPlaythrough), "Update")]
        [HarmonyPrefix]
        static bool StopTimer(LevelPlaythrough __instance, long maxLevelTime)
        {
            if (maxLevelTime != 0)
                maxTime = maxLevelTime;
            if (__instance != null)
                currentPlaythrough = __instance;
            ghostTimer += (long)(Time.deltaTime * 1000000f);
            ghostTimer = Math.Min(ghostTimer, maxTime);
            return false;
        }

        static long GetGhostMS(Game _) => ghostTimer;

        [HarmonyPatch(typeof(GhostPlayback), "LateUpdate")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> UseGhostTimer(IEnumerable<CodeInstruction> instructions)
        {
            var gettimer = AccessTools.Method(typeof(Game), "GetCurrentLevelTimerMicroseconds");
            foreach (var code in instructions)
            {
                if (code.Calls(gettimer))
                    yield return CodeInstruction.Call(typeof(EnsureTimer), "GetGhostMS");
                else
                    yield return code;
            }
        }


        [HarmonyPatch(typeof(FirstPersonDrifter), "Update")]
        [HarmonyPrefix]
        static void FPDUpdate(FirstPersonDrifter __instance) => processed = true;

        [HarmonyPatch(typeof(FirstPersonDrifter), "UpdateVelocity")]
        [HarmonyPostfix]
        static void GetVelocityTickTimer(FirstPersonDrifter __instance, ref Vector3 currentVelocity, float deltaTime)
        {
            cOverride = null;
            extendedTrigger = false;
            lastVel = currentVel;
            lastPos = currentPos;

            currentVel = currentVelocity;
            currentPos = __instance.transform.position;

            if (processed)
            {
                if (!locked && RM.mechController && RM.mechController.GetIsAlive())
                {
                    lastMS = currentMS;
                    currentMS += (long)(deltaTime * 1000000f);
                }
            }
            else
                currentMS = 0;
            currentPlaythrough?.OverrideLevelTimerMicroseconds(Math.Min(currentMS, maxTime));
        }

        [HarmonyPatch(typeof(DamageableTrigger), "OnTriggerStay")]
        [HarmonyPrefix]
        static void DamageableTriggerStop(Collider c)
        {
            if (c && !cOverride && c.attachedRigidbody?.GetComponent<LevelGate>())
            {
                extendedTrigger = true;
                cOverride = c;
            }

            maybeET = true;
            //if (NeonLite.DEBUG)
            //    NeonLite.Logger.Msg($"dt pre");

            //if (NeonLite.DEBUG)
            //    NeonLite.Logger.Msg($"dt {c} override? {cOverride}");
        }
        [HarmonyPatch(typeof(DamageableTrigger), "OnTriggerStay")]
        [HarmonyPostfix]
        static void DamageableTriggerStop()
        {
            maybeET = false;
            //if (NeonLite.DEBUG)
            //    NeonLite.Logger.Msg($"dt post");
        }



        [HarmonyPatch(typeof(MechController), "Die")]
        [HarmonyPrefix]
        static void OnDie() => locked = true;

        internal static long CalculateOffset(Collider trigger)
        {
            var rigidbody = (extendedTrigger ? RM.drifter.playerDashDamageableTrigger as Component : RM.drifter).GetComponent<Rigidbody>();
            if (NeonLite.DEBUG)
                NeonLite.Logger.Msg($"trigger {trigger} rigidbody {rigidbody}");

            var preLayer = rigidbody.gameObject.layer;
            rigidbody.gameObject.layer = LayerMask.NameToLayer("Player");

            var prePos = rigidbody.position;
            // because the player (should) update its velocity the *SECOND* before this is checked,
            // check the last pathing to see if we hit it b4 (this is how unity works)
            RaycastHit hit = default;
            rigidbody.position = lastPos;
            var vel = lastVel;
            var time = currentMS;
            var preTime = lastMS;
            var balanced = vel.magnitude * ((time - preTime) / 1000000f); // the amount you actually move

            if (NeonLite.DEBUG)
            {
                NeonLite.Logger.Msg($"test last {lastPos} {vel} {balanced} {preTime}-{time}");

                var playerC = rigidbody.GetComponent<CapsuleCollider>();
                var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                obj.name = "COLVIEW";
                UnityEngine.Object.Destroy(obj.GetComponent<CapsuleCollider>());
                obj.transform.localScale = new(playerC.radius * 2, playerC.height / 2, playerC.radius * 2);
                obj.transform.position = lastPos;
                obj.transform.localRotation = Quaternion.Euler(playerC.direction == 2 ? 90 : 0, 0, playerC.direction == 0 ? 90 : 0);

                var render = obj.GetComponent<MeshRenderer>();
                render.material.shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                render.sortingOrder = 1;
                render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                render.receiveShadows = false;
                render.material.SetColor("_TintColor", Color.red.Alpha(0.1f));

                obj = UnityEngine.Object.Instantiate(obj);
                obj.transform.position = currentPos;

                render = obj.GetComponent<MeshRenderer>();
                UnityEngine.Object.Destroy(obj.GetComponent<CapsuleCollider>());
                render.material.SetColor("_TintColor", Color.green.Alpha(0.1f));
                render.sortingOrder = 2;

                render = null;
                int materialCount = 1;
                if (trigger is MeshCollider mcollider)
                {
                    obj = new GameObject("COLVIEW", typeof(MeshRenderer), typeof(MeshFilter));
                    obj.transform.parent = trigger.transform;
                    obj.transform.localScale = Vector3.one;
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                    var filter = obj.GetComponent<MeshFilter>();
                    render = obj.GetComponent<MeshRenderer>();
                    filter.mesh = mcollider.sharedMesh;
                    materialCount = filter.mesh.subMeshCount;
                }
                else if (trigger is BoxCollider bcollider)
                {
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.name = "COLVIEW";
                    UnityEngine.Object.Destroy(obj.GetComponent<BoxCollider>());
                    obj.transform.parent = trigger.transform;
                    obj.transform.localScale = bcollider.size;
                    obj.transform.localPosition = bcollider.center;
                    obj.transform.localRotation = Quaternion.identity;
                    render = obj.GetComponent<MeshRenderer>();
                }
                else if (trigger is CapsuleCollider ccollider)
                {
                    obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    obj.name = "COLVIEW";
                    UnityEngine.Object.Destroy(obj.GetComponent<CapsuleCollider>());
                    obj.transform.parent = trigger.transform;
                    obj.transform.localScale = new(ccollider.radius * 2, ccollider.height / 2, ccollider.radius * 2);
                    obj.transform.localPosition = ccollider.center;
                    obj.transform.localRotation = Quaternion.Euler(ccollider.direction == 2 ? 90 : 0, 0, ccollider.direction == 0 ? 90 : 0);
                    render = obj.GetComponent<MeshRenderer>();
                }

                obj.transform.parent = null;

                if (render)
                {
                    //render.enabled = collider.isTrigger;
                    render.materials = new Material[materialCount];
                    for (int i = 0; i < materialCount; i++)
                    {
                        render.material.shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                        render.sortingOrder = 1;
                        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        render.receiveShadows = false;
                        render.materials[i].SetColor("_TintColor", Color.blue.Alpha(0.1f));
                    }
                }
            }

            var hits = rigidbody.SweepTestAll(vel.normalized, balanced, QueryTriggerInteraction.Collide);
            if (hits.Length > 0)
                hit = hits.OrderBy(x => x.distance).FirstOrDefault(x => x.collider == trigger);

            rigidbody.position = prePos;
            rigidbody.gameObject.layer = preLayer;
            if (!hit.collider)
            {
                if (NeonLite.DEBUG)
                    NeonLite.Logger.Msg($"missed, reporting 0%");

                return lastMS;
            }

            var percent = hit.distance / balanced;
            if (NeonLite.DEBUG)
                NeonLite.Logger.Msg($"percent {percent} {hit.distance}/{balanced}");

            return (long)(preTime + (time - preTime) * percent);
        }

        static void PerformFinish(Collider c)
        {
            if (locked)
                return;
            currentMS = CalculateOffset(c);

            currentPlaythrough?.OverrideLevelTimerMicroseconds(Math.Min(currentMS, maxTime));
            locked = true;
        }

        [HarmonyPatch(typeof(LevelGate), "OnTriggerStay")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static void SetTimeForFinish(LevelGate __instance, bool ____unlocked, bool ____playerWon, Collider other)
        {
            if (!____unlocked || ____playerWon || locked)
                return;
            PerformFinish(cOverride ?? __instance.GetComponentInChildren<MeshCollider>());
        }

        [HarmonyPatch(typeof(LevelGateBookOfLife), "OnTriggerStay")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        // ???
        static void SetTimeForFinish(LevelGateBookOfLife __instance, bool ____playerWon)
        {
            if (____playerWon || locked)
                return;
            PerformFinish(__instance.GetComponentInChildren<MeshCollider>());
        }

        [HarmonyPatch(typeof(CardPickup), "OnTriggerStay")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static void SetTimeForFinish(CardPickup __instance, PlayerCardData ____currentCard, bool ____pickupAble)
        {
            if (NeonLite.DEBUG)
                NeonLite.Logger.Msg($"triggerstay {____currentCard.cardName}");

            if (____currentCard.consumableType != PlayerCardData.ConsumableType.LoreCollectible || !____pickupAble || locked)
                return;

            PerformFinish(__instance.GetComponent<CapsuleCollider>());
        }

        [HarmonyPatch(typeof(FirstPersonDrifter), "OnMovementHitDamageable")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static void SetTimeForFinish(FirstPersonDrifter __instance, BaseDamageable dmg)
        {
            //if (NeonLite.DEBUG)
            //    NeonLite.Logger.Msg($"hit damage {maybeET} {dmg} {__instance.GetIsDashing()}");

            var lore = dmg as BreakableLoreCollectible;
            if (!lore || !__instance.GetIsDashing())
                return;

            // im not sure why this is sometimes false?
            // but it really should always be true even the times where it's false behaves like true
            // extendedTrigger = maybeET; // should basically be true
            extendedTrigger = true;

            PerformFinish(dmg.GetComponent<CapsuleCollider>());
        }

        [HarmonyPatch(typeof(PlayerWinTrigger), "OnTriggerEnter")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static void SetTimeForFinish(PlayerWinTrigger __instance, Collider other)
        {
            if (RM.mechController == null || !RM.mechController.GetIsAlive() || other != RM.drifter.GetComponent<CapsuleCollider>() || locked)
                return;
            PerformFinish(__instance.GetComponent<BoxCollider>());
        }
#if DEBUG
        /* // for now, im tired of working on this and doing nothing else 2day
        
        [HarmonyPatch(typeof(Game), "OnLevelWin")]
        [HarmonyPrefix]
        static bool StopFinishes()
        {
            locked = true;
            return false;
        } //*/
#endif
    }
}
