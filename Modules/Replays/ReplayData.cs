using Discord;
using HarmonyLib;
using MelonLoader;
using NeonLite.Modules.Optimization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static BaseDamageable;
using static MelonLoader.MelonLogger;
using static NeonLite.Modules.Replays.Frames;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static UnityEngine.UI.Image;

namespace NeonLite.Modules.Replays
{
    [HarmonyPatch]
    internal class ReplayData : MonoBehaviour, IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = true;

        static MelonPreferences_Entry<bool> recording;
        static MelonPreferences_Entry<bool> playing;
        static MelonPreferences_Entry<string> filename;
        static MelonPreferences_Entry<float> timescale;

        static bool restrict;
        static int forceAllow;


        class FrameBundle(List<IFrame> frames)
        {
            //public double time = 0;
            public List<IFrame> frames = frames;
        }

        static readonly List<FrameBundle> tickFrames = [];
        static readonly Dictionary<double, FrameBundle> timerFrames = [];
        static int curFrame;

        static int randSeed;

        static int curTimerPos;
        static int curMouseTimer;

        static void Setup()
        {
            recording = Settings.Add(Settings.h, "Replays", "recording", "Recording", null, false);
            recording.Value = true;
            playing = Settings.Add(Settings.h, "Replays", "playing", "Playing", null, false);
            playing.Value = false;
            filename = Settings.Add(Settings.h, "Replays", "filename", "Filename", null, "");
            timescale = Settings.Add(Settings.h, "Replays", "timescale", "Timescale", null, 1f, new MelonLoader.Preferences.ValueRange<float>(0, 5));
            timescale.Value = 1;
        }

        static readonly MethodInfo ogupvel = Helpers.Method(typeof(FirstPersonDrifter), "UpdateVelocity");
        static readonly MethodInfo ogpjsp = Helpers.Method(typeof(ProjectileBase), "CreateProjectile", [typeof(string), typeof(Vector3), typeof(Vector3), typeof(ProjectileWeapon)]);
        static readonly MethodInfo ogpjbset = Helpers.Method(typeof(ProjectileBase), "Setup");
        static readonly MethodInfo ogencset = Helpers.Method(typeof(EnemyEncounter), "Setup");

        static readonly MethodInfo ogupdcard = Helpers.Method(typeof(MechController), "UpdateCards");
        static readonly MethodInfo ogonpick = Helpers.Method(typeof(MechController), "OnPickupCard");
        static readonly MethodInfo ogpikcard = Helpers.Method(typeof(MechController), "DoCardPickup");
        static readonly MethodInfo ogconsume = Helpers.Method(typeof(MechController), "DoConsumable");
        static readonly MethodInfo ogfcardi = Helpers.Method(typeof(MechController), "FireCard", [typeof(int)]);
        static readonly MethodInfo ogfcardp = Helpers.Method(typeof(MechController), "FireCard", [typeof(PlayerCard)]);
        static readonly MethodInfo ogdcardi = Helpers.Method(typeof(MechController), "UseDiscardAbility", [typeof(int)]);
        static readonly MethodInfo ogcycleh = Helpers.Method(typeof(PlayerCardDeck), "CycleHand");

        static readonly MethodInfo ogabjmp = Helpers.Method(typeof(FirstPersonDrifter), "ForceJump");
        static readonly MethodInfo ogabflp = Helpers.Method(typeof(FirstPersonDrifter), "ForceFlap");
        static readonly MethodInfo ogabdsh = Helpers.Method(typeof(FirstPersonDrifter), "ForceDash");
        static readonly MethodInfo ogabstm = Helpers.Method(typeof(FirstPersonDrifter), "ForceStomp");
        static readonly MethodInfo ogabzip = Helpers.Method(typeof(FirstPersonDrifter), "DoZipline");
        static readonly MethodInfo ogabtlf = Helpers.Method(typeof(FirstPersonDrifter), "Telefrag");
        static readonly MethodInfo ogkikbak = Helpers.Method(typeof(MechController), "DoKickbackAbility");
        static readonly MethodInfo ogbakfir = Helpers.Method(typeof(MechController), "DoBackfireAbility");
        static readonly MethodInfo ogfirbal = Helpers.Method(typeof(MechController), "DoFireballAbility");
        static readonly MethodInfo ogonhit = Helpers.Method(typeof(MechController), "OnHit");

        static readonly MethodInfo ogenmdie = Helpers.Method(typeof(Enemy), "Die");
        static readonly MethodInfo ogtrpdie = Helpers.Method(typeof(EnemyTripwire), "Die");
        static readonly MethodInfo ogdmbdie = Helpers.Method(typeof(EnemyDemonBall), "Die");
        static readonly MethodInfo ogfofdie = Helpers.Method(typeof(EnemyForcefield), "Die");
        static readonly MethodInfo ogmmcdie = Helpers.Method(typeof(EnemyMimic), "Die");
        static readonly MethodInfo ogufodie = Helpers.Method(typeof(EnemyUFO), "Die");
        static readonly System.Type[] inherits = [typeof(EnemyTripwire), typeof(EnemyDemonBall), typeof(EnemyForcefield), typeof(EnemyMimic), typeof(EnemyUFO)];

        static readonly MethodInfo oguikill = Helpers.Method(typeof(PlayerUI), "OnGotKill");
        static readonly MethodInfo oggmkill = Helpers.Method(typeof(Game), "OnEnemyKill");
        static readonly MethodInfo ogwvkill = Helpers.Method(typeof(EnemyWave), "OnEnemyDead");

        static readonly MethodInfo ogmlrot = Helpers.Method(typeof(MouseLook), "UpdateRotation");

        static readonly MethodInfo ogpjwup = Helpers.Method(typeof(ProjectileWeapon), "WeaponUpdate");
        static readonly MethodInfo ogpjwfp = Helpers.Method(typeof(ProjectileWeapon), "FireProjectile");

        static readonly MethodInfo ogsettime = Helpers.Method(typeof(TimeManager), "SetTargetTimescale");

        static readonly MethodInfo ogvelfxu = Helpers.Method(typeof(VelocityFX), "Update");
        static readonly MethodInfo ogvelfxf = Helpers.Method(typeof(VelocityFX), "FixedUpdate");
        static readonly MethodInfo ogsprv3u = Helpers.Method(typeof(SpringVector3), "Update");

        static void TogglePatchPrePost(bool activate, MethodInfo method, Delegate pre, Delegate post)
        {
            Patching.TogglePatch(active, method, pre, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(active, method, post, Patching.PatchTarget.Postfix);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, ogupvel, LateUpdateHook, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, ogupvel, LateUpdateAfter, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, ogpjsp, OnProjectileFire, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, ogpjbset, OnProjectileSetup, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, ogencset, GetSetSeed, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, ogsettime, SetTimescale, Patching.PatchTarget.Prefix);

            TogglePatchPrePost(activate, ogupdcard, EnableRestrict, DisableRestrict);
            TogglePatchPrePost(activate, ogupvel, EnableRestrict, DisableRestrict);
            TogglePatchPrePost(activate, ogonpick, EnableRestrict, DisableRestrict);
            TogglePatchPrePost(activate, ogpjwup, EnableRestrict, DisableRestrict);

            TogglePatchPrePost(activate, ogenmdie, EnableRestrict, DisableRestrict);
            TogglePatchPrePost(activate, ogenmdie, RecordEnemyKill, RecordFunctionalEnd); // records *and* restricts itself
            TogglePatchPrePost(activate, ogtrpdie, EnableRestrict, DisableRestrict); //have to do the same for everything it overrides
            TogglePatchPrePost(activate, ogtrpdie, RecordEnemyKill, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogdmbdie, EnableRestrict, DisableRestrict);
            TogglePatchPrePost(activate, ogdmbdie, RecordEnemyKill, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogfofdie, EnableRestrict, DisableRestrict);
            TogglePatchPrePost(activate, ogfofdie, RecordEnemyKill, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogmmcdie, EnableRestrict, DisableRestrict);
            TogglePatchPrePost(activate, ogmmcdie, RecordEnemyKill, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogufodie, EnableRestrict, DisableRestrict);
            TogglePatchPrePost(activate, ogufodie, RecordEnemyKill, RecordFunctionalEnd);

            TogglePatchPrePost(activate, ogabjmp, RecordFunctional, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogabflp, RecordFunctional, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogabdsh, RecordFunctional, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogabstm, RecordFunctional, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogabzip, RecordFunctional, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogabtlf, RecordFunctional, RecordFunctionalEnd);
            //NeonLite.Harmony.Patch(oguikill, prefix: Helpers.HM(RunIfUnrestrict));
            //NeonLite.Harmony.Patch(ogwvkill, prefix: Helpers.HM(RecordEnemyKill));
            TogglePatchPrePost(activate, ogfcardi, RecordFunctionalTimer, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogfcardp, RecordFunctionalTimer, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogdcardi, RecordFunctionalTimer, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogcycleh, RunIfUnrestrict, RecordSwap);
            TogglePatchPrePost(activate, ogpikcard, RecordFunctionalTimer, RecordFunctionalEnd);

            TogglePatchPrePost(activate, ogkikbak, RecordFunctional, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogbakfir, RecordFunctional, RecordFunctionalEnd);
            TogglePatchPrePost(activate, ogfirbal, RecordFunctional, RecordFunctionalEnd);

            TogglePatchPrePost(activate, ogonhit, RecordFunctional, RecordFunctionalEnd);

            Patching.TogglePatch(activate, ogpjwup, GetProjectileIndex, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, ogpjwfp, RecordEnemyProjectile, Patching.PatchTarget.Prefix);

            Patching.TogglePatch(activate, ogmlrot, PreMouseRot, Patching.PatchTarget.Prefix);

            Patching.TogglePatch(activate, ogvelfxu, UpdateVelocityFX, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, ogvelfxf, FixedUpdateVelFX, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, ogsprv3u, SpringUpdate, Patching.PatchTarget.Prefix);

            Patching.TogglePatch(activate, oggmkill, RunIfUnrestrict, Patching.PatchTarget.Prefix);

            if (activate)
            {
                // ***THIS IS PROBABLY A REALLY FUCKING BAD FIX***
                // ***IF THIS GETS FIXED IN MELONLOADER, REMOVE THIS!!!!!!!!!!!***
                var type = AccessTools.TypeByName("MelonLoader.Core");
                var coreinstance = (HarmonyLib.Harmony)AccessTools.Field(type, "HarmonyInstance").GetValue(null);
                coreinstance.Unpatch(AccessTools.Method("HarmonyLib.PatchFunctions:ReversePatch"), HarmonyPatchType.Prefix);

                var hm = Helpers.HM(PerformHitscan);
                hm.reversePatchType = HarmonyReversePatchType.Original;
                var revpatcher = NeonLite.Harmony.CreateReversePatcher(ogfcardp, hm);
                revpatcher.Patch();

                type = AccessTools.TypeByName("MelonLoader.Fixes.InstancePatchFix");
                var refix = Helpers.Method(type, "PatchMethod");
                coreinstance.Patch(AccessTools.Method("HarmonyLib.PatchFunctions:ReversePatch"), prefix: refix.ToNewHarmonyMethod());
            }

            Patching.TogglePatch(activate, ogfcardp, FireCardHijack, Patching.PatchTarget.Transpiler);

            active = activate;
        }

        static void OnLevelLoad(LevelData level)
        {
            if (level.type != LevelData.LevelType.Level)
                return;

            curFrame = 0;
            curMouseTimer = 0;
            curTimerPos = 0;
            if (playing.Value)
                LoadData(filename.Value);
            else
            {
                tickFrames.Clear();
                timerFrames.Clear();
            }
            RM.mechController.GetOrAddComponent<ReplayData>();
        }

#pragma warning disable CS0618
        static void GetSetSeed()
        {
            if (playing.Value)
                UnityEngine.Random.seed = randSeed;
            else
                randSeed = UnityEngine.Random.seed;
        }
#pragma warning restore CS0618 

        static void SetTimescale(ref float newTimeScale)
        {
            if (MainMenu.Instance().GetCurrentState() == MainMenu.State.Staging && newTimeScale == 1)
                newTimeScale = timescale.Value;
        }

        static bool setAfter = false;

        static void LateUpdateHook(FirstPersonDrifter __instance, float deltaTime,
            ref Vector3 ___velocity, ref Vector3 ___movementVelocity,
            ref float ____moveStunAmount,
            ref float ___inputX, ref float ___inputY,
            ref bool ___jumpDown, ref bool ___jumpUp, ref bool ___jumpHeld,
            ref bool ____waitForReleaseJump)
        {
            if (recording.Value)
            {
                setAfter = true;

                var frame = new PlayerFrame()
                {
                    motorPos = __instance.Motor.TransientPosition,
                    velocity = ___velocity,
                    movementVel = ___movementVelocity,

                    moveStun = ____moveStunAmount,
                    inputX = ___inputX,
                    inputY = ___inputY,
                    jumpDown = ___jumpDown,
                    jumpUp = ___jumpUp,
                    jumpHeld = ___jumpHeld,
                    jumpRelease = ____waitForReleaseJump,
                    camRotation = __instance.m_cameraHolder.parent.rotation
                };
                if (tickFrames.Count > curFrame)
                    tickFrames[curFrame].frames.Insert(0, frame);
                else
                    tickFrames.Add(new([frame]));
                curFrame++;
            }
            else if (playing.Value && tickFrames.Count > curFrame)
            {
                setAfter = true;
                var r = restrict;
                restrict = false;

                var bundle = tickFrames[curFrame++];
                foreach (var frame in bundle.frames)
                {
                    if (frame is not PlayerFrame)
                        NeonLite.Logger.Msg(frame);
                    if (frame is PlayerFrame plframe)
                    {
                        __instance.Motor.SetPosition(plframe.motorPos);
                        ___velocity = plframe.velocity;
                        ___movementVelocity = plframe.movementVel;
                        ____moveStunAmount = plframe.moveStun;
                        ___inputX = plframe.inputX;
                        ___inputY = plframe.inputY;
                        ___jumpDown = plframe.jumpDown;
                        ___jumpUp = plframe.jumpUp;
                        ___jumpHeld = plframe.jumpHeld;
                        ____waitForReleaseJump = plframe.jumpRelease;
                        __instance.m_cameraHolder.parent.rotation = plframe.camRotation;
                    }
                    else if (frame is ProjectileFrame prFrame)
                        ProjectileBase.CreateProjectile(prFrame.id, prFrame.position, prFrame.direction, null);
                    else if (frame is HitscanFrame hsFrame)
                    {
                        ray = hsFrame.ray;
                        var card = new PlayerCard
                        {
                            data = new()
                            {
                                hitScanDist = hsFrame.dist,
                                hitScanDamage = hsFrame.damage,
                                rigidbodyKnockback = hsFrame.knockback,
                                canParryProjectiles = hsFrame.canParry,
                                isAbilityCard = hsFrame.isAbility,
                                burstIsMeleeSwipe = hsFrame.isSwipe
                            }
                        };
                        PerformHitscan(RM.mechController, card);
                    }
                    else if (frame is EnemyKillFrame ekFrame)
                    {
                        Enemy e = ObjectManager.Instance.damageableObjects[DamageableType.Enemy][ekFrame.enemy] as Enemy;
                        forceAllow = 1;
                        if (inherits.Contains(e.GetType()))
                            ++forceAllow;
                        NeonLite.Logger.Msg($"die {ekFrame.enemy}");

                        e.Die(ekFrame.damageSource);
                    }
                    else if (frame is FunctionalFrame fframe)
                    {
                        if (fframe.method.DeclaringType == typeof(FirstPersonDrifter))
                            fframe.instance = RM.drifter;
                        else if (fframe.method.DeclaringType == typeof(MechController))
                            fframe.instance = RM.mechController;
                        NeonLite.Logger.Msg($"run {fframe.method} {fframe.instance} {fframe.args}");
                        fframe.method.Invoke(fframe.instance, fframe.args);
                    }
                    else if (frame is EnemyProjectileFrame epFrame)
                    {
                        Enemy e = ObjectManager.Instance.damageableObjects[DamageableType.Enemy][epFrame.enemy] as Enemy;
                        var weapon = e.weapons[epFrame.weaponIndex] as ProjectileWeapon;
                        var proj = weapon.FireProjectile(epFrame.position, epFrame.direction);
                        ((Array)curProjectiles.GetValue(weapon)).SetValue(proj, epFrame.projectileIndex);
                    }
                }
                restrict = r;
            }
        }

        static void LateUpdateAfter(ref Vector3 currentVelocity)
        {
            if (setAfter)
            {
                if (recording.Value)
                {
                    var pf = (PlayerFrame)tickFrames[curFrame - 1].frames[0];
                    pf.endingVel = currentVelocity;
                    tickFrames[curFrame - 1].frames[0] = pf;
                }
                else
                    currentVelocity = ((PlayerFrame)tickFrames[curFrame - 1].frames[0]).endingVel;
            }
            setAfter = false;
        }

        static readonly MethodInfo reloadwp = Helpers.Method(typeof(MechController), "ReloadWeapon");

        void Update()
        {
            if (playing.Value)
            {
                var r = restrict;
                restrict = true;

                var time = EnsureTimer.InterpolatedTime;

                foreach (var bundle in timerFrames.Skip(curTimerPos))
                {
                    if (bundle.Key > time)
                        return;
                    ++curTimerPos;
                    foreach (var frame in bundle.Value.frames)
                    {
                        if (frame is FunctionalFrame fframe)
                        {
                            if (fframe.method.DeclaringType == typeof(FirstPersonDrifter))
                                fframe.instance = RM.drifter;
                            else if (fframe.method.DeclaringType == typeof(MechController))
                                fframe.instance = RM.mechController;
                            forceAllow = 1;
                            if (fframe.method == ogfcardi)
                                ++forceAllow;
                            fframe.method.Invoke(fframe.instance, fframe.args);
                        }
                        else if (frame is SwapFrame)
                        {
                            NeonLite.Logger.Msg($"swapframe");

                            forceAllow = 1;
                            RM.mechController.GetPlayerCardDeck().CycleHand(1);
                            RM.mechController._bulletsFiredThisBurst = 0;
                            RM.mechController.SetPlayerCards(false, null);
                            RM.mechController.StartCoroutine((IEnumerator)reloadwp.Invoke(RM.mechController, [0, false]));
                        }
                    }
                }

                restrict = r;
            }
            else if (recording.Value)
                curTimerPos = timerFrames.Count;
        }

        static bool OnProjectileFire(string path, Vector3 origin, Vector3 forward, ref ProjectileBase __result)
        {
            if (!recording.Value)
            {
                __result = new();
                return RunIfUnrestrict();
            }

            var frame = new ProjectileFrame()
            {
                id = path,
                position = origin,
                direction = forward
            };

            if (tickFrames.Count <= curFrame)
                tickFrames.Add(new([]));
            tickFrames[curFrame]?.frames.Add(frame);
            return true;
        }

        static Ray ray;
        static int num4;
        static bool PerformHitscan(MechController controller, PlayerCard card)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                if (instructions == null)
                    yield break;

                LocalBuilder wasNum4 = null;
                LocalBuilder wasRay = null;
                LocalBuilder flag3 = null;

                Label abortCode = generator.DefineLabel();

                IEnumerable<CodeInstruction> InnerTranspiler(IEnumerable<CodeInstruction> instructions)
                {
                    NeonLite.Logger.DebugMsg("-------------------------------------------INNER-------------------------------------------");

                    int hit = 0;
                    Label? label = null;
                    bool skip = true;

                    bool abort = false;

                    int local = -1;

                    LocalBuilder flag4 = null;
                    LocalBuilder flag5 = null;

                    foreach (var code in instructions)
                    {
                        if (label.HasValue && code.labels.Contains(label.Value))
                            break;
                        if (!skip)
                        {
                            NeonLite.Logger.Msg(code);
                            if (abort)
                            {
                                yield return new(code.opcode, abortCode);
                                abort = false;
                                continue;
                            }
                            if (code.LoadsConstant(0.0f) && wasRay == null)
                                local = 0;
                            else if (code.LoadsConstant(1) && wasNum4 == null)
                                local = 1;
                            else if (code.LoadsConstant(0) && flag3 == null)
                                local = 2;
                            else if (code.opcode == OpCodes.Ble && flag4 == null)
                            {
                                yield return new(code.opcode, abortCode);
                                local = 3;
                                continue;
                            }
                            else if (code.LoadsField(AccessTools.Field(typeof(PlayerCardData), "burstIsMeleeSwipe")) && flag5 == null)
                                local = 4;
                            if (code.opcode == OpCodes.Stloc_S || code.opcode == OpCodes.Ldloc_S)
                            {
                                switch (local)
                                {
                                    case 0: wasRay = code.operand as LocalBuilder; local = -1; break;
                                    case 1: wasNum4 = code.operand as LocalBuilder; local = -1; break;
                                    case 2: flag3 = code.operand as LocalBuilder; local = -1; break;
                                    case 3: flag4 = code.operand as LocalBuilder; local = -1; break;
                                    case 4: flag5 = code.operand as LocalBuilder; local = -1; break;
                                    default: break;
                                }

                                if (code.opcode == OpCodes.Stloc_S)
                                {
                                    /*if (code.operand == wasRay)
                                        yield return new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(ReplayData), "ray")).MoveLabelsFrom(code);
                                    else if (code.operand == wasNum4)
                                    {
                                        yield return new CodeInstruction(OpCodes.Stloc, temp).MoveLabelsFrom(code);
                                        yield return new CodeInstruction(OpCodes.Ldarg_3);
                                        yield return new CodeInstruction(OpCodes.Ldloc, temp);
                                        yield return new CodeInstruction(OpCodes.Stind_Ref);
                                    }
                                    else//*/
                                    yield return code;
                                }
                                else
                                {
                                    /*if (code.operand == wasRay)
                                        yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ReplayData), "ray")).MoveLabelsFrom(code);
                                    else if (code.operand == wasNum4)
                                    {
                                        yield return new CodeInstruction(OpCodes.Ldarg_3).MoveLabelsFrom(code);
                                        yield return new CodeInstruction(OpCodes.Ldind_Ref);
                                    }
                                    else//*/
                                    {
                                        if (code.operand == flag4 || code.operand == flag5)
                                            abort = true;
                                        yield return code;
                                    }
                                }
                            }
                            else
                                yield return code;
                        }
                        if (code.Branches(out var outLabel) && (++hit == 11))
                        {
                            label = outLabel;
                            skip = false;//*/
                        }
                    }
                    NeonLite.Logger.DebugMsg($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! {wasRay} {wasNum4} {flag3} {flag4} {flag5}");
                }

                var instlist = InnerTranspiler(instructions).ToList();
                NeonLite.Logger.DebugMsg("-------------------------------------------OUTER-------------------------------------------");
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ReplayData), "num4"));
                yield return new CodeInstruction(OpCodes.Stloc_S, wasNum4);
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ReplayData), "ray"));
                yield return new CodeInstruction(OpCodes.Stloc_S, wasRay);

                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return new CodeInstruction(OpCodes.Stloc_S, flag3);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Ldloc_S, wasRay);
                yield return new CodeInstruction(OpCodes.Call, Helpers.Method(typeof(ReplayData), "OnHitscanFire"));
                yield return new CodeInstruction(OpCodes.Brfalse, abortCode);


                foreach (var code in instlist.Take(instlist.Count - 1))
                {
                    NeonLite.Logger.DebugMsg(code);
                    yield return code;
                }
                NeonLite.Logger.Msg(new CodeInstruction(OpCodes.Ldloc_S, flag3).WithLabels(abortCode));
                NeonLite.Logger.Msg(new CodeInstruction(OpCodes.Ret));

                yield return new CodeInstruction(OpCodes.Ldloc_S, flag3).WithLabels(abortCode);
                yield return new CodeInstruction(OpCodes.Ldloc_S, wasNum4);
                yield return new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(ReplayData), "num4"));
                //yield return new CodeInstruction(OpCodes.Stind_Ref);
                yield return new(OpCodes.Ret);//*/
            }

            _ = Transpiler(null, null);
            return true;
        }

        static IEnumerable<CodeInstruction> FireCardHijack(IEnumerable<CodeInstruction> instructions)
        {
            LocalBuilder wasRay = null;
            LocalBuilder wasNum4 = null;
            LocalBuilder flag3 = null;

            int hit = 0;
            Label? label = null;
            bool skip = false;

            int local = -1;

            CodeInstruction escapeInst = null;

            foreach (var code in instructions)
            {
                if (label.HasValue && code.labels.Contains(label.Value))
                {
                    yield return escapeInst;
                    skip = false;
                }
                if (!skip)
                {
                    NeonLite.Logger.Msg(code);
                    if (code.Calls(Helpers.Method(typeof(Camera), "ViewportPointToRay", [typeof(Vector3)])) && wasRay == null)
                        local = 0;
                    else if (code.Calls(Helpers.Method(typeof(Debug), "DrawRay", [typeof(Vector3), typeof(Vector3), typeof(Color), typeof(float)])) && wasNum4 == null)
                        local = 1;
                    if (code.opcode == OpCodes.Stloc_S || code.opcode == OpCodes.Ldloc_S)
                    {
                        switch (local)
                        {
                            case 0: wasRay = code.operand as LocalBuilder; local = -1; break;
                            case 1: wasNum4 = code.operand as LocalBuilder; local = 2; break;
                            case 2: flag3 = code.operand as LocalBuilder; local = -1; break;
                            default: break;
                        }
                    }
                    yield return code;
                }

                if (code.Branches(out var outLabel))
                {
                    if (skip)
                        escapeInst = code;
                    else if (++hit == 11)
                    {
                        label = outLabel;
                        skip = true;//*/

                        yield return new(OpCodes.Ldloc_S, wasRay);
                        yield return new(OpCodes.Stsfld, AccessTools.Field(typeof(ReplayData), "ray"));
                        yield return new(OpCodes.Ldloc_S, wasNum4);
                        yield return new(OpCodes.Stsfld, AccessTools.Field(typeof(ReplayData), "num4"));
                        yield return new(OpCodes.Ldarg_0);
                        yield return new(OpCodes.Ldarg_1);
                        yield return new(OpCodes.Call, Helpers.Method(typeof(ReplayData), "PerformHitscan"));
                        yield return new(OpCodes.Stloc_S, flag3);
                    }
                }
            }
            NeonLite.Logger.DebugMsg($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! {wasRay} {wasNum4} {flag3}");
        }

        static bool OnHitscanFire(PlayerCard card, Ray ray)
        {
            if (!recording.Value)
                return RunIfUnrestrict();

            NeonLite.Logger.DebugMsg($"{ray.origin} {ray.direction}");

            var frame = new HitscanFrame()
            {
                ray = ray,
                dist = card.data.hitScanDist,
                damage = card.data.hitScanDamage,
                knockback = card.data.rigidbodyKnockback,
                canParry = card.data.canParryProjectiles,
                isAbility = card.data.isAbilityCard,
                isSwipe = card.data.burstIsMeleeSwipe
            };

            if (tickFrames.Count <= curFrame)
                tickFrames.Add(new([]));
            tickFrames[curFrame]?.frames.Add(frame);
            return true;
        }

        static void OnProjectileSetup(ProjectileBase __instance)
        {
            if (!playing.Value)
                return;
            __instance._Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }


        static void EnableRestrict(MethodBase __originalMethod)
        {
            //if (NeonLite.DEBUG)
            //    NeonLite.Logger.Msg($"restrict {__originalMethod}");
            restrict = true;
        }
        static void DisableRestrict() => restrict = false;
        static bool IsRestricted() => restrict && playing.Value;
        static bool RunIfUnrestrict() => !IsRestricted() || forceAllow-- > 0;


        static bool preventRecord = false;
        static bool RecordFunctional(object __instance, MethodBase __originalMethod, object[] __args)
        {
            if (!recording.Value || preventRecord)
                return true;
            preventRecord = true;

            var frame = new FunctionalFrame()
            {
                method = __originalMethod,
                instance = __instance,
                args = __args
            };

            if (tickFrames.Count <= curFrame)
                tickFrames.Add(new([]));
            tickFrames[curFrame]?.frames.Add(frame);
            return true;
        }
        static void RecordFunctionalEnd() => preventRecord = preventRecordKill = false;
        static bool RecordFunctionalTimer(object __instance, MethodBase __originalMethod, object[] __args)
        {
            if (!recording.Value || preventRecord)
                return RunIfUnrestrict();
            preventRecord = true;

            var frame = new FunctionalFrame()
            {
                method = __originalMethod,
                instance = __instance,
                args = __args
            };

            var time = EnsureTimer.InterpolatedTime;
            if (!timerFrames.TryGetValue(time, out var bundle))
            {
                bundle = new([]);
                timerFrames[time] = bundle;
            }
            bundle.frames.Add(frame);

            return true;
        }
        static void RecordSwap(bool __result)
        {
            if (!recording.Value || !__result)
                return;

            NeonLite.Logger.Msg($"swapframe");

            var time = EnsureTimer.InterpolatedTime;
            if (!timerFrames.TryGetValue(time, out var bundle))
            {
                bundle = new([]);
                timerFrames[time] = bundle;
            }
            bundle.frames.Add(new SwapFrame());

            return;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Game), "LevelSetupRoutine", MethodType.Enumerator)]
        static IEnumerable<CodeInstruction> LevelSetupPatch(IEnumerable<CodeInstruction> instructions)
        {
            bool check = false;
            bool skip = true;
            var levelSetup = AccessTools.Field(typeof(Game), "_levelSetup");
            var objmanInst = AccessTools.PropertyGetter(typeof(ObjectManager), "Instance");
            var objmanReset = Helpers.Method(typeof(ObjectManager), "Reset");
            foreach (var code in instructions)
            {
                if (skip && code.Calls(objmanReset))
                {
                    skip = false;
                    continue;
                }
                else if (code.Calls(objmanInst))
                {
                    skip = true;
                    yield return new CodeInstruction(OpCodes.Nop).MoveLabelsFrom(code);
                    continue;
                }

                yield return code;

                if (code.opcode == OpCodes.Ldc_I4_0)
                    check = true;
                else if (check)
                {
                    check = false;
                    if (code.StoresField(levelSetup))
                        yield return new CodeInstruction(OpCodes.Call, Helpers.Method(typeof(ReplayData), "SetupDamageables"));
                }
            }
        }
        static readonly Dictionary<BaseDamageable, int> reverseLookup = [];

        static void SetupDamageables()
        {
            reverseLookup.Clear();
            ObjectManager.Instance.Reset();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectManager), "RegisterDamageable")]
        static void RegisterReverse(ref ObjectManager __instance, ref BaseDamageable newDamageable)
        {
            reverseLookup.Add(newDamageable, __instance.damageableObjects[newDamageable.GetDamageableType()].Count - 1);
            if (playing.Value && newDamageable is Breakable breakable && breakable._fractureFX)
            {
                foreach (var chunk in breakable._fractureFX._chunks)
                    chunk._rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        static bool preventRecordKill = false;
        static bool RecordEnemyKill(Enemy __instance, DamageSource damageSource)
        {
            if (!recording.Value || preventRecordKill)
                return RunIfUnrestrict();
            preventRecordKill = true;

            var frame = new EnemyKillFrame()
            {
                enemy = reverseLookup[__instance],
                damageSource = damageSource
            };

            NeonLite.Logger.Msg($"die {frame.enemy}");

            if (tickFrames.Count <= curFrame)
                tickFrames.Add(new([]));
            tickFrames[curFrame]?.frames.Add(frame);
            return true;
        }

        static int startIndex = 0;
        static readonly FieldInfo projPerFrame = AccessTools.Field(typeof(ProjectileWeapon), "_projectilesPerFrame");
        static readonly FieldInfo projIndex = AccessTools.Field(typeof(ProjectileWeapon), "_projectileIndex");
        static readonly FieldInfo curProjectiles = AccessTools.Field(typeof(ProjectileWeapon), "_currentProjectiles");

        static void GetProjectileIndex(ProjectileWeapon __instance) => startIndex = (int)projIndex.GetValue(__instance);
        static bool RecordEnemyProjectile(ProjectileWeapon __instance, Vector3 position, Vector3 forward)
        {
            if (!recording.Value)
                return RunIfUnrestrict();

            Enemy e = __instance.weaponHolder.GetComponentInParent<Enemy>();
            if (!e)
                return true;

            int idx = e.weapons.FindIndex(x => x == __instance);

            if (idx == -1)
                return true;

            int projIndex = startIndex + (int)projPerFrame.GetValue(__instance);

            var frame = new EnemyProjectileFrame()
            {
                enemy = reverseLookup[e],
                weaponIndex = idx,
                projectileIndex = projIndex,
                position = position,
                direction = forward,
            };

            if (tickFrames.Count <= curFrame)
                tickFrames.Add(new([]));
            tickFrames[curFrame]?.frames.Add(frame);
            return true;
        }

        static void PreMouseRot(ref float ___rotationX, ref float ___rotationY, ref float ___rotAmountX, ref float ___rotAmountY)
        {
            var time = EnsureTimer.InterpolatedTime;

            if (recording.Value)
            {
                if (!timerFrames.TryGetValue(time, out var bundle))
                {
                    bundle = new([]);
                    timerFrames[time] = bundle;
                }
                foreach (var frame in bundle.frames)
                {
                    if (frame is MouseLookFrame mlFrame)
                    {
                        bundle.frames.Remove(frame);
                        if (mlFrame.rotationX == 0)
                            mlFrame.rotationX = ___rotationX;
                        if (mlFrame.rotationY == 0)
                            mlFrame.rotationY = ___rotationY;
                        bundle.frames.Add(mlFrame);

                        return;
                    }
                }
                bundle.frames.Add(new MouseLookFrame() { rotationX = ___rotationX, rotationY = ___rotationY });
            }
            else if (playing.Value && tickFrames.Count > curFrame)
            {
                ___rotAmountX = 0;
                ___rotAmountY = 0;
                double firstTime = 0;
                foreach (var bundle in timerFrames.Skip(curMouseTimer))
                {
                    curMouseTimer++;
                    foreach (var frame in bundle.Value.frames.Where(x => x is MouseLookFrame))
                    {
                        var mlFrame = (MouseLookFrame)frame;
                        if (bundle.Key >= time)
                        {
                            var t = 1 - (float)((bundle.Key - time) / (bundle.Key - firstTime));
                            ___rotationX = Mathf.Lerp(___rotationX, mlFrame.rotationX, t);
                            ___rotationY = Mathf.Lerp(___rotationY, mlFrame.rotationY, t);
                            curMouseTimer = Math.Max(0, curMouseTimer - 2);
                            return;
                        }
                        else
                        {
                            ___rotationX = mlFrame.rotationX;
                            ___rotationY = mlFrame.rotationY;
                            firstTime = bundle.Key;
                        }
                    }
                }
            }
        }

        static bool preventSpring = false;
        static void UpdateVelocityFX(SpringVector3 ____springVec, Transform ___sphereTransform)
        {
            if (!playing.Value)
                return;
            ____springVec.Update();
            if (____springVec.CurrentPos != Vector3.zero)
                ___sphereTransform.rotation = Quaternion.LookRotation(____springVec.CurrentPos, Vector3.up);
        }
        static void FixedUpdateVelFX() => preventSpring = true;
        static bool SpringUpdate()
        {
            var p = preventSpring;
            preventSpring = false;
            return !p;
        }

        static void LoadData(string filename)
        {
            return;
            tickFrames.Clear();
        }
        static void SaveData(string filename)
        {

        }
    }
}
