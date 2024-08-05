using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace NeonLite.Modules.Optimization
{
    internal class CleanTexts : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = true;

        static void Setup() { }

        static readonly FieldInfo textList = AccessTools.Field(typeof(AxKLocalizedTextLord), "m_localizedTexts");

        static void Activate(bool activate) { }

        static void OnLevelLoad(LevelData _)
        {
            var list = (List<AxKLocalizedTextObject_Interface>)textList.GetValue(AxKLocalizedTextLord.GetInstance());
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(null))
                {
                    list.RemoveAt(i);
                    --i;
                }
            }
        }

    }
}
