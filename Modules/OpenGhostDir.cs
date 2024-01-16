using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class OpenGhostDir : Module
    {

        private static MelonPreferences_Entry<bool> _setting_Apocalypse_display;

        public OpenGhostDir() =>
            _setting_Apocalypse_display = NeonLite.Config_NeonLite.CreateEntry("Open Ghost Directory Button", true, description: "Shows a button at the end to open this level's ghost directory in the file explorer.");

        static MenuButtonHolder ghostButton;

        public static string GetGhostDirectory()
        {
            string path = GhostRecorder.GetCompressedSavePathForLevel(Singleton<Game>.Instance.GetCurrentLevel());
            path = Path.GetDirectoryName(path) + "/";
            return path;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuScreenResults), "OnSetVisible")]
        private static void OnMenuScreen(ref MenuScreenResults __instance)
        {
            // EVEN IF WE'RE NOT ENABLED, lets try to help the user
            if (ghostButton == null)
            {
                var button = __instance._buttonContine;
                // copy the layout
                var layout = UnityEngine.Object.Instantiate(button.transform.parent.gameObject, button.transform.parent.parent);
                layout.name = "Ghost Button Holder";
                // empty it
                foreach (Transform child in layout.transform)
                    UnityEngine.Object.Destroy(child.gameObject);

                // copy the button and put it in the new layout
                ghostButton = UnityEngine.Object.Instantiate(button.gameObject, layout.transform).GetComponent<MenuButtonHolder>();
                ghostButton.name = "Button Ghost";
                ghostButton.buttonText = "Open Ghost Directory";
                ghostButton.buttonTextRef.text = "Open Ghost Directory";
                var pos = button.transform.parent.position;
                pos.x = -0.35f; // don't ask me how. the math just ISN'T THERE i had to hardcode it
                layout.transform.position = pos;

                ghostButton.ButtonRef.onClick.RemoveAllListeners();
                ghostButton.ButtonRef.onClick.AddListener(() => Process.Start("file://" + GetGhostDirectory()));
            }

            if (!__instance.buttonsToLoad.Contains(ghostButton))
                __instance.buttonsToLoad.Add(ghostButton);
        }

    }
}
