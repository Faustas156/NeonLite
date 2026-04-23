using I2.Loc;

namespace NeonLite.Modules.UI
{
    [Module]
    internal static class ShowLevelName
    {
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI", "showLevelName", "Show Level Name", "Displays the level name when you finish a level.", true);
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(MenuScreenResults), "OnSetVisible", PostSetVisible, Patching.PatchTarget.Postfix);
            active = activate;
        }

        static void PostSetVisible(MenuScreenResults __instance)
        {
            if (LevelRush.IsLevelRush())
                return;
            var split = __instance.levelComplete_Localized.textMeshProUGUI.text.Split();
            var level = NeonLite.Game.GetCurrentLevel();
            var name = LocalizationManager.GetTranslation(level.GetLevelDisplayName());
            if (string.IsNullOrEmpty(name))
                name = level.levelDisplayName;
            if (split.Length > 1)
                name = "  " + name;
            var list = split.ToList();
            var str = $"<nobr><alpha=#AA><size=40%><noparse>{name}</noparse></size></nobr><alpha=#FF><br>";
            if (split.Length < 2)
                list.Insert(list.Count - 1, str);
            else
                list[list.Count - 2] += str;
            __instance.levelComplete_Localized.textMeshProUGUI.text = string.Join("", list);
        }
    }
}
