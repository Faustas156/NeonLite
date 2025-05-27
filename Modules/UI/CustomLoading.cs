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
            setting = Settings.Add("NeonLite", "UI", "loadingIcon", "Custom Loading Icon", "Set a custom loading icon relacing Mikey by entering the path to a local image.\nMake sure to remove quotes!", "", null);
            active = setting.SetupForModule(Activate, static (_, after) => after != "");
        }

        static void Activate(bool activate)
        {
            if (activate && (!active || firstLoad))
                Patching.AddPatch(typeof(MenuScreenLoading), "SetVisible", PostSetVisible, Patching.PatchTarget.Postfix);
            else if (!activate)
                Patching.RemovePatch(typeof(MenuScreenLoading), "SetVisible", PostSetVisible);

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
            if (!cache)
            {
                if (!File.Exists(path))
                    return;

                var file = File.ReadAllBytes(path);
                cache = Helpers.LoadSprite(file, wrapMode: TextureWrapMode.Repeat, pivot: mikey.sprite.pivot / mikey.sprite.rect.size);
            }


            mikey.sprite = cache;
        }
    }
}