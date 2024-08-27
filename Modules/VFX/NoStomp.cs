using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.Rendering;

namespace NeonLite.Modules.VFX
{
    // ORIGINAL CODE BY PUPPYPOWERTOOLS AUTHOR HECATE/PANDORAS FOX
    internal class NoStomp : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static bool hit = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noStomp", "Disable stomp splashbang", "Disable the white flash and explosion that appear when you land a stomp.", false);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(MechController), "DoStompAbility");
        static void Activate(bool activate)
        {
            if (activate)
                NeonLite.Harmony.Patch(original, transpiler: Helpers.HM(StopFX));
            else
                NeonLite.Harmony.Unpatch(original, Helpers.MI(StopFX));

            active = activate;
        }

        static IEnumerable<CodeInstruction> StopFX(IEnumerable<CodeInstruction> instructions)
        {
            OpCode stopUntil = OpCodes.Nop;
            CodeInstruction storage = null;
            var exploder = AccessTools.Field(typeof(RM), "exploder");
            var scannerEffect = AccessTools.Field(typeof(RM), "scannerEffect");

            foreach (var code in instructions)
            {
                if (code.LoadsField(exploder))
                {
                    storage = code;
                    stopUntil = OpCodes.Ldstr;
                }
                else if (code.LoadsField(scannerEffect))
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
