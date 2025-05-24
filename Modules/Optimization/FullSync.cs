using HarmonyLib;
using System;
using System.Collections.Generic;

namespace NeonLite.Modules.Optimization
{
    internal class FullSync : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Optimization", "fullSync", "FullSync", "Fixes some VSync specific issues at the start of some stages.\nPorted into NeonLite from its own mod.", true);
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, Helpers.Method(typeof(Game), "LevelSetupRoutine").MoveNext(), LevelSetupPatches, Patching.PatchTarget.Transpiler);

            active = activate;
        }

        static IEnumerable<CodeInstruction> LevelSetupPatches(IEnumerable<CodeInstruction> instructions)
        {
            int hits = 0;
            var closeConsole = AccessTools.Method(typeof(GameConsole), "CloseConsole");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Calls(closeConsole) && ++hits == 4)
                    yield return Transpilers.EmitDelegate(static () => RM.time.SetTargetTimescale(0, true));
            }
        }
    }
}
