using HarmonyLib;
using MelonLoader;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace NeonLite.Modules.Optimization
{
    [HarmonyPatch]
    internal class MultiLoad : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        public static readonly HashSet<AsyncOperation> operations = [];
        static bool skip = false;

        public static MelonPreferences_Entry<bool> setting;

        static void Setup()
        {
            setting = Settings.Add(Settings.h, "Misc", "multiLoad", "MultiLoad", "Modify the level loading to load multiple scenes at once. Can help with load times.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static void Activate(bool activate)
        {
            if (activate)
                SuperRestart.setting.Value = false;
            active = activate;
        }

        [HarmonyPatch(typeof(Game), "LevelSetupRoutine", MethodType.Enumerator)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> InsertSkips(IEnumerable<CodeInstruction> instructions)
        {
            int hit = 0;
            int[] skipOn = [3, 6, 8];
            int[] skipOff = [4, 7, 9, 10];
            var start = AccessTools.Method(typeof(MonoBehaviour), "StartCoroutine", [typeof(IEnumerator)]);
            foreach (var code in instructions)
            {
                if (code.Calls(start))
                {
                    if (skipOn.Contains(++hit))
                    {
                        yield return new(OpCodes.Ldc_I4_1);
                        yield return CodeInstruction.StoreField(typeof(MultiLoad), "skip");
                    }
                    else if (skipOff.Contains(hit))
                    {
                        yield return new(OpCodes.Ldc_I4_0);
                        yield return CodeInstruction.StoreField(typeof(MultiLoad), "skip");
                    }
                }
                yield return code;
            }
        }

        [HarmonyPatch(typeof(MenuScreenLoading), "LoadScene", MethodType.Enumerator)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> InsertStall(IEnumerable<CodeInstruction> instructions)
        {
            var isDone = AccessTools.PropertyGetter(typeof(AsyncOperation), "isDone");
            foreach (var code in instructions)
            {
                if (code.Calls(isDone))
                    yield return CodeInstruction.Call(typeof(MultiLoad), "AllDone");
                else
                    yield return code;
            }
        }

        static bool AllDone(AsyncOperation current)
        {
            if (!current.isDone)
                operations.Add(current);
            operations.RemoveWhere(o => o.isDone);

            return (active && skip) || operations.Count == 0;
        }


    }
}
