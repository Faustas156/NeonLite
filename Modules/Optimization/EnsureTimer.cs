#if DEBUG
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
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

        internal static Collider cOverride;

        static long maxTime;

        static LevelPlaythrough currentPlaythrough;

        static void Setup() { }

        static readonly FieldInfo levelSetup = AccessTools.Field(typeof(Game), "_levelSetup");

        static void Activate(bool activate) => NeonLite.Game.OnLevelLoadComplete += SetTrue;

        static void SetTrue()
        {
            lastMS = 0;
            currentMS = 0;
            locked = false;
            processed = false;
        }

        [HarmonyPatch(typeof(LevelPlaythrough), "Update")]
        [HarmonyPrefix]
        static bool StopTimer(LevelPlaythrough __instance, long maxLevelTime)
        {
            if (maxLevelTime != 0)
                maxTime = maxLevelTime;
            if (__instance != null)
                currentPlaythrough = __instance;
            return false;
        }

        [HarmonyPatch(typeof(FirstPersonDrifter), "Update")]
        [HarmonyPrefix]
        static void FPDUpdate(FirstPersonDrifter __instance)
        {
            //levelSetup.SetValue(NeonLite.Game, false); 
            processed = true;
        }

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
            if (NeonLite.DEBUG)
                NeonLite.Logger.Msg($"after movement {currentMS}");
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
            //if (NeonLite.DEBUG)
            //    NeonLite.Logger.Msg($"dt {c} override? {cOverride}");
        }

        internal static long CalculateOffset(Collider trigger, Vector3 DEBUGPOS = default)
        {
            var rigidbody = (extendedTrigger ? RM.drifter.playerDashDamageableTrigger as Component : RM.drifter).GetComponent<Rigidbody>();
            if (NeonLite.DEBUG)
                NeonLite.Logger.Msg($"trigger {trigger} rigidbody {rigidbody}");

            var preLayer = rigidbody.gameObject.layer;
            rigidbody.gameObject.layer = LayerMask.NameToLayer("Player");

            var prePos = rigidbody.position;
            // because the player (should) update its position and velocity the *SECOND* before this is checked,
            // check the last pathing to see if we hit it b4 (this is the most common case bc of how unity works)
            RaycastHit hit = default;
            rigidbody.position = lastPos;
            var vel = lastVel;
            var time = currentMS;
            var preTime = lastMS;
            var balanced = vel.magnitude * ((time - preTime) / 1000000f); // the amount you actually move
            if (NeonLite.DEBUG)
            {
                NeonLite.Logger.Msg($"test last {lastPos} {vel} {balanced} {preTime}-{time}");
                NeonLite.Logger.Msg($"compared {lastPos}+{vel.normalized * balanced} = {lastPos + vel.normalized * balanced} = {currentPos}");

                var playerC = rigidbody.GetComponent<CapsuleCollider>();
                var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                obj.name = "COLVIEW";
                UnityEngine.Object.Destroy(obj.GetComponent<CapsuleCollider>());
                //obj.transform.parent = ccollider.transform;
                obj.transform.localScale = new(playerC.radius * 2, playerC.height / 2, playerC.radius * 2);
                obj.transform.position = lastPos;
                obj.transform.localRotation = Quaternion.Euler(playerC.direction == 2 ? 90 : 0, 0, playerC.direction == 0 ? 90 : 0);

                var render = obj.GetComponent<MeshRenderer>();
                render.material.shader = Shader.Find("NW/Environment/PBR_TriplanarTint_Env");
                //render.materials[i].SetFloat("_Cull", 0);
                render.material.SetColor("_AlbedoColorTint", Color.red);


                obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                obj.name = "COLVIEW";
                UnityEngine.Object.Destroy(obj.GetComponent<CapsuleCollider>());
                //obj.transform.parent = ccollider.transform;
                obj.transform.localScale = new(playerC.radius * 2, playerC.height / 2, playerC.radius * 2);
                obj.transform.position = currentPos;
                obj.transform.localRotation = Quaternion.Euler(playerC.direction == 2 ? 90 : 0, 0, playerC.direction == 0 ? 90 : 0);

                render = obj.GetComponent<MeshRenderer>();
                render.material.shader = Shader.Find("NW/Environment/PBR_TriplanarTint_Env");
                //render.materials[i].SetFloat("_Cull", 0);
                render.material.SetColor("_AlbedoColorTint", Color.green);

                if (DEBUGPOS != default)
                {
                    obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    obj.name = "COLVIEW";
                    UnityEngine.Object.Destroy(obj.GetComponent<CapsuleCollider>());
                    //obj.transform.parent = ccollider.transform;
                    obj.transform.localScale = new(playerC.radius * 2, playerC.height / 2, playerC.radius * 2);
                    obj.transform.position = DEBUGPOS;
                    obj.transform.localRotation = Quaternion.Euler(playerC.direction == 2 ? 90 : 0, 0, playerC.direction == 0 ? 90 : 0);

                    render = obj.GetComponent<MeshRenderer>();
                    render.material.shader = Shader.Find("NW/Environment/PBR_TriplanarTint_Env");
                    //render.materials[i].SetFloat("_Cull", 0);
                    render.material.SetColor("_AlbedoColorTint", Color.yellow);

                }

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

                if (render)
                {
                    //render.enabled = collider.isTrigger;
                    render.materials = new Material[materialCount];
                    for (int i = 0; i < materialCount; i++)
                    {
                        render.materials[i].shader = Shader.Find("NW/Environment/PBR_TriplanarTint_Env");
                        //render.materials[i].SetFloat("_Cull", 0);
                        render.materials[i].SetColor("_AlbedoColorTint", Color.blue);
                    }
                }
            }

            var hits = rigidbody.SweepTestAll(vel.normalized, balanced, QueryTriggerInteraction.Collide);
            if (hits.Length > 0)
                hit = hits.OrderBy(x => x.distance).FirstOrDefault(x => x.collider == trigger);

            rigidbody.position = prePos;
            rigidbody.gameObject.layer = preLayer;
            if (!hit.collider)
                return long.MaxValue;

            var percent = hit.distance / balanced;
            if (NeonLite.DEBUG)
                NeonLite.Logger.Msg($"percent {percent} {hit.distance}/{balanced}");

            return (long)(preTime + (time - preTime) * percent);
        }

        static void PerformFinish(Collider c, Vector3 DEBUGPOS = default)
        {
            if (locked)
                return;
            var finish = CalculateOffset(c, DEBUGPOS);
            if (finish != long.MaxValue)
                currentMS = finish;
            else
                currentMS = lastMS;
            currentPlaythrough?.OverrideLevelTimerMicroseconds(Math.Min(currentMS, maxTime));
            locked = true;
        }

        [HarmonyPatch(typeof(LevelGate), "OnTriggerStay")]
        [HarmonyPrefix]
        static bool SetTimeForFinish(LevelGate __instance, bool ____unlocked, bool ____playerWon, Collider other)
        {
            if (!____unlocked || ____playerWon || locked)
                return true;
            if (NeonLite.DEBUG)
                NeonLite.Logger.Msg(other);
            PerformFinish(cOverride ?? __instance.GetComponentInChildren<MeshCollider>(), other.attachedRigidbody.position);
            return !NeonLite.DEBUG;
        }

        [HarmonyPatch(typeof(LevelGateBookOfLife), "OnTriggerStay")]
        [HarmonyPrefix]
        static bool SetTimeForFinish(LevelGateBookOfLife __instance, bool ____playerWon)
        {
            if (____playerWon || locked)
                return true;
            PerformFinish(__instance.GetComponentInChildren<MeshCollider>());
            return !NeonLite.DEBUG;
        }

        [HarmonyPatch(typeof(CardPickup), "OnTriggerStay")]
        [HarmonyPrefix]
        static bool SetTimeForFinish(CardPickup __instance, PlayerCardData ____currentCard, bool ____pickupAble)
        {
            if (____currentCard.consumableType != PlayerCardData.ConsumableType.LoreCollectible || !____pickupAble || locked)
                return true;
            PerformFinish(__instance.GetComponent<CapsuleCollider>());
            return !NeonLite.DEBUG;
        }

        [HarmonyPatch(typeof(PlayerWinTrigger), "OnTriggerEnter")]
        [HarmonyPrefix]
        static bool SetTimeForFinish(PlayerWinTrigger __instance, Collider other)
        {
            if (RM.mechController == null || !RM.mechController.GetIsAlive() || other != RM.drifter.GetComponent<CapsuleCollider>() || locked)
                return true;
            PerformFinish(__instance.GetComponent<BoxCollider>());
            return !NeonLite.DEBUG;
        }

        [HarmonyPatch(typeof(MechController), "Die")]
        [HarmonyPrefix]
        static void OnDie() => locked = true;

        // for now, im tired of working on this and doing nothing else 2day
        [HarmonyPatch(typeof(Game), "OnLevelWin")]
        [HarmonyPrefix]
        static bool StopFinishes()
        {
            locked = true;
            return false;
        }

        //static void OnLevelLoad(LevelData _) => levelSetup.SetValue(NeonLite.Game, false);
    }
}
#endif