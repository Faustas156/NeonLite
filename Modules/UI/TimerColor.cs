using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace NeonLite.Modules.UI
{
    [Module]
    internal static class TimerColor
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static MelonPreferences_Entry<Color> setting;

        static void Setup()
        {
            setting = Settings.Add(Settings.h, "UI/In-game", "timerColor", "In-game Timer Color", "Set alpha to 0 to disable.", Color.white);
            active = setting.SetupForModule(Activate, static (_, after) => after.a != 0);
        }

        static void Activate(bool activate)
        {
            if (activate)
                Patching.TogglePatch(activate, typeof(PlayerUI), "UpdateTimerText", OnTimerUpdate, Patching.PatchTarget.Postfix);
            else
                Patching.RemovePatch(typeof(PlayerUI), "UpdateTimerText", OnTimerUpdate);

            active = activate;
        }

        static void OnTimerUpdate(ref PlayerUI __instance) => __instance.timerText.color = setting.Value;
    }
}
