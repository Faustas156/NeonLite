using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

//Main Menu/Canvas/Location Panel/Location Menu/Content/Scroll View/Viewport/Buttons/
namespace NeonWhiteQoL.cl
{
    public class RemoveMission : MonoBehaviour
    {
        public static void Initialize()
        {
            //MethodInfo method = typeof(MenuButtonHolder).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            //HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(RemoveMission).GetMethod("PostRemoveButton"));
            //NeonLite.Harmony.Patch(method, null, harmonyMethod);
        }
        public static void PostRemoveButton(MenuButtonHolder __instance)
        {
            //GameObject gameobject = __instance.gameObject.transform.Find("Button/Text").gameObject;
            //if (gameobject == null) return;
            //TextMeshProUGUI text = gameobject.GetComponent<TextMeshProUGUI>();
            //if (text == null || text.text != "Start Mission") return ;
            //Debug.Log(__instance.gameObject.activeSelf);
            //__instance.gameObject.transform.position = new Vector3(-1000, 0, 0);
        }
    }
}
