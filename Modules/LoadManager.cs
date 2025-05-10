using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NeonLite.Modules
{
    public class LoadManager : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        public static LevelData currentLevel;

        internal static Dictionary<Type, MethodInfo> modules = [];

        static void Activate(bool _)
        {
            AddModules(NeonLite.modules);

            var method = Helpers.Method(typeof(Game), "LevelSetupRoutine");
            Patching.AddPatch(method, Helpers.HM(SetCurrentLevel).SetPriority(Priority.First), Patching.PatchTarget.Prefix);
            method = method.MoveNext();
            Patching.AddPatch(method, Helpers.HM(AddLoadCall).SetPriority(Priority.First), Patching.PatchTarget.Transpiler);

            Patching.AddPatch(typeof(Game), "QuitToTitle", PreTitle, Patching.PatchTarget.Prefix);
            Patching.AddPatch(typeof(MenuScreenLoading), "LoadScene", Helpers.HM(PostMenuLoad).SetPriority(Priority.First), Patching.PatchTarget.Postfix);
        }

        public static void AddModules(IEnumerable<Type> modules) => modules.Where(t => Helpers.Method(t, "OnLevelLoad") != null).Do(x => LoadManager.modules.Add(x, Helpers.Method(x, "OnLevelLoad")));

        static float savedTimescale;
        static void DoTimescale()
        {
            NeonLite.Logger.DebugMsg("DoTimescale");

            if (!RM.time)
                return;
            savedTimescale = RM.time.GetTargetTimeScale();
            RM.time.SetTargetTimescale(0, true);
        }
        static void ResetTimescale()
        {
            NeonLite.Logger.DebugMsg("ResetTimescale");

            if (RM.time)
                RM.time?.SetTargetTimescale(savedTimescale, true);
        }

        static void SetCurrentLevel(LevelData newLevel) => currentLevel = newLevel; 

        static IEnumerable<CodeInstruction> AddLoadCall(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            IEnumerable<CodeInstruction> Transpiler()
            {
                List<Label> switchLabels = [];
                List<Label> mminstLabels = [];
                List<int> setStates = [];
                List<int> mmInsts = [];
                List<bool> staging = [];

                var inst = instructions.ToArray();

                int i = 0;

                int stateToSetStart = 0;

                FieldInfo state = null;
                FieldInfo current = null;

                var setState = Helpers.Method(typeof(MainMenu), "SetState");
                foreach (var code in inst)
                {
                    if (code.Calls(setState))
                    {
                        //NeonLite.Logger.Msg($"setStates {i}");

                        setStates.Add(i);
                    }
                    ++i;
                }

                var instance = Helpers.Method(typeof(MainMenu), "Instance");
                foreach (var idx in setStates)
                {
                    var wholeCall = inst.Reverse()
                        .Skip(inst.Length - idx)
                        .TakeWhile(x => !x.Calls(instance))
                        .Reverse().ToArray();

                    //NeonLite.Logger.Msg($"wholeCall {wholeCall.Length}");

                    bool mark = false;

                    if (wholeCall[0]?.operand == null || !Enum.IsDefined(typeof(MainMenu.State), (int)(sbyte)wholeCall[0].operand))
                        continue;

                    switch ((MainMenu.State)(int)(sbyte)wholeCall[0].operand)
                    {
                        case MainMenu.State.Map:
                            mark = wholeCall[2].LoadsConstant(0);
                            break;
                        case MainMenu.State.Staging:
                            mark = true;
                            break;
                        default:
                            break;
                    }
                    if (mark)
                    {
                        //NeonLite.Logger.Msg($"mark {idx} {idx - wholeCall.Length - 1}");
                        mmInsts.Add(idx - wholeCall.Length - 1);
                        mminstLabels.Add(generator.DefineLabel());
                        switchLabels.Add(generator.DefineLabel());
                        staging.Add((MainMenu.State)(int)(sbyte)wholeCall[0].operand == MainMenu.State.Staging);
                    }
                }

                i = 0;

                foreach (var code in inst)
                {
                    if (state == null && code.opcode == OpCodes.Ldfld)
                    {
                        state = (FieldInfo)code.operand;
                        var prop = AccessTools.FirstProperty(state.DeclaringType, _ => true);
                        var getter = prop.GetGetMethod(true);
                        current = (FieldInfo)PatchProcessor.ReadMethodBody(getter).First(kv => kv.Value != null).Value;
                    }

                    if (code.opcode == OpCodes.Switch && stateToSetStart == 0)
                    {
                        var jumptable = (Label[])code.operand;
                        stateToSetStart = jumptable.Length;

                        yield return new(OpCodes.Switch, jumptable.Concat(switchLabels).ToArray());
                    }
                    else if (code.Calls(instance) && mmInsts.IndexOf(i) != -1)
                    {
                        // RM.time.SetTargetTimescale(0, true)
                        yield return CodeInstruction.Call(typeof(LoadManager), "DoTimescale").MoveLabelsFrom(code);

                        // this.current = LoadManager.HandleLoads()
                        // this.state = stateToSet
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                        yield return new CodeInstruction(OpCodes.Dup); // this, this
                        yield return CodeInstruction.Call(typeof(LoadManager), "HandleLoads"); // this, this, handleloads
                        yield return new CodeInstruction(OpCodes.Stfld, current); // this
                        yield return new CodeInstruction(OpCodes.Ldc_I4, stateToSetStart++); // this, statetoset
                        yield return new CodeInstruction(OpCodes.Stfld, state); // -empty-

                        // return true
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return new CodeInstruction(OpCodes.Ret);

                        yield return CodeInstruction.Call(typeof(LoadManager), "ResetTimescale").WithLabels([mminstLabels[mmInsts.IndexOf(i)]]);
                        yield return code;
                    }
                    else
                        yield return code;
                    ++i;
                }

                for (i = 0; i < mmInsts.Count; ++i)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels([switchLabels[i]]);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_M1);
                    yield return new CodeInstruction(OpCodes.Stfld, state);
                    yield return new CodeInstruction(OpCodes.Br, mminstLabels[i]);
                }
            }

            //foreach (var c in Transpiler())
            //    NeonLite.Logger.Msg(c);
            return Transpiler();
        }

        static bool quittingToTitle;
        static void PreTitle() => quittingToTitle = true;

        static IEnumerator PostMenuLoad(IEnumerator __result)
        {
            while (__result.MoveNext())
                yield return __result.Current;
            if (!quittingToTitle)
                yield break;
            quittingToTitle = false;
            currentLevel = null;
            yield return HandleLoads();
        }

        public static IEnumerator HandleLoads()
        {
            NeonLite.Logger.DebugMsg("HandleLoads");

            Queue<MethodInfo> retries = [];
            foreach (var kv in modules)
            {
                var module = kv.Key;
                if (!(bool)AccessTools.Field(module, "active").GetValue(null))
                    continue;

                NeonLite.Logger.DebugMsg($"{module} OnLevelLoad");

                Helpers.StartProfiling($"{module} OLL");

                try
                {
                    var ret = kv.Value.Invoke(null, [currentLevel]);
                    if (ret != null && !(bool)ret)
                    {
                        NeonLite.Logger.DebugMsg($"{module} returned false, trying again later");

                        retries.Enqueue(kv.Value);
                    }
                }
                catch (Exception e)
                {
                    NeonLite.Logger.Error($"error in {module} OnLevelLoad:");
                    NeonLite.Logger.Error(e);
                }

                Helpers.EndProfiling();
            }
            NeonLite.Logger.DebugMsg($"Retries: {retries.Count}");

            if (retries.Count == 0)
                yield break;

            NeonLite.Logger.DebugMsg("Stalling load...");

            while (retries.Count > 0)
            {
                yield return null;
                int c = retries.Count;
                for (int i = 0; i < c; ++i)
                {
                    var method = retries.Dequeue();

                    NeonLite.Logger.DebugMsg($"{method.DeclaringType} OnLevelLoad");
                    Helpers.StartProfiling($"{method.DeclaringType} OLL");

                    try
                    {
                        var ret = method.Invoke(null, [currentLevel]);
                        if (!(bool)ret)
                            retries.Enqueue(method);
                    }
                    catch (Exception e)
                    {
                        NeonLite.Logger.Error($"error in {method.DeclaringType} OnLevelLoad:");
                        NeonLite.Logger.Error(e);
                    }

                    Helpers.EndProfiling();
                }
            }
        }
    }
}
