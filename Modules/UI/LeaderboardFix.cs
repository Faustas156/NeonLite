using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NeonLite.Modules.UI
{
#if !XBOX
    internal class LeaderboardFix : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static void Activate(bool _)
        {
            Patching.AddPatch(typeof(LeaderboardIntegrationSteam), "DownloadEntries", AddToScoreData, Patching.PatchTarget.Transpiler);
            Patching.AddPatch(typeof(LeaderboardIntegrationSteam), "OnLeaderboardScoresFriendsDownloaded", PatchSteamFriends, Patching.PatchTarget.Transpiler);
        }

        static IEnumerable<CodeInstruction> AddToScoreData(IEnumerable<CodeInstruction> instructions)
        {
            bool hit = false;
            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_I4_1 && !hit)
                {
                    hit = true;
                    yield return new(OpCodes.Ldarg_0);
                }
                else
                    yield return code;
            }
        }

        static IEnumerable<CodeInstruction> PatchSteamFriends(IEnumerable<CodeInstruction> instructions)
        {
            int ldHit = 0;
            int stHit = 0;

            object iLoc = null;
            object entryC = null;

            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Stloc_S && iLoc == null)
                    iLoc = code.operand;
                if (code.opcode == OpCodes.Ldfld && entryC == null)
                    entryC = code.operand;
                if (code.opcode == OpCodes.Ldc_I4_0 && ++ldHit == 6)
                    yield return new(OpCodes.Ldloc_S, iLoc); // don't load index 0 over and over, actually load i
                else if (code.opcode == OpCodes.Stloc_2 && ++stHit == 2)
                {
                    // add one to the index and set it
                    yield return new(OpCodes.Ldc_I4_1);
                    yield return new(OpCodes.Add);
                    yield return code;
                    // set i to the entrycount so we break out of the loop and don't waste our time
                    yield return new(OpCodes.Ldarg_0);
                    yield return new(OpCodes.Ldfld, entryC);
                    yield return new(OpCodes.Stloc_S, iLoc);
                }
                else
                    yield return code;
            }
        }
    }
#endif
}
