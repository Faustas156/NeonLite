using HarmonyLib;
using I2.Loc;
using MelonLoader;
using MelonLoader.TinyJSON;
using NeonLite.Modules.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;

namespace NeonLite.Modules.Misc
{
    internal class GameInfo : MonoBehaviour, IModule
    {
        static GameInfo instance;
#pragma warning disable CS0414
        const bool priority = false;
        static bool active = false;
        static bool tried = false;

        static GameObject prefab;

        Canvas c;
        CanvasGroup cg;

        static LevelData lastLevel;
        static string lastLevelID;

        static MelonPreferences_Entry<float> alpha;
        static MelonPreferences_Entry<float> scale;

        static MelonPreferences_Entry<bool> seshTimer;
        static MelonPreferences_Entry<bool> levelTimer;
        static MelonPreferences_Entry<bool> totalAttempts;
        static MelonPreferences_Entry<bool> seshAttempts;
        static MelonPreferences_Entry<bool> showCompleted;
        static MelonPreferences_Entry<bool> seshPB;

        SessionTimer seshTimerI;
        LevelTimer levelTimerI;
        SessionPB seshPBI;
        AttemptsTotal totalAttemptsI;
        AttemptsNow seshAttemptsI;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "UI/Info", "enabled", "Enable Game Info", "Enables additional information about the game and current level in the top left.", true);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;

            alpha = Settings.Add(Settings.h, "UI/Info", "alpha", "Opacity", null, 1f, new MelonLoader.Preferences.ValueRange<float>(0, 1));
            scale = Settings.Add(Settings.h, "UI/Info", "scale", "Scale", null, 1f, new MelonLoader.Preferences.ValueRange<float>(0, 5));
            seshTimer = Settings.Add(Settings.h, "UI/Info", "seshTimer", "Session Timer", "Shows how long the game's been open for.", true);
            levelTimer = Settings.Add(Settings.h, "UI/Info", "levelTimer", "Level Timer", "Shows how long you've been on this stage.", true);
            seshPB = Settings.Add(Settings.h, "UI/Info", "seshPB", "SessionPB", "Shows the best time you got for a level in this session.", true);
            totalAttempts = Settings.Add(Settings.h, "UI/Info", "totalAttempts", "Show Total Restarts", "Shows how many attempts you've made for this stage.", true);
            seshAttempts = Settings.Add(Settings.h, "UI/Info", "seshAttempts", "Show Session Restarts", "Shows how many attempts you've made for this stage this session.", true);
            showCompleted = Settings.Add(Settings.h, "UI/Info", "showCompleted", "Show # of Finishes", "Additionally shows the amount of completed finishes on the session attempts.", true);
            showCompleted.OnEntryValueChanged.Subscribe((_, _) => AttemptsNow.Relocalize());

            NeonLite.OnBundleLoad += bundle =>
            {
                if (NeonLite.DEBUG)
                    NeonLite.Logger.Msg("GameInfo onBundleLoad");

                prefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/InfoText.prefab");
                if (tried)
                    Activate(true);
            };
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(MainMenu), "SetState");

        static void Activate(bool activate)
        {
            active = activate;
            tried = true;

            if (activate)
            {
                if (prefab && !instance)
                {
                    NeonLite.Game.winAction += LevelWin;
                    Utils.InstantiateUI(prefab, "GameInfo", NeonLite.mmHolder.transform).AddComponent<GameInfo>();
                }
            }
            else
            {
                NeonLite.Game.winAction -= LevelWin;
                if (instance)
                    Destroy(instance.gameObject);
            }
        }
        static void LevelWin()
        {
            long ms = NeonLite.Game.GetCurrentLevelTimerMicroseconds();
            long levelPB = SessionPB.pbs[lastLevel.levelID];

            if (ms >= levelPB) return;
            SessionPB.pbs[lastLevel.levelID] = ms;
            if (instance.seshPBI)
            {
                instance.seshPBI.time = ms;
                instance.seshPBI.UpdateText();
            }
        }

        static void OnLevelLoad(LevelData level)
        {
            if (!instance || !level || level.levelID == lastLevelID)
            {
                lastLevel = level;
                lastLevelID = level?.levelID;
                return;
            }

            lastLevel = level;
            lastLevelID = level.levelID;

            if (!SessionPB.pbs.ContainsKey(level.levelID))
            {
                instance.seshPBI.time = 0;
                SessionPB.pbs.Add(level.levelID, long.MaxValue);
            }
            else
                instance.seshPBI.time = SessionPB.pbs[level.levelID];
            instance.seshPBI.UpdateText();

            instance.levelTimerI.time = 0;
        }

        void Awake() => instance = this;

        void Start()
        {
            c = GetComponentInParent<Canvas>();
            cg = GetComponent<CanvasGroup>();

            seshTimerI = transform.Find("SessionTimer").GetOrAddComponent<SessionTimer>();
            levelTimerI = transform.Find("LevelTimer").GetOrAddComponent<LevelTimer>();
            seshPBI = transform.Find("SessionPB").GetOrAddComponent<SessionPB>();
            totalAttemptsI = transform.Find("AttemptsTotal").GetOrAddComponent<AttemptsTotal>();
            seshAttemptsI = transform.Find("AttemptsNow").GetOrAddComponent<AttemptsNow>();
        }

        void Update()
        {
            transform.localPosition = c.ViewportToCanvasPosition(new Vector3(0f, 1f, 0));
            transform.localScale = new(scale.Value, scale.Value, 1);
            cg.alpha = alpha.Value;

            seshTimerI.gameObject.SetActive(seshTimer.Value);
            levelTimerI.gameObject.SetActive(levelTimer.Value && lastLevel && lastLevel.type != LevelData.LevelType.Hub);
            seshPBI.gameObject.SetActive(seshPB.Value && lastLevel && lastLevel.type != LevelData.LevelType.Hub);
            totalAttemptsI.gameObject.SetActive(totalAttempts.Value && lastLevel && lastLevel.type != LevelData.LevelType.Hub);
            seshAttemptsI.gameObject.SetActive(seshAttempts.Value && lastLevel && lastLevel.type != LevelData.LevelType.Hub);
        }


        public class RestartManager : MonoBehaviour, IModule
        {
#pragma warning disable CS0414
            const bool priority = false;
            const bool active = true;

            static string path;

            static LevelData lastLevel;

            public struct RestartInfo
            {
                public int total;
                public int queued;
                public int session;
                public int completed;
            }

            public static Dictionary<string, RestartInfo> restarts = [];

            static void Setup() { }

            static void Activate(bool _)
            {
                path = Path.Combine(Helpers.GetSaveDirectory(), "NeonLite", "restartcounter.json");
                Helpers.CreateDirectories(path);
                if (!File.Exists(path) || !Load(File.ReadAllText(path)))
                {
                    if (!File.Exists(path + ".bak") || !Load(File.ReadAllText(path + ".bak")))
                        NeonLite.Logger.Error("Failed to load restart counter.");
                }

                NeonLite.holder.AddComponent<RestartManager>();

                NeonLite.Game.winAction += () =>
                {
                    var ri = restarts[lastLevel.levelID];
                    ri.completed++;
                    restarts[lastLevel.levelID] = ri;
                    instance?.seshAttemptsI.UpdateText(ri.session, ri.completed);
                    Save();
                };
            }

            void OnDestroy() => Save();

            static void OnLevelLoad(LevelData level)
            {
                lastLevel = level;
                if (!level || level.type == LevelData.LevelType.Hub)
                    Save();
                else
                {
                    restarts.TryGetValue(level.levelID, out var ri);
                    ri.queued++;
                    ri.session++;
                    restarts[level.levelID] = ri;
                    instance?.totalAttemptsI.UpdateText(ri.total + ri.queued);
                    instance?.seshAttemptsI.UpdateText(ri.session, ri.completed);
                }
            }

            static void Save()
            {
                ProxyObject obj = [];

                foreach (var level in restarts.Keys.ToList())
                {
                    var ri = restarts[level];
                    ri.total += ri.queued;
                    ri.queued = 0;
                    restarts[level] = ri;
                    obj[level] = new ProxyNumber(ri.total);
                }

                if (File.Exists(path))
                {
                    if (File.Exists(path + ".bak"))
                        File.Delete(path + ".bak");
                    File.Move(path, path + ".bak");
                }

                File.WriteAllText(path, JSON.Dump(obj, EncodeOptions.PrettyPrint));
            }

            static bool Load(string js)
            {

                try
                {
                    var variant = JSON.Load(js) as ProxyObject;
                    foreach (var kv in variant)
                    {
                        RestartInfo ri = new()
                        {
                            total = kv.Value
                        };
                        restarts[kv.Key] = ri;
                    }
                }
                catch (Exception e)
                {
                    NeonLite.Logger.Error("Failed to parse restart counter:");
                    NeonLite.Logger.Error(e);
                    return false;
                }
                return true;
            }

        }

        class SessionTimer : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            TextMeshProUGUI text;
            void Awake() => text = GetComponent<TextMeshProUGUI>();
            void Start() => AxKLocalizedTextLord.GetInstance().AddText(this);
            void LateUpdate() => text.text = Helpers.FormatTime((long)Time.realtimeSinceStartup * 1000, false, ':', false, false);
            public void Localize() => ChangeFont();
            public void SetKey(string key) { }
            public void ChangeFont() => text.font = AxKLocalizedTextLord.GetInstance().fontLib.GetReplacement_TMP_FONT(Localization.fbi);
        }
        class LevelTimer : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            TextMeshProUGUI text;
            internal float time;
            void Awake() => text = GetComponent<TextMeshProUGUI>();
            void Start() => AxKLocalizedTextLord.GetInstance().AddText(this);
            void Update() => time += Time.deltaTime;
            void FixedUpdate() => text.text = Helpers.FormatTime((long)(time * 1000), false, ':', false, false);
            public void Localize() => ChangeFont();
            public void SetKey(string key) { }
            public void ChangeFont() => text.font = AxKLocalizedTextLord.GetInstance().fontLib.GetReplacement_TMP_FONT(Localization.fbi);
        }
        class SessionPB : MonoBehaviour
        {
            AxKLocalizedText text;
            internal long time;
            internal static Dictionary<string, long> pbs = [];
            void Start()
            {
                text = gameObject.AddComponent<AxKLocalizedText>();
                text.textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            }
            internal void UpdateText()
            {
                text?.SetKey("NeonLite/INFO_SESSIONPB",
                    [new("{0}",
                        Helpers.FormatTime((time == long.MaxValue ? 0 : time) / 1000, ShowMS.setting.Value, '.', true),
                        false)]);
            }
        }
        class AttemptsNow : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            static AttemptsNow instance;
            string localizeCache = "";
            TextMeshProUGUI text;
            void Awake() => text = GetComponent<TextMeshProUGUI>();
            void Start()
            {
                instance = this;
                AxKLocalizedTextLord.GetInstance().AddText(this);
                Localize();
            }
            internal static void Relocalize() => instance?.Localize();
            public void Localize()
            {
                SetKey(showCompleted.Value ? "NeonLite/INFO_ATTEMPTS_THIS" : "NeonLite/INFO_ATTEMPTS_THIS_NC");
                ChangeFont();
            }
            public void SetKey(string key) => localizeCache = LocalizationManager.GetTranslation(key);
            public void ChangeFont() => text.font = AxKLocalizedTextLord.GetInstance().fontLib.GetReplacement_TMP_FONT(Localization.fbi);

            internal void UpdateText(int tried, int completed) => text.text = localizeCache.Replace("{0}", completed.ToString()).Replace("{1}", tried.ToString());
        }

        class AttemptsTotal : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            string localizeCache = "";
            TextMeshProUGUI text;
            void Awake() => text = GetComponent<TextMeshProUGUI>();
            void Start()
            {
                AxKLocalizedTextLord.GetInstance().AddText(this);
                Localize();
            }
            public void Localize()
            {
                SetKey("NeonLite/INFO_ATTEMPTS_TOTAL");
                ChangeFont();
            }
            public void SetKey(string key) => localizeCache = LocalizationManager.GetTranslation(key);
            public void ChangeFont() => text.font = AxKLocalizedTextLord.GetInstance().fontLib.GetReplacement_TMP_FONT(Localization.fbi);

            internal void UpdateText(int count) => text.text = localizeCache.Replace("{0}", count.ToString());
        }

    }
}
