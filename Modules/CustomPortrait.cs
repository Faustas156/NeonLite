using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    public class CustomPortrait : Module
    {
        private static MelonPreferences_Entry<string> _setting_CustomPortrait;

        private static Texture2D portrait;
        private static string path;

        public CustomPortrait() =>
            _setting_CustomPortrait = NeonLite.Config_NeonLite.CreateEntry("Custom Portrait", "",
                description: "Set a custom in-game portrait by entering the path to a local image.");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MainMenu), "SetState")]
        private static void UpdatePortrait(MainMenu __instance, ref MainMenu.State newState)
        {
            if (newState != MainMenu.State.Staging) return;

            if (string.IsNullOrEmpty(_setting_CustomPortrait.Value)) return;

            var uiPortrait = RM.ui.portraitUI;
            if (uiPortrait != null)
            {
                var portraitImg = uiPortrait.playerHolder.GetComponentInChildren<MeshRenderer>();
                if (path != _setting_CustomPortrait.Value || portrait == null)
                {
                    path = _setting_CustomPortrait.Value;

                    if (!File.Exists(path))
                    {
                        return;
                    }

                    var imgBytes = File.ReadAllBytes(path);
                    portrait = LoadTexture(imgBytes);
                }

                portraitImg.material.mainTexture = portrait;
            }
        }

        private static Texture2D LoadTexture(byte[] image)
        {
            Texture2D texture = new(2, 2);
            texture.LoadImage(image);
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }
}