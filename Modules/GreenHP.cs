using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;


namespace NeonWhiteQoL.Modules
{
    public class GreenHP
    {
        private static GameObject bossName = null;
        private static GameObject bossHealth = null;
        private static FieldInfo _lastEnemyHealth = typeof(BossUI).GetField("_lastEnemyHealth", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Initialize()
        {
            MethodInfo method = typeof(BossUI).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            HarmonyMethod harmonyMethod = new (typeof(GreenHP).GetMethod("OnPostUpdateBossUI"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);
        }
        public static void OnPostUpdateBossUI(BossUI __instance)
        {
            if (!NeonLite.GreenHP_display.Value)
                return;

            int bossHP = (int)_lastEnemyHealth.GetValue(__instance);

            if (bossName == null)
            {
                bossName = GameObject.Find("HUD/BossUI/BossUI Anchor/BossUI Holder/Boss Name Text");
                bossHealth = GameObject.Find("HUD/BossUI/BossUI Anchor/BossUI Holder/Boss Health Text");
            }

            if (bossHealth == null)
            {
                bossHealth = UnityEngine.Object.Instantiate(bossName, bossName.transform.parent); //this causes a memory leak, it does not get destroyed and is still running 
                bossHealth.name = "Boss Health Text";
                bossHealth.transform.localPosition += new Vector3(3, 0, 0);
                bossHealth.SetActive(true);
            }
            TextMeshPro text = bossHealth.GetComponent<TextMeshPro>();
            text.SetText(bossHP.ToString());

        }
    }
}
