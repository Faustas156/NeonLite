using System.Reflection;
using System.Text;
using HarmonyLib;
using I2.Loc;
using MelonLoader.TinyJSON;
using Sylvan.Data.Csv;
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

        internal static event Action OnFontSetSetup;

        internal static AxKLocalizedText_FontLib.FontSetPro fbs;
        internal static int fbi = -1;

        internal static void Setup()
        {
            // this is illegal but we're doing it anyway

            /// V1 PATCHES
            Patching.AddPatch(typeof(LocalizationManager), "AddSource", ChangeSource, Patching.PatchTarget.Prefix, true);
            Patching.AddPatch(typeof(LocalizationManager), "DoLocalizeAll", LocalizeAll, Patching.PatchTarget.Prefix, true);

            /// V2 PATCHES
            CreateSource();
            Patching.AddPatch(typeof(LocalizationManager), "UpdateSources", SetupAddSource, Patching.PatchTarget.Prefix, true);
        }

        class RelocalizeTimer : MonoBehaviour
        {
            internal static float timer = -1f;

            void Update()
            {
                if (timer < 0)
                    return;
                timer -= Time.unscaledDeltaTime;
                if (timer < 0)
                    LocalizationManager.LocalizeAll();
            }
        }

        internal static void Activate(bool _)
        {
            NeonLite.holder.AddComponent<RelocalizeTimer>();

            // setup sidequests from the helper aprt of the sheet
            var gd = NeonLite.Game.GetGameData();
            var sqs = gd.GetCampaign("C_SIDEQUESTS");
            if (sqs)
            {
                foreach (var mission in sqs.missionData)
                    mission.missionDisplayName = "NeonLite/" + mission.missionID;
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

        ///////// V1 /////////
        /// WILL BE REMOVED EVENTUALLY ///

        const string OVERRIDE_SHEET = "1hfiFL_7ainT1jP5s4At_fWSJOzNMHUJhAuoP23QsEUY";
        const string OVERRIDE_NAME = "I2Loc NW Mod Localization";

        static void ChangeSource(LanguageSourceData Source)
        {
            NeonLite.Logger.DebugMsg("CHANGESOURCE");

            if (!Source.HasGoogleSpreadsheet())
                return;
            Source.Google_SpreadsheetKey = OVERRIDE_SHEET;
            Source.Google_SpreadsheetName = OVERRIDE_NAME;
            Source.GoogleUpdateFrequency = LanguageSourceData.eGoogleUpdateFrequency.Always;
            Source.GoogleUpdateSynchronization = LanguageSourceData.eGoogleUpdateSynchronization.AsSoonAsDownloaded;
        }

        ///////// V2 /////////

        static void SetupAddSource()
        {
            NeonLite.Logger.DebugMsg("SetupAddSource");
            if (modsSource == null)
                CreateSource();

            if (!LocalizationManager.Sources.Contains(modsSource))
                LocalizationManager.Sources.Add(modsSource);
        }

        static LanguageSourceData modsSource;
        static LanguageSourceData CreateSource()
        {
            modsSource = new LanguageSourceData();
            for (int i = 0; i < (int)NWLanguages.Count; ++i)
                modsSource.AddLanguage(languageNames[i], NWLangToCode((NWLanguages)i));
            modsSource.mLanguages.Do(x => x.SetLoaded(true));

            return modsSource;
        }

        public enum NWLanguages
        {
            Unknown = -1,
            English,
            French,
            Italian,
            German,
            Spanish,
            Russian,
            Japanese,
            Korean,
            Chinese_S,
            Chinese_T,
            Polish,
            Portugese_B,
            Spanish_LATAM,
            Portugese,
            Turkish,

            Count
        }

        static readonly string[] languageNames = [
            "English",
            "French",
            "Italian",
            "German",
            "Spanish",
            "Russian",
            "Japanese",
            "Korean",
            "Chinese (Simplified)",
            "Chinese",
            "Polish",
            "Portuguese (Brazil)",
            "Spanish (Latin America)",
            "Portuguese",
            "Turkish"
        ];

        public static string NWLangToCode(NWLanguages code)
            => code switch
            {
                NWLanguages.English => "en",
                NWLanguages.French => "fr",
                NWLanguages.Italian => "it",
                NWLanguages.German => "de",
                NWLanguages.Spanish => "es",
                NWLanguages.Russian => "ru",
                NWLanguages.Japanese => "ja",
                NWLanguages.Korean => "ko",
                NWLanguages.Chinese_S => "zh-CN",
                NWLanguages.Chinese_T => "zh",
                NWLanguages.Polish => "pl",
                NWLanguages.Portugese_B => "pt-BR",
                NWLanguages.Spanish_LATAM => "es-US",
                NWLanguages.Portugese => "pt",
                NWLanguages.Turkish => "tr",
                _ => "",
            };

        public static NWLanguages CodeToNWLang(string code)
        {
            code = code.ToLower();
            var ret = code switch
            {
                var s when s.StartsWith("en") => NWLanguages.English,
                var s when s.StartsWith("fr") => NWLanguages.French,
                var s when s.StartsWith("it") => NWLanguages.Italian,
                var s when s.StartsWith("de") => NWLanguages.German,
                var s when s.StartsWith("ru") => NWLanguages.Russian,
                var s when s.StartsWith("ja") => NWLanguages.Japanese,
                var s when s.StartsWith("ko") => NWLanguages.Korean,
                var s when s.StartsWith("pl") => NWLanguages.Polish,
                var s when s.StartsWith("tr") => NWLanguages.Turkish,
                _ => NWLanguages.Unknown
            };

            if (ret != NWLanguages.Unknown)
                return ret;

            if (code.StartsWith("es"))
            {
                if (code.Contains("us") || code.Contains("419"))
                    return NWLanguages.Spanish_LATAM;
                return NWLanguages.Spanish;
            }

            if (code.StartsWith("zh"))
            {
                if (code.Contains("cn"))
                    return NWLanguages.Chinese_S;
                return NWLanguages.Chinese_T;
            }

            if (code.StartsWith("pt"))
            {
                if (code.Contains("br"))
                    return NWLanguages.Portugese_B;
                return NWLanguages.Portugese;
            }

            return NWLanguages.Unknown;
        }

        static readonly Dictionary<NWLanguages, int> idxCache = new((int)NWLanguages.Count);
        static int GetIndex(NWLanguages lang)
        {
            if (!idxCache.TryGetValue(lang, out var ret))
            {
                ret = modsSource.GetLanguageIndexFromCode(NWLangToCode(lang));
                idxCache.Add(lang, ret);
            }
            return ret;
        }

        public class TermPair(NWLanguages nwLang, string locale)
        {
            public readonly NWLanguages nwLanguage = nwLang;
            public readonly string translation = string.IsNullOrEmpty(locale) ? null : locale;

            public TermPair(string code, string locale) : this(CodeToNWLang(code), locale) { }
            internal int IDX => GetIndex(nwLanguage);
        }

        public class LocaleCategory(string c)
        {
            readonly public string category = c;

            public string Term(string term) => $"{category}/{term}";

            public string T(string term, bool fixForRTL = true, int maxLineLengthForRTL = 0, bool ignoreRTLnumbers = true, bool applyParameters = false, GameObject localParametersRoot = null, string overrideLanguage = null, bool allowLocalizedParameters = true)
                => GetTranslation(term, fixForRTL, maxLineLengthForRTL, ignoreRTLnumbers, applyParameters, localParametersRoot, overrideLanguage, allowLocalizedParameters);

            public string GetTranslation(string term, bool fixForRTL = true, int maxLineLengthForRTL = 0, bool ignoreRTLnumbers = true, bool applyParameters = false, GameObject localParametersRoot = null, string overrideLanguage = null, bool allowLocalizedParameters = true)
            {
                if (!LocalizationManager.TryGetTranslation(Term(term), out var ret, fixForRTL, maxLineLengthForRTL, ignoreRTLnumbers, applyParameters, localParametersRoot, overrideLanguage, allowLocalizedParameters))
                    ret = Term(term);
                return ret;
            }

            public void AddTerm(string term, params TermPair[] pairs)
            {
                NeonLite.Logger.DebugMsg($"AddTerm {Term(term)} Paircount {pairs.Length}");

                var data = modsSource.AddTerm(Term(term));
                foreach (var p in pairs)
                {
                    NeonLite.Logger.DebugMsg($"- {p.nwLanguage}: {p.translation}");
                    data.SetTranslation(p.IDX, p.translation);
                }

                RelocalizeTimer.timer = 1f;
            }
        }

        public delegate void StringRead(LocaleCategory category, string str);
        public delegate void StreamRead(LocaleCategory category, Stream str);

        public static void Reader_CSVStream(LocaleCategory category, Stream csv)
        {
            using var sReader = new StreamReader(csv);
            using var cur = CsvDataReader.Create(sReader);

            var schema = cur.GetColumnSchema();

            int keyCol = 0;
            Dictionary<int, NWLanguages> mapping = [];

            for (int i = 0; i < schema.Count; ++i)
            {
                var column = schema[i].ColumnName;
                NeonLite.Logger.DebugMsg(column);

                var lower = column.ToLower();
                if (lower.StartsWith("identifier") || lower.StartsWith("key"))
                    keyCol = i;
                else if (lower == "text" || lower == "base" || lower.Contains("source"))
                    mapping.Add(i, NWLanguages.English);
                else if (CodeToNWLang(lower) != NWLanguages.Unknown)
                    mapping.Add(i, CodeToNWLang(lower));
                else if (languageNames.Contains(column))
                    mapping.Add(i, (NWLanguages)Array.IndexOf(languageNames, column));
            }

            while (cur.Read())
            {
                var pairs = mapping.Select(kv => new TermPair(kv.Value, cur.GetString(kv.Key)));
                category.AddTerm(cur.GetString(keyCol), [.. pairs]);
            }
        }

        public static void Reader_CSVString(LocaleCategory category, string csv)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
            Reader_CSVStream(category, stream);
        }

        public static void Reader_JSONString(LocaleCategory category, string json)
        {
            var j = JSON.Load(json) as ProxyObject;
            List<TermPair> termBuf = new((int)NWLanguages.Count);

            foreach (var kv in j)
            {
                var term = kv.Key;
                var obj = kv.Value as ProxyObject;

                foreach (var kv2 in obj)
                {
                    var lang = NWLanguages.Unknown;
                    if (CodeToNWLang(kv2.Key) != NWLanguages.Unknown)
                        lang = CodeToNWLang(kv2.Key);
                    else if (languageNames.Contains(kv2.Key))
                        lang = (NWLanguages)Array.IndexOf(languageNames, kv2.Key);

                    if (lang == NWLanguages.Unknown)
                        continue;

                    termBuf.Add(new TermPair(lang, (string)kv2.Value));
                }

                category.AddTerm(term, [.. termBuf]);
                termBuf.Clear();
            }
        }

        public static void Reader_JSONStream(LocaleCategory category, Stream json)
        {
            using var reader = new StreamReader(json);
            Reader_JSONString(category, reader.ReadToEnd());
        }

        public static LocaleCategory GetLocale_String(string lcName, StringRead reader, string text, string URL = null)
        {
            var lc = new LocaleCategory(lcName);
            try
            {
                reader(lc, text);
            }
            catch (Exception e)
            {
                NeonLite.Logger.Error($"Error loading locale for {lcName}");
                NeonLite.Logger.Warning(e);
            }
            if (URL != null)
            {
                Helpers.DownloadURL(URL, req =>
                {
                    if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        NeonLite.Logger.Warning($"Failed to download locale for {lcName}.");
                        return;
                    }

                    try
                    {
                        reader(lc, req.downloadHandler.text);
                    }
                    catch (Exception e)
                    {
                        NeonLite.Logger.Error($"Error loading locale for {lcName}");
                        NeonLite.Logger.Warning(e);
                    }
                });
            }
            return lc;
        }

        public static LocaleCategory GetLocale_Stream(string lcName, StreamRead reader, Stream stream, string URL = null)
        {
            var lc = new LocaleCategory(lcName);
            try
            {
                reader(lc, stream);
            }
            catch (Exception e)
            {
                NeonLite.Logger.Error($"Error loading locale for {lcName}");
                NeonLite.Logger.Warning(e);
            }
            if (URL != null)
            {
                Helpers.DownloadURL(URL, req =>
                {
                    if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        NeonLite.Logger.Warning($"Failed to download locale for {lcName}.");
                        return;
                    }

                    try
                    {
                        using var reqS = new MemoryStream(req.downloadHandler.data);
                        reader(lc, reqS);
                    }
                    catch (Exception e)
                    {
                        NeonLite.Logger.Error($"Error loading locale for {lcName}");
                        NeonLite.Logger.Warning(e);
                    }
                });
            }
            return lc;
        }
    }
}
