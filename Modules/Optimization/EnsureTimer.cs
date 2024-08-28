#if DEBUG
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace NeonLite.Modules.Optimization
{
    [HarmonyPatch]
    internal class EnsureTimer : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static bool skip = false;
        static bool next = false;

        static void Setup() { }

        static readonly FieldInfo levelSetup = AccessTools.Field(typeof(Game), "_levelSetup");

        static void Activate(bool activate) => NeonLite.Game.OnLevelLoadComplete += SetTrue;

        static void SetTrue()
        {
            next = false;
            skip = true;
        }

        [HarmonyPatch(typeof(LevelPlaythrough), "Update")]
        [HarmonyPrefix]
        static bool SkipFirstTimer(LevelPlaythrough __instance)
        {
            if (skip || next)
            {
                if (NeonLite.DEBUG)
                    NeonLite.Logger.Msg("resetting timer");
                __instance.Reset();
                next = false;
                return next;
            }
            return true;
        }
        [HarmonyPatch(typeof(FirstPersonDrifter), "UpdateVelocity")]
        [HarmonyPrefix]
        static void FPDUpdate()
        {
            if (skip)
            {
                if (NeonLite.DEBUG)
                    NeonLite.Logger.Msg("first parsed frame");
                next = true;
                skip = false;
                levelSetup.SetValue(NeonLite.Game, true);
            }
        }

        static void OnLevelLoad(LevelData _) => levelSetup.SetValue(NeonLite.Game, false);
    }
}
#endif