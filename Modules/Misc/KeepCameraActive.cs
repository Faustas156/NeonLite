using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NeonLite.Modules.Misc
{
    internal class KeepCameraActive : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static void Activate(bool _)
        {
            Patching.AddPatch(typeof(MainMenu), "SetState", WhenTitleIsReady, Patching.PatchTarget.Prefix);
            Patching.AddPatch(typeof(MainMenu), "SetState", Transpiler, Patching.PatchTarget.Transpiler);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int skip = 0;
            var crtCamera = AccessTools.Field(typeof(MainMenu), "CRTCamera");

            foreach (var code in instructions)
            {
                if (code.LoadsField(crtCamera))
                {
                    yield return code;
                    yield return new(OpCodes.Ldc_I4_1);
                    skip = 3;
                }
                else if (--skip < 0)
                    yield return code;
            }
        }

        static void WhenTitleIsReady(MainMenu.State newState)
        {
            if (newState == MainMenu.State.Title)
                NeonLite.mmHolder.GetComponent<CanvasGroup>().alpha = 1;
        }

    }
}
