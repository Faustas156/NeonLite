using HarmonyLib;
using Steamworks;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLite.GameObjects
{
    [HarmonyPatch]
    internal class OpenGhostDir
    {
        private static GameObject _ghostButton;
        private static string _path;

        internal static void Initialize()
        {
            if (!NeonLite.s_Setting_GhostButton.Value || _ghostButton != null) return;

            GameObject backButton = GameObject.Find("Main Menu/Canvas/BackButtonHolderHolder/Back Button Holder/Button");
            Transform levelInfo = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Inventory Inspector/Inventory Inspector Holder/Panels/Leaderboards And LevelInfo").transform;

            _ghostButton = Utils.InstantiateUI(backButton,
                "Ghost Button",
                levelInfo.transform);

            Transform parent = backButton.transform.parent;

            parent.localPosition += new Vector3(0, 0, 0);
            _ghostButton.transform.localPosition = new Vector3(-315f, 420f);
            _ghostButton.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            TextMeshProUGUI text = _ghostButton.GetComponentInChildren<TextMeshProUGUI>();
            text.SetText("Ghost Directory");

            Button button = _ghostButton.GetComponent<Button>();
            button.onClick.AddListener(() => Process.Start(@"explorer.exe", $"\"{_path}\""));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelInfo), "SetLevel")]
        private static void PostSetLevel(ref LevelData level) =>
            GhostUtils.GetPath(level.levelID, GhostUtils.GhostType.PersonalGhost, ref _path);
    }
}
