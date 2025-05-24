using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

#pragma warning disable CS0414

namespace NeonLite.Modules.Optimization
{
    internal class CleanTexts : IModule
    {
        const bool priority = true;
        const bool active = true;

        static readonly FieldInfo textList = Helpers.Field(typeof(AxKLocalizedTextLord), "m_localizedTexts");
        static void OnLevelLoad(LevelData _)
        {
            var list = (List<AxKLocalizedTextObject_Interface>)textList.GetValue(AxKLocalizedTextLord.GetInstance());
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(null))
                {
                    list.RemoveAt(i);
                    --i;
                }
            }
        }
    }

    internal class BetterProgressBar : IModule
    {
        const bool priority = true;
        const bool active = true;
        
        static void Activate(bool _)
        {
            Patching.AddPatch(typeof(ProgressBar), "OnEnable", NoEnable, Patching.PatchTarget.Prefix);
            Patching.AddPatch(typeof(ProgressBar), "SetBarVisibility", UpdateOnVisible, Patching.PatchTarget.Prefix);
        }

        static bool NoEnable() => false;

        static void UpdateOnVisible(ProgressBar __instance, bool ____isVisible, bool visible)
        {
            if (!____isVisible)
                if (visible)
                    UpdateManager.SubscribeToUpdate(__instance);
            else if (!visible)
                    UpdateManager.UnsubscribeFromUpdate(__instance);
        }
    }

    internal class BetterFog : IModule
    {
        const bool priority = true;
        const bool active = true;

        static void Activate(bool _)
        {
            Patching.AddPatch(typeof(Setup), "ApplyHeightFogMat", FogOverride, Patching.PatchTarget.Prefix);
        }

        static bool FogOverride(Setup __instance, ref bool __result)
        {
            // the og function calls Object.FindObjectsOfType<HeightFogGlobal>() a fucking ABSURD amount of times
            // this boosts it in TTT by like way too many ms at least like 30
            if (!__instance.HasFogPreset())
                __result = false;
            else
            {
                var fogs = GameObject.FindObjectsOfType<HeightFogGlobal>();
                if (fogs.Length <= 0)
                    __result = false;
                else
                {
                    __result = true;
                    foreach (var f in fogs)
                        f.UpdateFogPreset(__instance.heightFogPreset);
                }
            }

            return false;
        }
    }

    internal class PreloadObjects : IModule
    {
        const bool priority = true;
        const bool active = true;

        static void Activate(bool _)
        {
            Patching.AddPatch(Helpers.Method(typeof(ObjectSpawner), "Spawn", [typeof(float)]), Preload, Patching.PatchTarget.Prefix);
        }

        static void Preload(ObjectSpawner __instance)
        {
            switch (__instance._objectType)
            {
                case ObjectSpawner.Type.ExplosiveBarrel:
                    Utils.PreloadFromResources("ExplosiveBarrel");
                    break;
                case ObjectSpawner.Type.BreakablePlatform:
                    Utils.PreloadFromResources("BreakablePlatform");
                    break;
                case ObjectSpawner.Type.EnvironmentPortal:
                    Utils.PreloadFromResources("EnvironmentPortal");
                    break;
            }
        }
    }

    internal class EarlyLBUpload : IModule
    {
        const bool priority = true;
        const bool active = true;

        static void Activate(bool _)
        {
            Patching.AddPatch(typeof(MenuScreenResults), "LevelCompleteRoutine", SetLevelFirst, Patching.PatchTarget.Postfix);
            Patching.AddPatch(Helpers.Method(typeof(MenuScreenResults), "LevelCompleteRoutine").MoveNext(), SkipOverLevel, Patching.PatchTarget.Transpiler);
        }

        static IEnumerator SetLevelFirst(IEnumerator __result, MenuScreenResults __instance)
        {
            if (!LevelRush.IsLevelRush())
            {
                var level = NeonLite.Game.GetCurrentLevel();
                __instance.leaderboardsAndLevelInfoRef.SetLevel(level, false, true, false, !GameDataManager.GetLevelStats(level.levelID).IsNewBest());
            }

            while (__result.MoveNext())
                yield return __result.Current;
        }

        static IEnumerable<CodeInstruction> SkipOverLevel(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(true, new CodeMatch(static x => x.Calls(Helpers.Method(typeof(LeaderboardsAndLevelInfo), "SetLevel"))))
                .MatchBack(true, new CodeMatch(static x => x.Branches(out _)))
                .CloneInPlace(out var branch)
                .Advance(1)
                .Insert(new CodeInstruction(OpCodes.Br, branch.Operand))
                .InstructionEnumeration();
        }
    }
}
