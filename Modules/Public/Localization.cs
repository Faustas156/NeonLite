using System.Reflection;
using HarmonyLib;
using I2.Loc;
using TMPro;
using UnityEngine;

#pragma warning disable IDE0130
namespace NeonLite.Modules
{
    [Module(100)]
    public static class Localization
    {
#pragma warning disable CS0414
        const bool priority = false;
        const bool active = true;

        static bool wasActivated = false;

        const string overrideSheet = "1hfiFL_7ainT1jP5s4At_fWSJOzNMHUJhAuoP23QsEUY";
        const string overrideSheetName = "I2Loc NW Mod Localization";

        internal static event Action OnFontSetSetup;

        internal static AxKLocalizedText_FontLib.FontSetPro fbs;
        internal static int fbi = -1;

        internal static void Setup()
        {
            // this is illegal but we're doing it anyway
            Patching.AddPatch(typeof(LocalizationManager), "AddSource", ChangeSource, Patching.PatchTarget.Prefix, true);
            Patching.AddPatch(typeof(LocalizationManager), "DoLocalizeAll", LocalizeAll, Patching.PatchTarget.Prefix, true);
        }

        internal static void Activate(bool _)
        {
            if (wasActivated)
                return;
            wasActivated = true;

            // setup sidequests from the helper aprt of the sheet
            var gd = NeonLite.Game.GetGameData();
            var sqs = gd.GetCampaign("C_SIDEQUESTS");
            if (sqs)
            {
                foreach (var mission in sqs.missionData)
                    mission.missionDisplayName = "Interface/" + mission.missionID;
            }

            SetupFontSet();
        }

        internal static void SetupFontSet()
        {
            NeonLite.Logger.DebugMsg("SETUPFONTSET");
            var bundle = NeonLite.bundle;
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
            OnFontSetSetup?.Invoke();
        }

        static void ChangeSource(LanguageSourceData Source)
        {
            NeonLite.Logger.DebugMsg("CHANGESOURCE");

            if (!Source.HasGoogleSpreadsheet())
                return;
            Source.Google_SpreadsheetKey = overrideSheet;
            Source.Google_SpreadsheetName = overrideSheetName;
            Source.GoogleUpdateFrequency = LanguageSourceData.eGoogleUpdateFrequency.Always;
            Source.GoogleUpdateSynchronization = LanguageSourceData.eGoogleUpdateSynchronization.AsSoonAsDownloaded;
        }

        static void LocalizeAll() => AxKLocalizedTextLord.GetInstance().OnLanguageSet();

        static readonly FieldInfo fontIndex = Helpers.Field(typeof(AxKLocalizedText), "m_fontIndex");

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
            NeonLite.Logger.DebugMsg("ADDFONTSET");

            AxKLocalizedText_FontLib.FontSetPro ret = new()
            {
                english = font,
                englishFontMats = [font?.material]
            };
            var fontLib = AxKLocalizedTextLord.GetInstance().fontLib;
            NeonLite.Logger.DebugMsg($"fontlib {fontLib}");
            NeonLite.Logger.DebugMsg($"fontlibL {fontLib.textMeshProFontSets.Length}");
            fontLib.textMeshProFontSets = [.. fontLib.textMeshProFontSets.AddItem(ret)];
            NeonLite.Logger.DebugMsg($"fontlib {fontLib.textMeshProFontSets.Length}");

            int i = fontLib.textMeshProFontSets.Length - 1;
            return (ret, i);
        }
        public static void UpdateFontSet(AxKLocalizedText_FontLib.FontSetPro fontSet, int index) => AxKLocalizedTextLord.GetInstance().fontLib.textMeshProFontSets[index] = fontSet;
    }
}
