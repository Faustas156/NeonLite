﻿using HarmonyLib;
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

        static void Setup() {}

        static readonly MethodInfo oglvli = AccessTools.Method(typeof(MainMenu), "SetState");

        static void Activate(bool activate) => NeonLite.Harmony.Patch(oglvli, postfix: Helpers.HM(PostSetState));

        static void PostSetState(MainMenu.State newState)
        {
            if (newState != MainMenu.State.GlobalNeonScore)
            {
                if (button)
                    button.SetActive(false);
                return;
            }

            if (!button)
            {
                var obj = new GameObject("Copy PB Holder");
                var backButton = Singleton<BackButtonAccessor>.Instance.BackButton.gameObject;
                obj.transform.parent = backButton.transform.parent;
                var pbButton = Utils.InstantiateUI(backButton, "Copy PB Button", obj.transform);

                obj.transform.localPosition = new Vector3(0, 130f);
                obj.transform.localScale = Vector3.one;
                pbButton.transform.localPosition = Vector3.zero;
                pbButton.transform.localScale = Vector3.one;

                //var ani = obj.GetComponent<Animator>();
                //var oldAni = backButton.GetComponentInParent<Animator>();
                //ani.runtimeAnimatorController = oldAni.runtimeAnimatorController;
                //ani.updateMode = AnimatorUpdateMode.UnscaledTime;
                //ani.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var bh = obj.AddComponent<MenuButtonHolder>();
                bh.ButtonRef.onClick.RemoveAllListeners();
                bh.ButtonRef.onClick.AddListener(CopyAllTimes);
                button = obj;
            }
            button.SetActive(true);
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
                    if (m.missionID.StartsWith("M_SIDEQUESTS_"))
                    {
                        if (m.missionID == "M_SIDEQUESTS_YELLOW")
                            yellow = m;
                        else if (m.missionID == "M_SIDEQUESTS_RED")
                            red = m;
                        else if (m.missionID == "M_SIDEQUESTS_VIOLET")
                            violet = m;
                        continue;
                    }
                    foreach (var l in m.levels)
                    {
                        final.AppendLine(Helpers.FormatTime(gd.GetLevelStats(l.levelID).GetTimeBestMicroseconds() / 1000, true, '.'));
                    }
                }
            }

            foreach (var l in red.levels)
                final.AppendLine(Helpers.FormatTime(gd.GetLevelStats(l.levelID).GetTimeBestMicroseconds() / 1000, true, '.'));
            foreach (var l in violet.levels)
                final.AppendLine(Helpers.FormatTime(gd.GetLevelStats(l.levelID).GetTimeBestMicroseconds() / 1000, true, '.'));
            foreach (var l in yellow.levels)
                final.AppendLine(Helpers.FormatTime(gd.GetLevelStats(l.levelID).GetTimeBestMicroseconds() / 1000, true, '.'));


            GUIUtility.systemCopyBuffer = final.ToString();
            button.GetComponentInChildren<AxKLocalizedText>().SetKey("NeonLite/BUTTON_COPIED");
        }

    }
}