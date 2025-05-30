﻿using MelonLoader;
using System.IO;
using UnityEngine;

namespace NeonLite.Modules.UI
{
    internal class CustomPortrait : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static MelonPreferences_Entry<string> setting;
        static Texture2D cache;

        static void Setup()
        {
            setting = Settings.Add(Settings.h, "UI/In-game", "portrait", "Custom portrait", "Set a custom in-game portrait by entering the path to a local image.\nMake sure to remove quotes!", "");
            active = setting.SetupForModule(Activate, static (_, after) => after != "");
        }

        static void Activate(bool activate)
        {
            cache = null;
            active = activate;
        }
        static void OnLevelLoad(LevelData level)
        {
            if (!level || level.type == LevelData.LevelType.Hub)
                return;

            var uiPortrait = RM.ui.portraitUI;
            if (!uiPortrait)
                return;

            var portraitImg = uiPortrait.playerHolder.GetComponentInChildren<MeshRenderer>();
            var path = setting.Value;
            if (!cache)
            {
                if (!File.Exists(path))
                    return;

                var imgBytes = File.ReadAllBytes(path);
                cache = Helpers.LoadTexture(imgBytes);
            }

            portraitImg.material.mainTexture = cache;
        }
    }
}
