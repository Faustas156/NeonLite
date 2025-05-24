using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace NeonLite.Modules.Optimization
{
    internal class ScrollFix : IModule
    {
        const bool priority = true;
        static bool active = true;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Optimization", "scrollFix", "Fix Scroll Wheel Coyote", "Fixes the weird camera jitter that happens on some systems when you use scroll wheel to coyote.", true);
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(FirstPersonDrifter), "Update", AddTryCatch, Patching.PatchTarget.Transpiler);
            active = activate;
        }

        static IEnumerable<CodeInstruction> AddTryCatch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            return new CodeMatcher(instructions, generator)
                .MatchForward(true, new CodeMatch(static x => x.Calls(Helpers.Method(typeof(GameInput), "GetButton")))) // go to get button
                .MatchBack(true, new CodeMatch(static x => x.opcode == OpCodes.Ldarg_0)) // go back to this
                .Do(m => m.Blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock))) // add exception block
                .MatchForward(true, new CodeMatch(static x => x.opcode == OpCodes.Stfld)) // go to stfld
                .CloneInPlace(out var stfld)
                .Advance(1)
                .CreateLabel(out var end) // create the end label
                .Insert(
                    new CodeInstruction(OpCodes.Leave, end), // add a leave
                    new CodeInstruction(OpCodes.Pop).WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock)), // start the catch
                    //new CodeInstruction(OpCodes.Ldc_I4_0), // store false if goes wrong
                    //stfld.Instruction,
                    new CodeInstruction(OpCodes.Leave, end).WithBlocks(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock)) // add a leave for the catch
                 )
                .InstructionEnumeration();
        }
    }
}
