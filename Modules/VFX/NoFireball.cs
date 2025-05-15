using HarmonyLib;
using MelonLoader;
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

        static MelonPreferences_Entry<float> startDelay;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noFireball", "Disable fireball screen effect", "Disables the red outline from fireball.", false);
            startDelay = Settings.Add(Settings.h, "VFX", "fireballDelay", "Fireball skip", 
                "Setting this option to anything above 0 will skip the first X seconds of the fireball instead of preventing it entirely.\nRequires the above setting to be on.", 
                0f, new MelonLoader.Preferences.ValueRange<float>(0, 2));
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
            if (startDelay.Value > 0)
            {
                ps.Simulate(startDelay.Value, true, true);
                ps.Play();
            }
            else
                ps.Stop();
        }

        static IEnumerable<CodeInstruction> StopParticles(IEnumerable<CodeInstruction> instructions)
        {
            var play = Helpers.Method(typeof(ParticleSystem), "Play");
            var over = Helpers.Method(typeof(NoFireball), "PlayOverride");

            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(x => x.Calls(play)))
                .Repeat(m => m.SetInstruction(new CodeInstruction(OpCodes.Call, over).WithLabels(m.Labels)))
                .InstructionEnumeration();
        }
    }
}
