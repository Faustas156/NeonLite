using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MelonLoader;

namespace NeonLite.Modules.Misc
{
    // ORIGINAL CODE BY PUPPYPOWERTOOLS AUTHOR HECATE/PANDORAS FOX
    // this used to just set the powerprefs variable but powerprefs is basically entirely gone in the xbox release
    // so we have to use a transpiler here instead
    internal class RushSeed : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static MelonPreferences_Entry<int> setting;

        static void Setup()
        {
            setting = Settings.Add(Settings.h, "Misc", "rushSeed", "Level Rush Seed", "Negative is random.", -1);
            setting.OnEntryValueChanged.Subscribe((before, after) =>
            {
                if (before < 0 && after >= 0)
                    Activate(true);
                else if (before >= 0 && after < 0)
                    Activate(false);
            });
            active = setting.Value >= 0;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(LevelRushStats), "RandomizeIndex");

        static void Activate(bool activate)
        {
            if (activate)
                Patching.AddPatch(original, PutInSeed, Patching.PatchTarget.Transpiler);
            else
                Patching.RemovePatch(original, PutInSeed);
            active = activate;
        }

        static IEnumerable<CodeInstruction> PutInSeed(IEnumerable<CodeInstruction> instructions)
        {
            //var ctor = AccessTools.Constructor(typeof(Random));
            var getter = AccessTools.PropertyGetter(typeof(MelonPreferences_Entry<int>), "Value");

            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Newobj)
                {
                    yield return new(OpCodes.Pop);
                    yield return CodeInstruction.LoadField(typeof(RushSeed), "setting");
                    yield return new(OpCodes.Call, getter);
                }
                yield return code;
            }
        }

    }
}
