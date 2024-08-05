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
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
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
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
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
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
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
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
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
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(CRTRendererFeature.CRTEffectPass), "Execute");
        static void Activate(bool activate)
        {
            if (activate)
                NeonLite.Harmony.Patch(original, prefix: Helpers.HM(StopCRT));
            else
                NeonLite.Harmony.Unpatch(original, Helpers.MI(StopCRT));

            active = activate;
        }

        static bool StopCRT() => false;
    }

}
