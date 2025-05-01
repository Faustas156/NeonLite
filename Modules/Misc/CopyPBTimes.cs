using HarmonyLib;
using MelonLoader;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NeonLite.Modules.Misc
{
    internal class CopyPBTimes : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static GameObject button;
        static GameObject titleButton;

        static MelonPreferences_Entry<bool> titleButtonS;

        static void Setup() {
            titleButtonS = Settings.Add(Settings.h, "UI", "copyPBTitle", "Copy PB Times in Title", "Allows you to copy your PB times from the title screen.", true);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(MainMenu), "SetState", PostSetState, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(MenuScreenTitle), "OnSetVisible", AddTitleButton, Patching.PatchTarget.Postfix);
        }

        static void AddTitleButton(MenuScreenTitle __instance)
        {
            if (!titleButton)
            {
                titleButton = Utils.InstantiateUI(__instance.quitButton, "Copy PB Title Button Holder", __instance.quitButton.transform.parent);
                titleButton.transform.SetSiblingIndex(titleButton.transform.parent.childCount - 2);

                __instance.buttonsToLoad.Insert(__instance.buttonsToLoad.Count - 1, titleButton.GetComponent<MenuButtonHolder>());
                __instance.LoadButtons();

                var bh = titleButton.GetComponent<MenuButtonHolder>();
                bh.ButtonRef.onClick.RemoveAllListeners();
                bh.ButtonRef.onClick.AddListener(CopyAllTimes);
            }

            titleButton.GetComponentInChildren<AxKLocalizedText>().SetKey("NeonLite/BUTTON_COPYPBS");
            titleButton.SetActive(titleButtonS.Value);
        }

        static void PostSetState(MainMenu.State newState)
        {
            if (newState != MainMenu.State.GlobalNeonScore)
            {
                if (button)
                    button.GetComponent<MenuButtonHolder>().UnloadButton();
                return;
            }

            if (!button)
            {
                var obj = new GameObject("Copy PB Holder", typeof(Animator));
                var backButton = Singleton<BackButtonAccessor>.Instance.BackButton.gameObject;
                obj.transform.parent = backButton.transform.parent;
                var pbButton = Utils.InstantiateUI(backButton, "Button", obj.transform);

                obj.transform.localPosition = new Vector3(0, 130f);
                obj.transform.localScale = Vector3.one;
                pbButton.transform.localPosition = Vector3.zero;
                pbButton.transform.localScale = Vector3.one;

                var ani = obj.GetComponent<Animator>();
                var oldAni = backButton.GetComponentInParent<Animator>(true);
                ani.runtimeAnimatorController = oldAni.runtimeAnimatorController;
                ani.updateMode = AnimatorUpdateMode.UnscaledTime;
                ani.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                ani.Rebind();

                var bh = obj.AddComponent<MenuButtonHolder>();
                bh.ButtonRef.onClick.RemoveAllListeners();
                bh.ButtonRef.onClick.AddListener(CopyAllTimes);
                bh.animatorRef = ani;
                button = obj;
            }
            button.GetComponent<MenuButtonHolder>().LoadButton();
            button.GetComponentInChildren<AxKLocalizedText>().SetKey("NeonLite/BUTTON_COPYPBS");
        }

        static void CopyAllTimes()
        {
            StringBuilder final = new();
            var gd = NeonLite.Game.GetGameData();

            MissionData red = null;
            MissionData violet = null;
            MissionData yellow = null;

            foreach (var campaign in gd.campaigns)
            {
                foreach (var m in campaign.missionData)
                {
                    if (m.missionType == MissionData.MissionType.SideQuest)
                    {
                        if (m.missionID == "M_SIDEQUESTS_YELLOW")
                            yellow = m;
                        else if (m.missionID == "M_SIDEQUESTS_RED")
                            red = m;
                        else if (m.missionID == "M_SIDEQUESTS_VIOLET")
                            violet = m;
                        continue;
                    }
                    else if (m.missionType != MissionData.MissionType.MainQuest)
                        continue;
                    foreach (var l in m.levels)
                        final.AppendLine(Helpers.FormatTime(gd.GetLevelStats(l.levelID).GetTimeBestMicroseconds() / 1000, true, '.'));
                }
            }

            foreach (var l in red.levels)
                final.AppendLine(Helpers.FormatTime(gd.GetLevelStats(l.levelID).GetTimeBestMicroseconds() / 1000, true, '.'));
            foreach (var l in violet.levels)
                final.AppendLine(Helpers.FormatTime(gd.GetLevelStats(l.levelID).GetTimeBestMicroseconds() / 1000, true, '.'));
            foreach (var l in yellow.levels)
                final.AppendLine(Helpers.FormatTime(gd.GetLevelStats(l.levelID).GetTimeBestMicroseconds() / 1000, true, '.'));

            GUIUtility.systemCopyBuffer = final.ToString();

            button?.GetComponentInChildren<AxKLocalizedText>().SetKey("NeonLite/BUTTON_COPIED");
            titleButton?.GetComponentInChildren<AxKLocalizedText>().SetKey("NeonLite/BUTTON_COPIED");
        }

    }
}
