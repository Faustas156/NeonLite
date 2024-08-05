using Boxophobic.StyledGUI;
using HarmonyLib;
using I2.Loc;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace NeonLite.Modules.Misc
{
    // ORIGINAL CODE BY PUPPYPOWERTOOLS AUTHOR HECATE/PANDORAS FOX
    internal class Speedometer : MonoBehaviour, IModule
    {
        static Speedometer instance;
#pragma warning disable CS0414
        const bool priority = false;

        static bool active = false;
        static bool tried = false;

        static GameObject prefab;
        static int altfbi = -1;

        Canvas c;
        CanvasGroup cg;
        bool show = false;

        static LevelData lastLevel;

        static MelonPreferences_Entry<float> xp;
        static MelonPreferences_Entry<float> yp;
        static MelonPreferences_Entry<float> alpha;
        static MelonPreferences_Entry<float> scale;

        static MelonPreferences_Entry<Color> defaultC;
        static MelonPreferences_Entry<Color> dashingC;
        static MelonPreferences_Entry<Color> fastC;
        static MelonPreferences_Entry<Color> slowC;
        static MelonPreferences_Entry<bool> oldStyle;
        static MelonPreferences_Entry<bool> minimal;

        static MelonPreferences_Entry<bool> position;
        static MelonPreferences_Entry<bool> rotation;
        static MelonPreferences_Entry<bool> rawVel;
        static MelonPreferences_Entry<bool> lateral;
        static MelonPreferences_Entry<bool> yVel;
        static MelonPreferences_Entry<bool> swapTimer;
        static MelonPreferences_Entry<bool> coyoteTimer;

        Position positionI;
        Rotation rotationI;
        FullVelocity rawVelI;
        Lateral lateralI;
        YVelocity yVelI;
        SwapTimer swapTimerI;
        CoyoteTimer coyoteTimerI;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Speedometer", "enabled", "Speedometer", "Enables displaying additional information about the player.", false);
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;

            alpha = Settings.Add(Settings.h, "Speedometer", "alpha", "Opacity", null, 1f, new MelonLoader.Preferences.ValueRange<float>(0, 1));
            scale = Settings.Add(Settings.h, "Speedometer", "scale", "Scale", null, 1f, new MelonLoader.Preferences.ValueRange<float>(0, 5));

            xp = Settings.Add(Settings.h, "Speedometer", "x", "X Position", null, 1f, new MelonLoader.Preferences.ValueRange<float>(0, 1));
            yp = Settings.Add(Settings.h, "Speedometer", "y", "Y Position", null, 1f, new MelonLoader.Preferences.ValueRange<float>(0, 1));

            defaultC = Settings.Add(Settings.h, "Speedometer", "flatColor", "Text color (Default)", null, Color.yellow);
            dashingC = Settings.Add(Settings.h, "Speedometer", "dashColor", "Text color (Dashing)", null, Color.blue);
            fastC = Settings.Add(Settings.h, "Speedometer", "fastColor", "Text color (Fast)", null, Color.green);
            slowC = Settings.Add(Settings.h, "Speedometer", "slowColor", "Text color (Slow)", null, Color.red);
            oldStyle = Settings.Add(Settings.h, "Speedometer", "oldStyle", "Use Arial Font", "Enable to somewhat replicate how it looks in PuppyPowertools.", true);
            oldStyle.OnEntryValueChanged.Subscribe((_, _) => Relocalize());
            minimal = Settings.Add(Settings.h, "Speedometer", "minimal", "Minimal", "Gets rid of most text and only show numbers.", false);

            position = Settings.Add(Settings.h, "Speedometer", "position", "Show Position", null, true);
            rotation = Settings.Add(Settings.h, "Speedometer", "rotation", "Show Rotation", null, false);
            rawVel = Settings.Add(Settings.h, "Speedometer", "rawVel", "Show Full Velocity", null, false);
            lateral = Settings.Add(Settings.h, "Speedometer", "lateral", "Show Lateral Velocity", null, true);
            yVel = Settings.Add(Settings.h, "Speedometer", "yVel", "Show Vertical Velocity", null, true);
            swapTimer = Settings.Add(Settings.h, "Speedometer", "swapTimer", "Show Swap Timer", null, false);
            coyoteTimer  = Settings.Add(Settings.h, "Speedometer", "coyoteTimer", "Show Coyote Timer", null, true);

            NeonLite.OnBundleLoad += bundle =>
            {
                prefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/Speedometer.prefab");
                if (tried)
                    Activate(true);
                if (Localization.fbi != -1)
                    PrepareAltFont();
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
                    Utils.InstantiateUI(prefab, "Speedometer", NeonLite.mmHolder.transform).AddComponent<Speedometer>();
                    NeonLite.Harmony.Patch(original, postfix: Helpers.HM(OnPlaying));
                }
            }
            else
            {
                NeonLite.Harmony.Unpatch(original, Helpers.MI(OnPlaying));
                NeonLite.Game.winAction -= LevelWin;
                if (instance)
                    Destroy(instance.gameObject);
            }
        }

        static void OnPlaying(MainMenu.State newState)
        {
            if (newState == MainMenu.State.None && lastLevel && lastLevel.type != LevelData.LevelType.Hub)
                instance.show = true;
            else
                instance.show = false;
        }
        static void LevelWin() => instance.show = false;
        static void OnLevelLoad(LevelData level)
        {
            lastLevel = level;
            instance.show = false;
            if (altfbi == -1 && Localization.fbi != -1)
                PrepareAltFont();
        }

        static void Relocalize()
        {
            if (instance == null)
                return;
            instance.positionI?.ChangeFont();
            instance.rotationI?.ChangeFont();
            instance.rawVelI?.Localize();
            instance.lateralI?.Localize();
            instance.yVelI?.Localize();
            instance.swapTimerI?.Localize();
            instance.coyoteTimerI?.Localize();
        }

        static void PrepareAltFont()
        {
            (_, altfbi) = Localization.AddFontSet(null);
            var fbs = Localization.fbs; // this copies!
            var buffer = NeonLite.bundle.LoadAsset<TMP_FontAsset>("Assets/Fonts/ARIALBD SDF.asset");
            fbs.english = buffer;
            fbs.englishFontMats = [buffer.material];
            Localization.UpdateFontSet(fbs, altfbi);
            Relocalize();
        }

        void Awake() => instance = this;

        void Start()
        {
            c = GetComponentInParent<Canvas>();
            cg = GetComponent<CanvasGroup>();

            positionI = transform.Find("Position").GetOrAddComponent<Position>();
            rotationI = transform.Find("Rotation").GetOrAddComponent<Rotation>();
            rawVelI = transform.Find("Velocity").GetOrAddComponent<FullVelocity>();
            lateralI = transform.Find("LateralVel").GetOrAddComponent<Lateral>();
            yVelI = transform.Find("VerticalVel").GetOrAddComponent<YVelocity>();
            swapTimerI = transform.Find("SwapTimer").GetOrAddComponent<SwapTimer>();
            coyoteTimerI = transform.Find("CoyoteTimer").GetOrAddComponent<CoyoteTimer>();
        }

        void Update()
        {
            transform.localPosition = c.ViewportToCanvasPosition(new Vector3(xp.Value, 1 - yp.Value, 0));
            transform.localScale = new(scale.Value, scale.Value, 1);
            cg.alpha = show ? alpha.Value : 0;

            positionI.gameObject.SetActive(position.Value && show);
            rotationI.gameObject.SetActive(rotation.Value && show);
            rawVelI.gameObject.SetActive(rawVel.Value && show);
            lateralI.gameObject.SetActive(lateral.Value && show);
            yVelI.gameObject.SetActive(yVel.Value && show);
            swapTimerI.gameObject.SetActive(swapTimer.Value && show);
            coyoteTimerI.gameObject.SetActive(coyoteTimer.Value && show);
        }

        class Position : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            TextMeshProUGUI text;
            StringBuilder sb = new();
            void Awake() => text = GetComponent<TextMeshProUGUI>();
            void Start() => AxKLocalizedTextLord.GetInstance().AddText(this);
            void LateUpdate()
            {
                var pos = RM.playerPosition;
                sb.Clear();
                sb.Append(pos.x.ToString("0.00"));
                sb.Append(", ");
                sb.Append(pos.y.ToString("0.00"));
                sb.Append(", ");
                sb.Append(pos.z.ToString("0.00"));
                text.text = sb.ToString();
                text.color = defaultC.Value;
            }
            public void Localize() => ChangeFont();
            public void SetKey(string key) { }
            public void ChangeFont() => text.font = AxKLocalizedTextLord.GetInstance().fontLib.GetReplacement_TMP_FONT(oldStyle.Value ? altfbi : Localization.fbi);
        }
        class Rotation : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            TextMeshProUGUI text;
            readonly StringBuilder sb = new();
            void Awake() => text = GetComponent<TextMeshProUGUI>();
            void Start() => AxKLocalizedTextLord.GetInstance().AddText(this);
            void LateUpdate()
            {
                var rot = RM.mechController.playerCamera.PlayerCam.transform.forward;
                sb.Clear();
                sb.Append(RM.drifter.mouseLookX.RotationX.ToString("0.00"));
                sb.Append(", ");
                sb.Append(RM.drifter.mouseLookY.RotationY.ToString("0.00"));
                sb.Append(" | ");
                sb.Append(rot.x.ToString("0.00"));
                sb.Append(", ");
                sb.Append(rot.y.ToString("0.00"));
                sb.Append(", ");
                sb.Append(rot.z.ToString("0.00"));
                text.text = sb.ToString();
                text.color = defaultC.Value;
            }
            public void Localize() => ChangeFont();
            public void SetKey(string key) { }
            public void ChangeFont() => text.font = AxKLocalizedTextLord.GetInstance().fontLib.GetReplacement_TMP_FONT(oldStyle.Value ? altfbi : Localization.fbi);
        }
        class FullVelocity : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            static FullVelocity instance;
            TextMeshProUGUI text;
            string localizeCache;
            readonly StringBuilder sb = new();
            void Awake() => text = GetComponent<TextMeshProUGUI>();
            void Start()
            {
                instance = this;
                AxKLocalizedTextLord.GetInstance().AddText(this);
                Localize();
            }
            void FixedUpdate()
            {
                var vel = RM.drifter.Motor.BaseVelocity;
                sb.Clear();
                sb.Append(vel.x.ToString("0.00"));
                sb.Append(", ");
                sb.Append(vel.y.ToString("0.00"));
                sb.Append(", ");
                sb.Append(vel.z.ToString("0.00"));
                text.text = minimal.Value ? sb.ToString() : localizeCache.Replace("{0}", sb.ToString());
                text.color = defaultC.Value;
            }
            internal static void Relocalize() => instance?.Localize();
            public void Localize()
            {
                SetKey("NeonLite/SPEEDOMETER_VELOCITY");
                ChangeFont();
            }
            public void SetKey(string key) => localizeCache = LocalizationManager.GetTranslation(key);
            public void ChangeFont() => text.font = AxKLocalizedTextLord.GetInstance().fontLib.GetReplacement_TMP_FONT(oldStyle.Value ? altfbi : Localization.fbi);
        }
        class Lateral : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            static Lateral instance;
            TextMeshProUGUI text;
            string localizeCache;
            readonly StringBuilder sb = new();
            Vector2 v = new();
            void Awake() => text = GetComponent<TextMeshProUGUI>();
            void Start()
            {
                instance = this;
                AxKLocalizedTextLord.GetInstance().AddText(this);
                Localize();
            }
            void FixedUpdate()
            {
                var vel = RM.drifter.Motor.BaseVelocity;
                v.x = vel.x;
                v.y = vel.z;
                float m = v.magnitude;
                text.text = minimal.Value ? m.ToString("0.00") : localizeCache.Replace("{0}", m.ToString("0.00"));
                if (RM.drifter.GetIsDashing())
                    text.color = dashingC.Value;
                else if (m > 18.76)
                    text.color = fastC.Value;
                else if (m < 18.7)
                    text.color = slowC.Value;
                else
                    text.color = defaultC.Value;
            }
            internal static void Relocalize() => instance?.Localize();
            public void Localize()
            {
                SetKey("NeonLite/SPEEDOMETER_LATERAL");
                ChangeFont();
            }
            public void SetKey(string key) => localizeCache = LocalizationManager.GetTranslation(key);
            public void ChangeFont() => text.font = AxKLocalizedTextLord.GetInstance().fontLib.GetReplacement_TMP_FONT(oldStyle.Value ? altfbi : Localization.fbi);
        }

        class YVelocity : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            static YVelocity instance;
            TextMeshProUGUI text;
            string localizeCache;
            readonly StringBuilder sb = new();
            void Awake() => text = GetComponent<TextMeshProUGUI>();
            void Start()
            {
                instance = this;
                AxKLocalizedTextLord.GetInstance().AddText(this);
                Localize();
            }
            void FixedUpdate()
            {
                var vel = RM.drifter.Motor.BaseVelocity.y;
                text.text = minimal.Value ? vel.ToString("0.00") : localizeCache.Replace("{0}", vel.ToString("0.00"));
                if (RM.drifter.GetIsDashing())
                    text.color = dashingC.Value;
                else if (vel > 0.1)
                    text.color = fastC.Value;
                else if (vel < -0.1)
                    text.color = slowC.Value;
                else
                    text.color = defaultC.Value;
            }
            internal static void Relocalize() => instance?.Localize();
            public void Localize()
            {
                SetKey("NeonLite/SPEEDOMETER_YVEL");
                ChangeFont();
            }
            public void SetKey(string key) => localizeCache = LocalizationManager.GetTranslation(key);
            public void ChangeFont() => text.font = AxKLocalizedTextLord.GetInstance().fontLib.GetReplacement_TMP_FONT(oldStyle.Value ? altfbi : Localization.fbi);
        }
        class SwapTimer : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            static SwapTimer instance;
            TextMeshProUGUI text;
            string localizeCache;
            readonly StringBuilder sb = new();
            static readonly FieldInfo stField = AccessTools.Field(typeof(MechController), "weaponReloadTimer");
            void Awake() => text = GetComponent<TextMeshProUGUI>();
            void Start()
            {
                instance = this;
                AxKLocalizedTextLord.GetInstance().AddText(this);
                Localize();
            }
            void LateUpdate()
            {
                var vel = Math.Max(0, (float)stField.GetValue(RM.mechController));
                text.text = minimal.Value ? vel.ToString("0.000") : localizeCache.Replace("{0}", vel.ToString("0.000"));
                text.color = defaultC.Value;
            }
            internal static void Relocalize() => instance?.Localize();
            public void Localize()
            {
                SetKey("NeonLite/SPEEDOMETER_SWAP");
                ChangeFont();
            }
            public void SetKey(string key) => localizeCache = LocalizationManager.GetTranslation(key);
            public void ChangeFont() => text.font = AxKLocalizedTextLord.GetInstance().fontLib.GetReplacement_TMP_FONT(oldStyle.Value ? altfbi : Localization.fbi);
        }

        class CoyoteTimer : MonoBehaviour, AxKLocalizedTextObject_Interface
        {
            static CoyoteTimer instance;
            TextMeshProUGUI text;
            string localizeCache;
            readonly StringBuilder sb = new();
            static readonly FieldInfo cyField = AccessTools.Field(typeof(FirstPersonDrifter), "jumpForgivenessTimer");
            void Awake() => text = GetComponent<TextMeshProUGUI>();
            void Start()
            {
                instance = this;
                AxKLocalizedTextLord.GetInstance().AddText(this);
                Localize();
            }
            void LateUpdate()
            {
                var vel = Math.Max(0, (float)cyField.GetValue(RM.drifter));
                text.text = minimal.Value ? vel.ToString("0.000") : localizeCache.Replace("{0}", vel.ToString("0.000"));
                if (vel > 0)
                    text.color = fastC.Value;
                else
                    text.color = slowC.Value;
            }
            internal static void Relocalize() => instance?.Localize();
            public void Localize()
            {
                SetKey("NeonLite/SPEEDOMETER_COYOTE");
                ChangeFont();
            }
            public void SetKey(string key) => localizeCache = LocalizationManager.GetTranslation(key);
            public void ChangeFont() => text.font = AxKLocalizedTextLord.GetInstance().fontLib.GetReplacement_TMP_FONT(oldStyle.Value ? altfbi : Localization.fbi);
        }
    }
}
