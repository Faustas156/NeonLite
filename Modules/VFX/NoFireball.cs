﻿using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NeonLite.Modules.VFX
{
    internal class NoFireball : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        internal static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noFireball", "Disable fireball screen effect", "Disables the red outline from fireball.", false);
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static readonly MethodInfo original = Helpers.Method(typeof(MechController), "FireballRoutine").MoveNext();
        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, original, StopParticles, Patching.PatchTarget.Transpiler);

            active = activate;
        }

        static void PlayOverride(ParticleSystem ps)
        {
            if (active)
                ps.Stop();
            else
                ps.Play();
        }

        static IEnumerable<CodeInstruction> StopParticles(IEnumerable<CodeInstruction> instructions)
        {
            var play = Helpers.Method(typeof(ParticleSystem), "Play");
            var over = Helpers.Method(typeof(NoFireball), "PlayOverride");

            foreach (var code in instructions)
            {
                if (code.Calls(play))
                    yield return new CodeInstruction(OpCodes.Call, over).MoveLabelsFrom(code);
                else
                    yield return code;
            }
        }
    }
}
