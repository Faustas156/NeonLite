using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace NeonLite.Modules.UI
{
    internal class TimerColor : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static bool hit = false;

        static MelonPreferences_Entry<Color> setting;

        static void Setup()
        {
            setting = Settings.Add(Settings.h, "UI/In-game", "timerColor", "In-game Timer Color", "Set alpha to 0 to disable.", Color.white);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after.a != 0));
            active = setting.Value.a != 0;
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(PlayerUI), "UpdateTimerText");
        static void Activate(bool activate)
        {
            if (activate)
                Patching.AddPatch(original, OnTimerUpdate, Patching.PatchTarget.Postfix);
            else
                Patching.RemovePatch(original, OnTimerUpdate);

            active = activate;
        }

        static void OnTimerUpdate(ref PlayerUI __instance) => __instance.timerText.color = setting.Value;
    }
}
