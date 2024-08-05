using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace NeonLite.Modules
{
    // forced to use annotations, enumerator patching just isn't working for some reason
    [HarmonyPatch(typeof(Game))]
    internal class LoadManager : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static List<Type> modules = [];

        static void Setup() { }
        static void Activate(bool _) => modules = NeonLite.modules.Where(t => AccessTools.Method(t, "OnLevelLoad") != null).ToList();

        [HarmonyPatch("LevelSetupRoutine", MethodType.Enumerator)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> AddLoadCall(IEnumerable<CodeInstruction> instructions)
        {
            bool hit = false;
            var mminst = AccessTools.Method(typeof(MainMenu), "Instance");
            foreach (var code in instructions)
            {
                if (code.Calls(mminst) && code.labels.Count != 0 && !hit)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1).MoveLabelsFrom(code);
                    yield return CodeInstruction.LoadField(typeof(Game), "_currentLevel");
                    yield return CodeInstruction.Call(typeof(LoadManager), "HandleLoads");
                }
                yield return code;
            }
        }

        [HarmonyPatch("QuitToTitle")]
        [HarmonyPrefix]
        static void PreTitle() => HandleLoads(null);

        static void HandleLoads(LevelData level)
        {
            foreach (var module in modules.Where(t => (bool)AccessTools.Field(t, "active").GetValue(null)))
            {
                if (NeonLite.DEBUG)
                    NeonLite.Logger.Msg($"{module} OnLevelLoad");

                try
                {
                    AccessTools.Method(module, "OnLevelLoad").Invoke(null, [level]);
                }
                catch (Exception e)
                {
                    NeonLite.Logger.Error($"error in {module} OnLevelLoad:");
                    NeonLite.Logger.Error(e);
                }
            }
        }
    }
}
