using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace NeonLite.Modules.Misc
{
    internal class OpenGhostDir : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static Dictionary<LevelInfo, GameObject> buttons = [];
        static string currentPath;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "ghostDir", "Open Ghost Directory Button", "Adds a button to access the ghost directory of the selected/current level.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static readonly MethodInfo oglvli = AccessTools.Method(typeof(LevelInfo), "SetLevel");


        static void Activate(bool activate)
        {
            active = activate;

            if (activate)
                NeonLite.Harmony.Patch(oglvli, postfix: Helpers.HM(PostSetLevel));
            else
            {
                foreach (var kv in buttons)
                    UnityEngine.Object.Destroy(kv.Value);
                buttons.Clear();

                NeonLite.Harmony.Unpatch(oglvli, Helpers.MI(PostSetLevel));
            }
        }

        static void PostSetLevel(LevelInfo __instance, LevelData level)
        {
            if (!buttons.ContainsKey(__instance))
            {
                var obj = new GameObject("Ghost Button Holder", typeof(Animator));
                var backButton = Singleton<BackButtonAccessor>.Instance.BackButton.gameObject;
                obj.transform.parent = __instance.transform;
                var ghostButton = Utils.InstantiateUI(backButton, "Ghost Button", obj.transform);

                obj.transform.localPosition = new Vector3(105f, 110f);
                obj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                ghostButton.transform.localPosition = Vector3.zero;
                ghostButton.transform.localScale = Vector3.one;
                ghostButton.GetComponentInChildren<AxKLocalizedText>().SetKey("NeonLite/BUTTON_GHOSTDIRECTORY");

                //var ani = obj.GetComponent<Animator>();
                //var oldAni = backButton.GetComponentInParent<Animator>();
                //ani.runtimeAnimatorController = oldAni.runtimeAnimatorController;
                //ani.updateMode = AnimatorUpdateMode.UnscaledTime;
                //ani.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var bh = obj.AddComponent<MenuButtonHolder>();
                bh.ButtonRef.onClick.RemoveAllListeners();
                bh.ButtonRef.onClick.AddListener(() => Process.Start("file://" + currentPath));
                buttons.Add(__instance, obj);
            }

            //buttons[__instance].GetComponent<MenuButtonHolder>().ForceVisible();
            GhostUtils.GetPath(level.levelID, GhostUtils.GhostType.PersonalGhost, ref currentPath);
        }
    }
}
