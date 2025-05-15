using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace NeonLite.Modules.Optimization
{
    internal class CleanTexts : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static readonly FieldInfo textList = Helpers.Field(typeof(AxKLocalizedTextLord), "m_localizedTexts");

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
