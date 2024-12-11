using HarmonyLib;
using MelonLoader;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLite.Modules.UI
{
    internal class CustomLoading : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static bool firstLoad = true;
        static MelonPreferences_Entry<string> setting;
        static Sprite cache;

        static void Setup()
        {
            setting = Settings.Add("NeonLite", "UI", "loadingIcon", "Custom Loading Icon", "Set a custom loading icon relacing Mikey by entering the path to a local image (256x256).\nMake sure to remove quotes!", "", null);
            active = setting.SetupForModule(Activate, (_, after) => after != "");
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(MenuScreenLoading), "SetVisible", null, null);

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

        static void PostSetVisible(MenuScreenLoading __instance, bool vis)
        {
            if (!vis)
                return;
            var mikey = __instance.mikeyIndicator.GetComponentInChildren<Image>();

            string path = setting.Value;
            NeonLite.Logger.DebugMsg(path);

            if (!File.Exists(path))
                return;

            if (!cache)
            {
                NeonLite.Logger.DebugMsg("build cache");
                var file = File.ReadAllBytes(path);
                var tex = LoadTexture(file);

                if (tex.width != mikey.sprite.texture.width || tex.height != mikey.sprite.texture.height)
                    return;

                tex.filterMode = FilterMode.Trilinear;
                tex.wrapModeW = TextureWrapMode.Repeat;
                cache = Sprite.Create(tex, mikey.sprite.rect, mikey.sprite.pivot);
            }

            NeonLite.Logger.DebugMsg("set");

            mikey.sprite = cache;
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