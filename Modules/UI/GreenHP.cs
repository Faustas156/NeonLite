using HarmonyLib;
using TMPro;
using UnityEngine;

namespace NeonLite.Modules.UI
{
    internal class GreenHP : MonoBehaviour, IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        Enemy green;
        TextMeshPro text;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI/In-game", "greenHP", "Show Neon Green's HP", "Displays the HP of Neon Green in text form.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        static void Activate(bool activate) => active = activate;

        static void OnLevelLoad(LevelData level)
        {
            if (!level || !level.isBossFight)
                return;

            GameObject bossName = RM.ui.bossUI.nameText.gameObject;
            GameObject bossHealth = Instantiate(bossName, bossName.transform.parent);
            var hp = bossHealth.AddComponent<GreenHP>();
            bossHealth.name = "Boss Health Text";
            bossHealth.transform.localPosition += new Vector3(3, 0, 0);
            bossHealth.SetActive(true);

            hp.green = (Enemy)AccessTools.Field(typeof(BossUI), "_currentBossEnemy").GetValue(RM.ui.bossUI);
        }

        void Awake() => text = GetComponent<TextMeshPro>();
        void LateUpdate() => text.SetText(green.CurrentHealth.ToString());
    }
}
