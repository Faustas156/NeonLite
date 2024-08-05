using HarmonyLib;
using I2.Loc;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace NeonLite.Modules
{
    public class Localization : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        const string overrideSheet = "1hfiFL_7ainT1jP5s4At_fWSJOzNMHUJhAuoP23QsEUY";
        const string overrideSheetName = "I2Loc NW Mod Localization";

        internal static AxKLocalizedText_FontLib.FontSetPro fbs;
        internal static int fbi = -1;

        static void Setup() { }

        static readonly MethodInfo ogadd = AccessTools.Method(typeof(LocalizationManager), "AddSource");
        static readonly MethodInfo ogall = AccessTools.Method(typeof(LocalizationManager), "DoLocalizeAll");

        static void Activate(bool _)
        {
            NeonLite.Harmony.Patch(ogadd, prefix: Helpers.HM(ChangeSource));
            NeonLite.Harmony.Patch(ogall, prefix: Helpers.HM(LocalizeAll));
            NeonLite.OnBundleLoad += SetupFontSet;
        }

        internal static void SetupFontSet(AssetBundle bundle)
        {
            (fbs, fbi) = AddFontSet(bundle.LoadAsset<TMP_FontAsset>("Assets/Fonts/NovaMono-Regular SDF.asset"));
            var buffer = bundle.LoadAsset<TMP_FontAsset>("Assets/Fonts/NotoSansTC-Regular SDF.asset");
            fbs.chinese = buffer;
            fbs.chineseFontMats = [buffer.material];
            buffer = bundle.LoadAsset<TMP_FontAsset>("Assets/Fonts/NotoSansSC-Regular SDF.asset");
            fbs.chineseSimp = buffer;
            fbs.chineseSimpFontMats = [buffer.material];
            buffer = bundle.LoadAsset<TMP_FontAsset>("Assets/Fonts/NanumGothicCoding-Regular SDF.asset");
            fbs.korean = buffer;
            fbs.koreanFontMats = [buffer.material];
            buffer = bundle.LoadAsset<TMP_FontAsset>("Assets/Fonts/MPLUS1Code-Regular SDF.asset");
            fbs.japanese = buffer;
            fbs.japaneseFontMats = [buffer.material];
            buffer = bundle.LoadAsset<TMP_FontAsset>("Assets/Fonts/PTMono-Regular SDF.asset");
            fbs.russian = buffer;
            fbs.russianFontMats = [buffer.material];
            UpdateFontSet(fbs, fbi);
        }

        static void ChangeSource(ref LanguageSourceData Source)
        {
            if (!Source.HasGoogleSpreadsheet())
                return;
            Source.Google_SpreadsheetKey = overrideSheet;
            Source.Google_SpreadsheetName = overrideSheetName;
            Source.GoogleUpdateFrequency = LanguageSourceData.eGoogleUpdateFrequency.Always;
            Source.GoogleUpdateSynchronization = LanguageSourceData.eGoogleUpdateSynchronization.OnSceneLoaded;
        }

        static void LocalizeAll() => AxKLocalizedTextLord.GetInstance().OnLanguageSet();

        static readonly FieldInfo fontIndex = AccessTools.Field(typeof(AxKLocalizedText), "m_fontIndex");

        public static AxKLocalizedText SetupUI(Component obj, int fontBase = -1)
        {
            var local = obj.GetOrAddComponent<AxKLocalizedText>();
            local.textMeshProUGUI = obj.GetComponent<TextMeshProUGUI>();
            fontIndex.SetValue(local, fontBase);
            return local;
        }

        public static AxKLocalizedText Setup(Component obj, int fontBase = -1)
        {
            var local = obj.GetOrAddComponent<AxKLocalizedText>();
            local.textMeshPro = obj.GetComponent<TextMeshPro>();
            fontIndex.SetValue(local, fontBase);
            return local;
        }

        public static (AxKLocalizedText_FontLib.FontSetPro, int) AddFontSet(TMP_FontAsset font)
        {
            AxKLocalizedText_FontLib.FontSetPro ret = new()
            {
                english = font,
                englishFontMats = [font?.material]
            };
            var fontLib = AxKLocalizedTextLord.GetInstance().fontLib;
            fontLib.textMeshProFontSets = fontLib.textMeshProFontSets.AddItem(ret).ToArray();
            int i = fontLib.textMeshProFontSets.Length - 1;
            return (ret, i);
        }
        public static void UpdateFontSet(AxKLocalizedText_FontLib.FontSetPro fontSet, int index) => AxKLocalizedTextLord.GetInstance().fontLib.textMeshProFontSets[index] = fontSet;
    }
}
