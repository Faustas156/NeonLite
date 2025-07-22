using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace NeonLite.Modules.Optimization
{
    internal class LockMouse : IModule
    {
        const bool priority = true;
        static bool active = true;
        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "lockMouse", "Confine Mouse", "Prevent the mouse from moving off the screen during loads and on the staging screen.", true);
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(MainMenu), "SetState", SettingState, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(MainMenu), "SetState", ConfineMouse, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(Game), "SetCursorLock", IgnoreCursorLock, Patching.PatchTarget.Prefix);

            active = activate;
        }


        static bool settingState = false;
        static void SettingState() => settingState = true;
        static void ConfineMouse(MainMenu.State newState, MainMenu.State ____lastMenuState)
        {
            // if staging or if we're loading from ingame or staging
            if (newState == MainMenu.State.Staging || (newState == MainMenu.State.Loading && (____lastMenuState == MainMenu.State.None || ____lastMenuState == MainMenu.State.Staging)))
                Cursor.lockState = CursorLockMode.Confined;
            settingState = false;
        }
        static bool IgnoreCursorLock() => settingState;
    }

    internal class DisableTextNav : IModule
    {
        const bool priority = true;
        const bool active = true;

        static void Activate(bool _)
        {
            Patching.AddPatch(typeof(GameInput), "OnActionChange", DisableNav, Patching.PatchTarget.Prefix);
        }

        static bool DisableNav(object o, InputActionChange change)
        {
            if (EventSystem.current &&
                EventSystem.current.currentSelectedGameObject &&
                (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() ||
                 EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>()))
                return false;
            return true;
        }
    }

    internal class StopNav : IModule
    {
        const bool priority = false;
        static bool active = true;

        static readonly List<(InputActionSetupExtensions.BindingSyntax, string)> removedBinds = [];

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "stopNav", "Stop Keyboard Navigation", "Prevent the keyboard from navigating the UI with WASD.", true);
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate)
        {
            if (!activate)
            {
                foreach ((var binding, var path) in removedBinds)
                    binding.WithPath(path);

                removedBinds.Clear();
            }
            else
                UnityEngine.Resources.FindObjectsOfTypeAll<InputActionAsset>().Do(HijackBindings);
            
            //Patching.TogglePatch(activate, typeof(InputActionAsset), "ReResolveIfNecessary", HijackBindings, Patching.PatchTarget.Prefix);
            active = activate;
        }
         
        static void HijackBindings(InputActionAsset __instance)
        {
            // for some reason there are 2 NWControls ????
            // so uhhhhhh
            if (__instance.name != "NWControls")
                return;

            var nav = __instance.FindActionMap("UI").FindAction("Navigate");

            void RemoveBinding(string path)
            {
                var change = nav.ChangeBindingWithPath(path);
                removedBinds.Add((change, path));
                change.WithPath("");
            }

            RemoveBinding("<Keyboard>/w");
            RemoveBinding("<Keyboard>/a");
            RemoveBinding("<Keyboard>/s");
            RemoveBinding("<Keyboard>/d");
        }
    }
}
