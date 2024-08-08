﻿using ClockStone;

namespace NeonLite.Modules.Misc
{
    internal class AmbienceRemover : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "ambienceOff", "Ambience Remover", "Removes the ambience from the game, even when muted.", false);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static void Activate(bool activate) => active = activate;
        static void OnLevelLoad(LevelData _) => SingletonMonoBehaviour<AudioController>.Instance.ambienceSoundEnabled = false;
    }
}