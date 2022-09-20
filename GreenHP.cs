using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace NeonWhiteQoL
{
    public class GreenHP
    {
        public static void Initialize()
        {
            MethodInfo method = typeof(BossUI).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(GreenHP).GetMethod("OnPostUpdateBossUI"));
            NeonLite.harmony.Patch(method, null, harmonyMethod);
        }
        public static void OnPostUpdateBossUI(BossUI __instance)
        {
            //FieldInfo fi = __instance.GetType().GetField("_lastEnemyHealth", BindingFlags.Instance | BindingFlags.NonPublic);
            //int bossHP = (int)fi.GetValue(__instance);

            GameObject bossName = GameObject.Find("HUD/BossUI/BossUI Anchor/BossUI Holder/Boss Name Text");
            GameObject bossHealth = GameObject.Find("HUD/BossUI/BossUI Anchor/BossUI Holder/Boss Health Text");

            if (bossHealth == null)
            {
                bossHealth = UnityEngine.Object.Instantiate(bossName, bossName.transform.parent);
                bossHealth.name = "Boss Health Text";
                bossHealth.transform.localPosition += new Vector3(3, 0, 0);
                bossHealth.SetActive(true);
            }
            TextMeshPro text = bossHealth.GetComponent<TextMeshPro>();
            text.SetText("");
        }
    }
}
