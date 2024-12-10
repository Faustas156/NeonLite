using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace NeonLite.Modules.UI
{
    // ORIGINAL CODE BY MADMON FOR CUSTOMCROSSHAIRCOLORS
    internal class CrosshairColors : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static MelonPreferences_Entry<Color> mainColor;
        static MelonPreferences_Entry<Color> telefragOn;
        static MelonPreferences_Entry<Color> telefragOff;
        static MelonPreferences_Entry<Color> overheat;
        static MelonPreferences_Entry<Color> ziplineInner;
        static MelonPreferences_Entry<Color> ziplineOuter;
        static MelonPreferences_Entry<Color> ziplineOn;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Crosshair", "enabled", "Crosshair Colors", "Enables setting custom crosshair colors.", false);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;

            mainColor = Settings.Add(Settings.h, "Crosshair", "main", "Main Color", null, Color.gray);
            overheat = Settings.Add(Settings.h, "Crosshair", "overheat", "Overheat", "Purify/stomp/fireball hexagon", Color.grey);
            ziplineInner = Settings.Add(Settings.h, "Crosshair", "zipInner", "Inner Zipline Hexagon", "Requires level restart.", Color.grey);
            ziplineOuter = Settings.Add(Settings.h, "Crosshair", "zipOuter", "Outer Zipline Hexagon", "Requires level restart.", Color.white);
            ziplineOn = Settings.Add(Settings.h, "Crosshair", "zipLocked", "\"Locked On\" Zipline Color", "Requires level restart.", Color.green);
            telefragOn = Settings.Add(Settings.h, "Crosshair", "teleOn", "Book of Life Active", null, new Color(1f, 0f, 0.4308f, 1f));
            telefragOff = Settings.Add(Settings.h, "Crosshair", "teleOff", "Book of Life Inactive", null, Color.grey);
        }

        static readonly MethodInfo ogload = AccessTools.Method(typeof(UIAbilityIndicator_Zipline), "Start");

        static void Activate(bool activate)
        {
            if (activate)
                Patching.AddPatch(ogload, SetIndicatorUI, Patching.PatchTarget.Postfix);
            else
                Patching.RemovePatch(ogload, SetIndicatorUI);

            active = activate;
        }

        public static void SetIndicatorUI(ref Color ____colorGray, ref Color ____colorWhite, ref Color ____colorGreen)
        {
            ____colorGray = ziplineInner.Value;
            ____colorWhite = ziplineOuter.Value;
            ____colorGreen = ziplineOn.Value;
        }

        static void OnLevelLoad(LevelData level)
        {
            if (level == null || level.type == LevelData.LevelType.Hub)
                return;

            RM.ui.crosshair.GetComponent<MeshRenderer>().material.color = mainColor.Value;
            RM.ui._telefragIndicator.indicatorUIOn.GetComponent<MeshRenderer>().material.color = telefragOn.Value;
            RM.ui._telefragIndicator.indicatorUIOff.GetComponent<MeshRenderer>().material.color = telefragOff.Value;
            RM.ui.crosshairOverheatIndicator.GetComponent<MeshRenderer>().material.color = overheat.Value;
        }

    }
}
