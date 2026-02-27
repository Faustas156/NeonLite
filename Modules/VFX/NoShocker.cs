using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

#pragma warning disable CS0414

namespace NeonLite.Modules.VFX
{
    // ORIGINAL CODE BY PUPPYPOWERTOOLS AUTHOR HECATE/PANDORAS FOX
    [Module]
    internal static class NoShocker
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noShocker", "Disable shocker overlay", "Disable the white flash from shockers, including the explosion.", false);
            active = setting.SetupForModule(Activate, static (_, after) => after);
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
            var exploder = Helpers.Field(typeof(RM), "exploder");
            var ui = Helpers.Field(typeof(RM), "ui");

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
