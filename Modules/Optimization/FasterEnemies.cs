using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NeonLite.Modules.Optimization
{
    internal class FasterEnemies : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static void Activate(bool _)
        {
            Patching.AddPatch(typeof(Enemy), "Setup", NewStart, Patching.PatchTarget.Prefix);
            Patching.AddPatch(typeof(BaseDamageable), "GetMaterials", GetMatRewrite, Patching.PatchTarget.Prefix);
        }

        readonly static int hitTrigger = Animator.StringToHash("HIT");
        readonly static int idleAnim = Animator.StringToHash("PLAY");
        readonly static int offsetFloat = Animator.StringToHash("OFFSET");
        readonly static int speedFloat = Animator.StringToHash("SPEED");

        static void BDSetup(BaseDamageable damageable)
        {
            var collider = Helpers.Field(typeof(BaseDamageable), "_collider");
            var healthBar = Helpers.Field(typeof(BaseDamageable), "_healthBar");
            var hasHealthBar = Helpers.Field(typeof(BaseDamageable), "_hasHealthBar");
            // var showHealthBar = Helpers.Field(typeof(BaseDamageable), "showHealthBar");
            var startScale = Helpers.Field(typeof(BaseDamageable), "_startScale");
            var breakablePlatform = Helpers.Field(typeof(BaseDamageable), "_breakablePlatform");

            collider.SetValue(damageable, damageable.GetComponent<Collider>());
            if (true)// (bool)showHealthBar.GetValue(damageable)) // all enemies do this
            {
                GameObject gameObject = GameObject.Instantiate<GameObject>(EnemySpawner.ProgressBar);
                var p = gameObject.GetComponent<ProgressBar>();
                healthBar.SetValue(damageable, p);
                p.SetParent(damageable.transform);
                hasHealthBar.SetValue(damageable, true);
                p.SetBarVisibility(false, true);
            }
            startScale.SetValue(damageable, damageable.transform.localScale);
            damageable.SetNewHealth(damageable.maxHealth, damageable.maxHealth);
            breakablePlatform.SetValue(damageable, damageable.FindBreakablePlatform());
        }

        static bool NewStart(Enemy __instance, ref float ____headDriftSeed, ref EnemyAnimationController ____enemyAnimation, ref Animator ____animator)
        {
            Helpers.EnableProfiling(false);     

            if (__instance is EnemyBoss)
            {
                Helpers.StartProfiling("Setup Enemy Weapons");
                __instance.SetupEnemyWeapons();
                Helpers.EndProfiling();
            }

            ObjectManager.Instance.RegisterEnemy(__instance);
            // ew
            BDSetup(__instance);

            ____headDriftSeed = __instance.GetHashCode() * 0.1f;
            NeonLite.Logger.DebugMsg($"2 {__instance}");
            Helpers.StartProfiling("Animation");
            if (__instance.bodyHolder != null)
            {
                ____enemyAnimation = __instance.bodyHolder.GetComponent<EnemyAnimationController>();
                if (____enemyAnimation == null)
                {
                    ____animator = __instance.bodyHolder.GetComponent<Animator>();
                    if (____animator && ____animator.runtimeAnimatorController)
                    {
                        ____animator.ResetTrigger(hitTrigger);
                        ____animator.Play(idleAnim);
                        ____animator.SetFloat(offsetFloat, UnityEngine.Random.Range(0f, ____animator.GetCurrentAnimatorStateInfo(0).length));
                        ____animator.SetFloat(speedFloat, 1f + UnityEngine.Random.Range(-0.2f, 0.2f));
                    }

                }
            }
            Helpers.EndProfiling();

            Helpers.StartProfiling("Rotation");
            if (__instance.rotateBody && RM.drifter)
            {
                Vector3 vector = new Vector3(RM.playerPosition.x, __instance.transform.position.y, RM.playerPosition.z) - __instance.transform.position;
                __instance.transform.rotation = Quaternion.LookRotation(vector, Vector3.up);
            }
            else if (__instance.randomizeStartRotation)
            {   
                UnityEngine.Random.InitState((int)(__instance.transform.position.x + __instance.transform.position.z));
                __instance.transform.Rotate(new Vector3(0f, UnityEngine.Random.Range(0f, 360f), 0f));
            }
            Helpers.EndProfiling();

            Helpers.StartProfiling("EnemySetup");
            __instance.EnemySetup();
            Helpers.EndProfiling();
            Helpers.StartProfiling("EnemyStart");
            __instance.EnemyStart();
            Helpers.EndProfiling();

            Helpers.StartProfiling("Reload Shader Property");
            if (__instance is EnemyTripwire)
                __instance.useReloadShaderProperty = false; // these do not reload
            if (__instance.useReloadShaderProperty)
                __instance.SetReloadShaderFXAmount(1f);
            Helpers.EndProfiling();
            Helpers.StartProfiling("StartAmbientAudio");
            Helpers.Method(typeof(Enemy), "StartAmbientAudio").Invoke(__instance, null);
            Helpers.EndProfiling();

            Helpers.EnableProfiling(true);

            return false;
        }

        static bool GetMatRewrite(BaseDamageable __instance, ref Material[] ____materials, ref Material[] __result)
        {
            ____materials ??= __instance.GetRenderers().SelectMany(static r => r.materials).ToArray();
            __result = ____materials;
            return false;
        }
    }
}
