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
            setting = Settings.Add("NeonLite", "UI/In-game", "ending", "Custom ending image", "Set a custom in-game ending image of White by entering the path to a local image (2048x2048).\nMake sure to remove quotes!", "", null);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after != ""));
            active = setting.Value != "";
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(MenuScreenResults), "OnSetVisible", null, null);

        static void Activate(bool activate)
        {
            if (activate && (!active || firstLoad))
                Patching.AddPatch(original, PostSetVisible, Patching.PatchTarget.Postfix);
            else if (!activate)
                Patching.RemovePatch(original, PostSetVisible);

            cache = null;
            active = activate;
            firstLoad = false;
        }

        static void PostSetVisible(MenuScreenResults __instance)
        {
            Image character = __instance.characterImage;

            string path = setting.Value;
            if (NeonLite.DEBUG)
                NeonLite.Logger.Msg(path);

            if (!File.Exists(path))
                return;

            if (!cache)
            {
                if (NeonLite.DEBUG)
                    NeonLite.Logger.Msg("build cache");
                var file = File.ReadAllBytes(path);
                var tex = LoadTexture(file);

                if (tex.width != character.sprite.texture.width || tex.height != character.sprite.texture.height)
                    return;

                tex.filterMode = FilterMode.Trilinear;
                tex.wrapModeW = TextureWrapMode.Repeat;
                cache = Sprite.Create(tex, character.sprite.rect, character.sprite.pivot);
            }

            if (NeonLite.DEBUG)
                NeonLite.Logger.Msg("set");

            character.sprite = cache;
        }

        private static Texture2D LoadTexture(byte[] image)
        {
            Texture2D texture2D = new(1, 1, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(texture2D, image, true);
            texture2D.wrapMode = TextureWrapMode.Clamp;
            return texture2D;
        }
    }
}