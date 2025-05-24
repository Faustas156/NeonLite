using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NeonLite.Modules.VFX
{
    // ORIGINAL CODE BY PUPPYPOWERTOOLS AUTHOR HECATE/PANDORAS FOX
    internal class NoStompFlash : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noStomp", "Disable stomp flash", "Disable the white explosion that appears when you land a stomp.", false);
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(MechController), "DoStompAbility", StopFX, Patching.PatchTarget.Transpiler);
            active = activate;
        }

        static IEnumerable<CodeInstruction> StopFX(IEnumerable<CodeInstruction> instructions)
        {
            var exploder = Helpers.Field(typeof(RM), "exploder");

            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(x => x.LoadsField(exploder)))
                .CloneInPlace(out var scan)
                .MatchForward(false, new CodeMatch(static x => x.opcode == OpCodes.Ldstr))
                .AddLabels(scan.Labels)
                .CloneInPlace(out var ld)
                .RemoveInstructionsInRange(scan.Pos, ld.Pos - 1)
                .InstructionEnumeration();
        }
    }

    internal class NoStompSplash : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noStompSplash", "Disable stomp wave", "Disable the green wave that appears when you land a stomp.", false);
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(MechController), "DoStompAbility", StopFX, Patching.PatchTarget.Transpiler);
            active = activate;
        }

        static IEnumerable<CodeInstruction> StopFX(IEnumerable<CodeInstruction> instructions)
        {
            var scannerEffect = Helpers.Field(typeof(RM), "scannerEffect");

            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(x => x.LoadsField(scannerEffect)))
                .CloneInPlace(out var scan)
                .MatchForward(false, new CodeMatch(static x => x.opcode == OpCodes.Ldarg_0))
                .AddLabels(scan.Labels)
                .CloneInPlace(out var ld)
                .RemoveInstructionsInRange(scan.Pos, ld.Pos - 1)
                .InstructionEnumeration();
        }
    }

}
