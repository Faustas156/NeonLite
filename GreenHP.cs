using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;
using static MelonLoader.MelonLogger;

namespace NeonWhiteQoL
{
    public class GreenHP
    {
        private static GameObject bossName = null;
        private static GameObject bossHealth = null;
        private static FieldInfo _lastEnemyHealth = null;

        public static void Initialize()
        {
            MethodInfo method = typeof(BossUI).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(GreenHP).GetMethod("OnPostUpdateBossUI"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);
        }
        public static void OnPostUpdateBossUI(BossUI __instance)
        {
            if (_lastEnemyHealth == null)
                _lastEnemyHealth = __instance.GetType().GetField("_lastEnemyHealth", BindingFlags.Instance | BindingFlags.NonPublic);
            int bossHP = (int) _lastEnemyHealth.GetValue(__instance);

            if (bossName == null)
            {
                bossName = GameObject.Find("HUD/BossUI/BossUI Anchor/BossUI Holder/Boss Name Text");
                bossHealth = GameObject.Find("HUD/BossUI/BossUI Anchor/BossUI Holder/Boss Health Text");
            }

            if (bossHealth == null)
            {
                bossHealth = UnityEngine.Object.Instantiate(bossName, bossName.transform.parent);
                bossHealth.name = "Boss Health Text";
                bossHealth.transform.localPosition += new Vector3(3, 0, 0);
                bossHealth.SetActive(true);
            }
            TextMeshPro text = bossHealth.GetComponent<TextMeshPro>();
            text.SetText(bossHP + "");
        }
    }
}
