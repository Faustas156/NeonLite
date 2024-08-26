#if !XBOX && DEBUG
using System.Reflection;
using HarmonyLib;

namespace NeonLite.Modules.Optimization
{
    internal class UpdateGlobal : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;
        
        static bool ready;

        static void Setup()
        {
            var setting = Settings.Add("NeonLite", "Misc", "updateGlobal", "Auto-update Global", "Updates your Global Neon Rank the instant you PB a stage.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo oglbupld = AccessTools.Method(typeof(Leaderboards), "OnLeaderboardUploaded");
        static readonly MethodInfo original = AccessTools.Method(typeof(LeaderboardIntegrationSteam), "SetupLeaderboardForLevel");

        static void Activate(bool activate)
        {
            if (activate)
            {
                NeonLite.Harmony.Patch(oglbupld, Helpers.HM(PreLBUploaded));
                NeonLite.Harmony.Patch(original, Helpers.HM(ChangeCallback));
            }
            else
            {
                NeonLite.Harmony.Unpatch(oglbupld, Helpers.MI(PreLBUploaded));
                NeonLite.Harmony.Unpatch(original, Helpers.MI(ChangeCallback));
            }

            active = activate;
        }

        static void PreLBUploaded() => ready = true;

        static void ChangeCallback(ref LeaderboardIntegrationSteam.LeaderboardLoadedCallback newCallback)
        {
            if (!ready)
                return;

            ready = false;
            LeaderboardIntegrationSteam.LeaderboardLoadedCallback cb = newCallback;
            newCallback = (result, offline) =>
            {
                cb?.Invoke(result, offline);
                LeaderboardIntegrationSteam.UploadScore_GlobalNeonRank(null, (result, _) =>
                {
                    if (result)
                        NeonLite.Logger.Msg("Updated global!");
                    else
                        NeonLite.Logger.Warning("Failed to update global.");
                });
            };
        }
    }
}
#endif