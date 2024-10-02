using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MelonLoader.MelonLogger;

namespace NeonLite.Modules.Replays
{
    internal class ReplayData : MonoBehaviour, IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = true;

        static MelonPreferences_Entry<bool> recording;
        static MelonPreferences_Entry<bool> playing;
        static MelonPreferences_Entry<string> filename;

        enum FrameType
        {
            Player,
            Projectile,
            Hitscan
        }

        struct PlayerFrame
        {
            public float inputX;
            public float inputY;
            public bool jumpDown;
            public bool jumpHeld;
            public bool jumpUp;
            public bool jumpRelease;

            public Quaternion camY;
            public Quaternion camX;
        }

        struct ProjectileFrame
        {
            public string id;
            public Vector3 position;
            public Vector3 direction;
        }

        struct HitscanFrame
        {
            public Vector3 position;
            public Vector3 velocity;
            public Quaternion rotation;
        }

        class FrameBundle(List<object> frames)
        {
            public List<object> frames = frames;
        }

        enum DyanmicTypes
        {
            CamPos,
            FireProjectile,
        }

        static readonly List<FrameBundle> bundles = [];
        static int curFrame;
        static int randSeed;

        static void Setup()
        {
            recording = Settings.Add(Settings.h, "Replays", "recording", "Recording", null, false);
            playing = Settings.Add(Settings.h, "Replays", "playing", "Playing", null, false);
            filename = Settings.Add(Settings.h, "Replays", "filename", "Filename", null, "");
        }

        static readonly MethodInfo ogupvel = AccessTools.Method(typeof(FirstPersonDrifter), "UpdateVelocity");
        static readonly MethodInfo ogpjsp = AccessTools.Method(typeof(ProjectileBase), "CreateProjectile", [typeof(string), typeof(Vector3), typeof(Vector3), typeof(ProjectileWeapon)]);
        static readonly MethodInfo ogencset = AccessTools.Method(typeof(EnemyEncounter), "Setup");

        static void Activate(bool activate)
        {
            if (activate)
            {
                NeonLite.Harmony.Patch(ogupvel, prefix: Helpers.HM(LateUpdateHook));
                NeonLite.Harmony.Patch(ogpjsp, prefix: Helpers.HM(OnProjectileFire));
                NeonLite.Harmony.Patch(ogencset, prefix: Helpers.HM(GetSetSeed));
            }
            else
            {
                NeonLite.Harmony.Unpatch(ogupvel, Helpers.MI(LateUpdateHook));
                NeonLite.Harmony.Unpatch(ogpjsp, Helpers.MI(OnProjectileFire));
                NeonLite.Harmony.Unpatch(ogencset, Helpers.MI(GetSetSeed));
            }

            active = activate;
        }

        static void OnLevelLoad(LevelData _)
        {
            curFrame = 0;
            if (playing.Value)
                LoadData(filename.Value);
            else
                bundles.Clear();
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

        static void LateUpdateHook(FirstPersonDrifter __instance,
            ref float ___inputX, ref float ___inputY,
            ref bool ___jumpDown, ref bool ___jumpUp, ref bool ___jumpHeld,
            ref bool ____waitForReleaseJump)
        {
            if (recording.Value)
            {
                var frame = new PlayerFrame()
                {
                    inputX = ___inputX,
                    inputY = ___inputY,
                    jumpDown = ___jumpDown,
                    jumpUp = ___jumpUp,
                    jumpHeld = ___jumpHeld,
                    jumpRelease = ____waitForReleaseJump,
                    camY = __instance.m_cameraRotationY,
                    camX = __instance.m_cameraRotationX
                };
                if (bundles.Count > curFrame)
                    bundles[curFrame].frames.Add(frame);
                else
                    bundles.Add(new([frame]));
                curFrame++;
            }
            else if (bundles.Count > curFrame)
            {
                var bundle = bundles[curFrame++];
                foreach (var frame in bundle.frames) {
                    if (frame is PlayerFrame plframe)
                    {
                        ___inputX = plframe.inputX;
                        ___inputY = plframe.inputY;
                        ___jumpDown = plframe.jumpDown;
                        ___jumpUp = plframe.jumpUp;
                        ___jumpHeld = plframe.jumpHeld;
                        ____waitForReleaseJump = plframe.jumpRelease;
                        __instance.m_cameraHolder.localRotation = __instance.m_cameraRotationY = plframe.camY;
                        __instance.m_cameraHolder.parent.localRotation = __instance.m_cameraRotationX = plframe.camX;
                    }
                    else if (frame is ProjectileFrame prFrame)
                        ProjectileBase.CreateProjectile(prFrame.id, prFrame.position, prFrame.direction, null);
                }
            }
        }

        static void OnProjectileFire(string path, Vector3 origin, Vector3 forward)
        {
            if (!recording.Value)
                return;
            var frame = new ProjectileFrame()
            {
                id = path,
                position = origin,
                direction = forward
            };

            if (bundles.Count <= curFrame)
                bundles.Add(new([]));
            bundles[curFrame]?.frames.Add(frame);
        }

        static void LoadData(string filename)
        {
            return;
            bundles.Clear();
        }
        static void SaveData(string filename)
        {

        }
    }
}
