using HarmonyLib;
using System.Reflection;

#pragma warning disable CS0414

namespace NeonLite.Modules.UI.Toggles
{
    // these are all super small so into 1 file they go

    internal class HidePortrait : IModule
    {
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI/In-game", "noPortrait", "Disable player portrait", "Disables the bottom-left player portrait.", false);
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static void Activate(bool activate) => active = activate;
        static void OnLevelLoad(LevelData level)
        {
            if (!level || level.type == LevelData.LevelType.Hub || level.type == LevelData.LevelType.None)
                return;
            RM.ui.portraitUI.gameObject.SetActive(false);
        }
    }
    internal class HideBackstory : IModule
    {
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI/In-game", "noBackstory", "Disable player backstory", "Disables the bottom-left player backstory.", false);
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static void Activate(bool activate) => active = activate;
        static void OnLevelLoad(LevelData level)
        {
            if (!level || level.type == LevelData.LevelType.Hub)
                return;
            RM.ui.portraitUI.transform.parent.Find("Backstory").gameObject.SetActive(false);
        }
    }
    internal class HideBottomBar : IModule
    {
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI/In-game", "noFlames", "Disable bottom bar", "Disables the bottom flames.", false);
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static void Activate(bool activate) => active = activate;
        static void OnLevelLoad(LevelData level)
        {
            if (!level || level.type == LevelData.LevelType.Hub)
                return;
            RM.ui.transform.Find("Overlays/BottomBar").gameObject.SetActive(false);
        }
    }
    internal class HideDamage : IModule
    {
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI/In-game", "noWarning", "Disable low HP overlay", "Disables the red overlay for low HP.", false);
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static void Activate(bool activate) => active = activate;
        static void OnLevelLoad(LevelData level)
        {
            if (!level || level.type == LevelData.LevelType.Hub)
                return;
            RM.ui.transform.Find("Overlays/DamageOverlay").gameObject.SetActive(false);
        }
    }

    internal class NoCRT : IModule
    {
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI", "noCRT", "Disable CRT in menus", "Disables the CRT-like effect in menus.", false);
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static void Activate(bool activate)
        {
            if (activate)
                Patching.TogglePatch(activate, typeof(CRTRendererFeature.CRTEffectPass), "Execute", StopCRT, Patching.PatchTarget.Prefix);
            else
                Patching.RemovePatch(typeof(CRTRendererFeature.CRTEffectPass), "Execute", StopCRT);

            active = activate;
        }

        static bool StopCRT() => false;
    }

}
