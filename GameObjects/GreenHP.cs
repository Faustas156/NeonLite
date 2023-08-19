using TMPro;
using UnityEngine;


namespace NeonLite.GameObjects
{
    public class GreenHP : MonoBehaviour
    {
        private readonly Enemy _green;
        private readonly TextMeshPro _healthText;


        public static void Initialize()
        {
            if (!NeonLite.GreenHP_display.Value || !NeonLite.Game.GetCurrentLevel().isBossFight) return;

            GameObject bossName = RM.ui.bossUI.nameText.gameObject;
            GameObject bossHealth = Instantiate(bossName, bossName.transform.parent);
            bossHealth.AddComponent<GreenHP>();
            bossHealth.name = "Boss Health Text";
            bossHealth.transform.localPosition += new Vector3(3, 0, 0);
            bossHealth.SetActive(true);
        }

        private GreenHP()
        {
            _green = (Enemy)typeof(BossUI).GetField("_currentBossEnemy", NeonLite.s_privateInstance).GetValue(RM.ui.bossUI);
            _healthText = gameObject.GetComponent<TextMeshPro>();
        }

        private void FixedUpdate() =>
            _healthText.SetText(_green.CurrentHealth.ToString());
    }
}
