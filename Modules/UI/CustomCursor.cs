using HarmonyLib;
using MelonLoader;
using MelonLoader.Preferences;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLite.Modules.UI
{
    internal class CustomCursor : IModule
    {
#pragma warning disable CS0414
        const bool priority = false;
        static bool active = false;

        static MelonPreferences_Entry<string> setting;

        static MelonPreferences_Entry<float> scale;
        static MelonPreferences_Entry<float> rotation;

        static Sprite original;
        static Sprite cache;

        static void Setup()
        {
            setting = Settings.Add("NeonLite", "UI", "cursorImage", "Custom Cursor Image", "Set a custom cursor by entering the path to a local image.\nMake sure to remove quotes!", "", null);
            scale = Settings.Add("NeonLite", "UI", "cursorScale", "Cursor Scale", null, 1f, new ValueRange<float>(0, 5));
            rotation = Settings.Add("NeonLite", "UI", "cursorRot", "Cursor Rotation", null, 0f, new ValueRange<float>(0, 360));

            active = setting.SetupForModule(Activate, static (_, after) => after != "");
            scale.OnEntryValueChanged.Subscribe((_, _) => Activate(setting.Value != ""));
            rotation.OnEntryValueChanged.Subscribe((_, _) => Activate(setting.Value != ""));
        }

        static void Activate(bool activate)
        {
            string path = setting.Value;
            original ??= RM.Pointer.CursorImage.sprite;
            activate &= File.Exists(path);

            if (activate)
            {
                var file = File.ReadAllBytes(path);

                if (!cache || cache.name != path)
                {
                    cache = LoadSprite(file);
                    cache.name = path;
                }

                RM.Pointer.CursorImage.sprite = cache;
                RM.Pointer.transform.localRotation = Quaternion.Inverse(RM.Pointer.CursorImage.transform.localRotation) * Quaternion.Euler(0, 0, rotation.Value);
                RM.Pointer.transform.localScale = Vector2.one * scale.Value;
            }
            else
            {
                RM.Pointer.CursorImage.sprite = original;
                RM.Pointer.transform.localRotation = Quaternion.Euler(0, 0, rotation.Value);
            }

            active = activate;
        }

        public static Texture2D LoadTexture(byte[] image, FilterMode filterMode = FilterMode.Trilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            Texture2D texture2D = new(1, 1, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(texture2D, image, true);
            texture2D.wrapMode = wrapMode;
            texture2D.filterMode = filterMode;
            return texture2D;
        }

        // load a sprite with a default centered pivot
        public static Sprite LoadSprite(byte[] image, FilterMode filterMode = FilterMode.Trilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            var tex = LoadTexture(image, filterMode, wrapMode);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new(tex.width / 2, tex.height / 2));
        }
    }
}