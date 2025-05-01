using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

#pragma warning disable CS0414

namespace NeonLite.Modules.Misc.VFX
{
    // ORIGINAL CODE BY PUPPYPOWERTOOLS AUTHOR HECATE/PANDORAS FOX
    internal class NoShocker : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noShocker", "Disable shocker overlay", "Disable the white flash from shockers, including the explosion.", false);
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(ShockWeapon), "DoShock", StopFX, Patching.PatchTarget.Transpiler);

            active = activate;
        }

        static IEnumerable<CodeInstruction> StopFX(IEnumerable<CodeInstruction> instructions)
        {
            OpCode stopUntil = OpCodes.Nop;
            CodeInstruction storage = null;
            var exploder = AccessTools.Field(typeof(RM), "exploder");
            var ui = AccessTools.Field(typeof(RM), "ui");

            foreach (var code in instructions)
            {
                if (code.LoadsField(exploder))
                {
                    storage = code;
                    stopUntil = OpCodes.Ldstr;
                }
                else if (code.LoadsField(ui))
                {
                    storage = code;
                    stopUntil = OpCodes.Ldarg_0;
                }
                else if (stopUntil == OpCodes.Nop || stopUntil == code.opcode)
                {
                    stopUntil = OpCodes.Nop;
                    if (storage != null)
                        yield return code.MoveLabelsFrom(storage);
                    else
                        yield return code;
                    storage = null;
                }
            }
        }
    }
}
