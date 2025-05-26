using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace NeonLite.Modules.UI
{
    internal class StartRebind : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static void Activate(bool _)
        {
            Patching.AddPatch(typeof(ControlMapper), "OnEnable", AddToBindingData, Patching.PatchTarget.Prefix);
            Patching.AddPatch(typeof(ControlMapperRebindLocalisationData), "GetActionKey", GetActionKey, Patching.PatchTarget.Prefix);
            Patching.AddPatch(typeof(ControlMapper), "ShowControls", AddAdd, Patching.PatchTarget.Transpiler);
        }

        static IEnumerable<CodeInstruction> AddAdd(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(true, new CodeMatch(x => x.Calls(AccessTools.PropertyGetter(typeof(InputActionMap), "actions"))))
                .Advance(1)
                .Insert(CodeInstruction.Call(typeof(StartRebind), "AddUIStart"))
                .InstructionEnumeration();
        }

        static ReadOnlyArray<InputAction> AddUIStart(ReadOnlyArray<InputAction> actions)
        {
            var start = Singleton<GameInput>.Instance.Controls.UI.Start;
            if (start.bindings.Any(x => x.groups == "PC"))
            {
                // change the binding to include Keyboard
                start.ChangeBindingWithGroup("PC").WithGroups("Keyboard");
            }
            return new ReadOnlyArray<InputAction>(actions.AddItem(start).ToArray());
        }

        static void AddToBindingData(ControlMapper __instance)
        {
            var bindingData = __instance.PreferredBindingData;
            if (bindingData.GetPreferredBindingEntry(GameInput.InputDeviceClass.Keyboard, "Start") == null)
            {
                var bindList = (List<InputActionPreferredBindingData.DeviceClassActionBindingPairs>)
                    Helpers.Field(typeof(InputActionPreferredBindingData), "_bindingData").GetValue(bindingData);

                foreach (var data in bindList)
                {
                    var bindingEntry = new PreferredBindingEntry();
                    Helpers.Field(typeof(PreferredBindingEntry), "_bindingType").SetValue(bindingEntry, PreferredBindingType.Button);
                    InputActionPreferredBindingData.ActionPreferredBindingPair newPair = new("Start")
                    {
                        bindingEntry = bindingEntry
                    };
                    data.actionPreferredBindingPairs.Add(newPair);
                }
            }
        }

        static bool GetActionKey(string actionName, ref string __result)
        {
            if (actionName == "Start")
                __result = "NeonLite/REBIND_CONTROL_START";
            return actionName != "Start";
        }
    }
}
