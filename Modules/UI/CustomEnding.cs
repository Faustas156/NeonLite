using HarmonyLib;
using MelonLoader;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLite.Modules.UI
{
    internal class CustomEnding : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static bool firstLoad = true;
        static MelonPreferences_Entry<string> setting;
        static Sprite cache;

        static void Setup()
        {
            setting = Settings.Add("NeonLite", "UI", "endingImage", "Custom ending image", "Set a custom in-game ending image of White by entering the path to a local image.\nMake sure to remove quotes!", "", null);
            active = setting.SetupForModule(Activate, static (_, after) => after != "");
        }

        static void Activate(bool activate)
        {
            if (activate && (!active || firstLoad))
                Patching.AddPatch(typeof(MenuScreenResults), "OnSetVisible", PostSetVisible, Patching.PatchTarget.Postfix);
            else if (!activate)
                Patching.RemovePatch(typeof(MenuScreenResults), "OnSetVisible", PostSetVisible);

            cache = null;
            active = activate;
            firstLoad = false;
        }

        static void PostSetVisible(MenuScreenResults __instance)
        {
            Image character = __instance.characterImage;

            string path = setting.Value;
            NeonLite.Logger.DebugMsg(path);

            if (!cache)
            {
                if (!File.Exists(path))
                    return;

                var file = File.ReadAllBytes(path);
                cache = Helpers.LoadSprite(file, wrapMode: TextureWrapMode.Repeat, pivot: character.sprite.pivot / character.sprite.rect.size);
            }

            character.sprite = cache;
        }
    }
}