#if !XBOX
using HarmonyLib;
using MelonLoader;
using System.Reflection;

namespace NeonLite.Modules.Optimization
{
    internal class UpdateGlobal : IModule
    {
#pragma warning disable CS0414
        const bool priority = false;
        static bool active = false;

        static bool ready;
        static bool popupPrepped;
        static bool titleDone;

        static MelonPreferences_Entry<bool> setting;
        static MelonPreferences_Entry<bool> popup;

        static void Setup()
        {
            setting = Settings.Add("NeonLite", "Optimization", "updateGlobal", "Auto-update Global", "Updates your Global Neon Rank the instant you PB a stage.", true);
            popup = Settings.Add("NeonLite", "Optimization", "updateGlobalP", "Auto-update Global Popup", null, true, true);

            setting.IsHidden = true;
            return;
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(Leaderboards), "OnLeaderboardUploaded", Helpers.HM(PreLBUploaded), Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(LeaderboardIntegrationSteam), "SetupLeaderboardForLevel", Helpers.HM(ChangeCallback), Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(MenuScreenTitle), "OnSetVisible", Helpers.HM(OnTitleShow), Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(GameData), "GetGlobalNeonScore", Helpers.HM(NeonScoreDebug), Patching.PatchTarget.Prefix);

            if (activate)
            {
                if (titleDone)
                    DoPopup();
                else
                    popupPrepped = true;
            }

            active = activate;
        }

        static void PreLBUploaded() => ready = true;

        static void OnTitleShow()
        {
            if (!titleDone && popupPrepped)
                DoPopup();
            titleDone = true;
        }

        static void DoPopup()
        {
            if (!popup.Value)
            {
                MainMenu.Instance()._popup.SetPopup("NeonLite/AUTOGLOBAL_NOTICE", static () =>
                {
                    setting.Value = false;
                    MelonPreferences.Save();
                }, static () => { });
                popup.Value = true;
                MelonPreferences.Save();
            }
        }

        static void NeonScoreDebug(int __result) => NeonLite.Logger.Msg($"Calculated global microseconds: {__result}");

        static bool ChangeCallback(int ___previousUserRanking, LevelData newData, Leaderboards newRef, LeaderboardIntegrationSteam.LeaderboardLoadedCallback newCallback)
        {
            if (!ready)
                return true;

            ready = false;
            LeaderboardIntegrationSteam.UploadScore_GlobalNeonRank(null, (result, _) =>
            {
                if (result)
                    NeonLite.Logger.Msg("Updated global!");
                else
                    NeonLite.Logger.Warning("Failed to update global.");

                Helpers.Field(typeof(LeaderboardIntegrationSteam), "previousUserRanking").SetValue(null, ___previousUserRanking);
                LeaderboardIntegrationSteam.SetupLeaderboardForLevel(newData, newRef, newCallback);
            });
            return false;
        }
    }
}
#endif