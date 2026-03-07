using System.Reflection;
using System.Text;
using HarmonyLib;
using I2.Loc;
using MelonLoader;
using NeonLite.Modules.UI.Status;
using Semver;
using TMPro;
using UnityEngine;

#pragma warning disable IDE0130
namespace NeonLite.Modules
{
    [Module(100)]
    public static class Verifier
    {
        const bool priority = false;
        const bool active = true;

        const string STATS_KEY = "nl_verified";
        const string STATS_KEY_PB = "nl_verifiedPB";
        internal static string currentKey = STATS_KEY_PB;

        const string LB_FILE = "nl_verify";

        public static bool Verified { get; private set; }

        public static event Action OnReset;

        static bool prevVerified;
        static MelonPreferences_Entry<bool> force;

        internal static TextMeshProUGUI icon;

        static void Setup()
        {
            force = Settings.Add(Settings.h, "Misc", "forceUnv", "Force Unverified", null, false, true);
            force.OnEntryValueChanged.Subscribe((_, after) => SetVerified(Verified && !after));
            StatusText.OnTextReady += SetIcon;
        }

        static void Activate(bool _)
        {
            GetModList(true);
            NeonLite.Game.winAction += OnLevelWin;

            Patching.AddPatch(typeof(LevelInfo), "SetLevel", Helpers.HM(VerifyLI).SetPriority(Priority.Last), Patching.PatchTarget.Postfix);
            Patching.AddPatch(typeof(MenuScreenResults), "OnSetVisible", Helpers.HM(VerifyMSR).SetPriority(Priority.Last), Patching.PatchTarget.Postfix);
            Patching.AddPatch(typeof(MenuScreenLevelRushComplete), "OnSetVisible", Helpers.HM(VerifyMSLR).SetPriority(Priority.Last), Patching.PatchTarget.Postfix);

            PastSight.OnActive += OnPastSightActive;
#if !XBOX
            SteamLBFiles.OnLBWrite += OnSteamLBWrite;
            SteamLBFiles.RegisterForLoad(LB_FILE, OnSteamLBRead);
#endif
        }

        internal class ModuleData(MethodInfo m, FieldInfo f, Assembly a)
        {
            public MethodInfo method = m;
            public FieldInfo active = f;
            public Assembly assembly = a;
        }
        internal static Dictionary<Type, ModuleData> modules = [];

        public static void AddModules(IEnumerable<Type> modules)
        {
            modules.Where(static t => Helpers.Method(t, "CheckVerifiable") != null).Do(static x =>
            {
                Verifier.modules.Add(x, new(Helpers.Method(x, "CheckVerifiable"), Helpers.Field(x, "active"), x.Assembly));
            });
        }

        public static TMP_SpriteAsset SpriteAsset { get; internal set; }

        static void OnPastSightActive(bool active)
        {
            if (active)
                currentKey = STATS_KEY;
            else
                currentKey = STATS_KEY_PB;
        }

        static bool awaitLevelInfo;
        static void OnLevelWin()
        {
            if (LevelRush.IsLevelRush())
            {
                if (LevelRush.HasBeatenFinalLevelRushLevel_PreadvanceCheck())
                {
                    if (!PrintVerifications())
                        NeonLite.Logger.Msg($"(Above log produced for rush clear)");

                    prevVerified = Verified;
                }
                else
                    otherRush.UnionWith(other);
                return;
            }

            var level = NeonLite.Game.GetCurrentLevel();
            if (!level)
                return; // now how'd you do that

            if (!PrintVerifications())
            {
                string lname = LocalizationManager.GetTranslation(level.GetLevelDisplayName());
                if (string.IsNullOrEmpty(lname))
                    lname = level.levelDisplayName;

                NeonLite.Logger.Msg($"(Above log produced for {lname} clear)");
            }

            level.StatsSetCustom(STATS_KEY, Verified);
            awaitLevelInfo = true;
            prevVerified = Verified;
        }

        static bool wasVerified;
        static void OnLevelLoad(LevelData _)
        {
            wasVerified = Verified;
            if (!fetched)
                GetModList(false);

            if (!LevelRush.IsLevelRush() || LevelRush.GetCurrentLevelRushTimerMicroseconds() <= 0)
                otherRush.Clear();

            CheckVerifications(CheckVStatus.Other);
        }

        class ModInfo(string n, SemVersion ver)
        {
            public string name = n;
            public SemVersion version = ver;
        }
        static readonly List<ModInfo> verifiedMods = [];
        static bool fetched = false;
        const string filename = "verifiedmods.txt";
        const string URL = "https://raw.githubusercontent.com/Faustas156/NeonLite/main/Resources/" + filename;

        static void GetModList(bool print = true, Action cb = null)
        {
            Helpers.DownloadURL(URL, request =>
            {
                string backup = Path.Combine(Helpers.GetSaveDirectory(), "NeonLite", filename);
                Helpers.CreateDirectories(backup);
                var load = request.result == UnityEngine.Networking.UnityWebRequest.Result.Success && Load(request.downloadHandler.text, print);
                if (load)
                {
                    File.WriteAllText(backup, request.downloadHandler.text);
                    fetched = true;
                }
                else if (!File.Exists(backup) || !Load(File.ReadAllText(backup), print))
                {
                    if (print)
                        NeonLite.Logger.Warning("Could not load up to date verified mod list. Loading the backup resource; this could be really outdated!");

                    var resource = Resources.r.verifiedmods;
                    if (!Load(resource, print) && print)
                        NeonLite.Logger.Error("Failed to load the verified mod list.");
                }

                cb?.Invoke();
            });
        }

        static bool Load(string text, bool print = true)
        {
            verifiedMods.Clear();

            var reader = new StringReader(text);

            try
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("//") || string.IsNullOrEmpty(line))
                        continue;

                    if (line.Contains("//"))
                        line = line.Substring(0, line.IndexOf("//"));

                    var split = line.Split();

                    if (split.Length > 1)
                    {
                        if (SemVersion.TryParse(split.Last().Trim(), out var version))
                            verifiedMods.Add(new(string.Join(" ", split.Take(split.Length - 1)).Trim(), version));
                        else
                            verifiedMods.Add(new(line, null));
                    }
                    else
                        verifiedMods.Add(new(line, null));
                }
            }
            catch
            {
                return false;
            }

            CheckVerifications(CheckVStatus.Mods, print);
            return true;
        }

        static readonly Dictionary<MelonAssembly, Func<string>> addedAssemblies = [];

        static readonly List<string> unknown = [];
        static readonly List<(ModInfo, SemVersion)> outdated = [];
        static readonly HashSet<string> other = [];
        static readonly HashSet<string> otherRush = [];

        static void AddToOther(string s)
        {
            NeonLite.Logger.BetaMsg($"Unverifiable status added: {s}");
            if (LevelRush.IsLevelRush())
            {
                var level = LevelRush.GetCurrentLevelRushLevelData();

                string lname = LocalizationManager.GetTranslation(level.GetLevelDisplayName());
                if (string.IsNullOrEmpty(lname))
                    lname = level.levelDisplayName;

                other.Add($"{s} ({lname})");
                return;
            }
            other.Add(s);
        }

        /// <summary>
        /// Checked on level and game startup.
        /// </summary>
        public static void SetVerificationCheck(MelonAssembly assembly, Func<string> status)
        {
            if (addedAssemblies.ContainsKey(assembly))
                addedAssemblies[assembly] = status;
            else
                addedAssemblies.Add(assembly, status);
        }


        /// <summary>
        /// Can be ran at any time to set the **current run** to be unverifiable, using a melon assembly
        /// </summary>
        public static void SetRunUnverifiable(MelonAssembly assembly, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return;

            AddToOther($"{assembly.Assembly.GetName().Name}: {status}");
            SetVerified(false);
        }
        /// <summary>
        /// Can be ran at any time to set the **current run** to be unverifiable, using a module type
        /// </summary>
        public static void SetRunUnverifiable(Type module, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return;

            AddToOther($"{module.Assembly.GetName().Name} {module.Name}: {status}");
            SetVerified(false);
        }


        /// <summary>
        /// Can be ran at any time to set the **current rush** to be unverifiable, using a melon assembly
        /// </summary>
        public static void SetRushUnverifiable(MelonAssembly assembly, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return;

            otherRush.Add($"{assembly.Assembly.GetName().Name}: {status}");
            SetVerified(false);
        }
        /// <summary>
        /// Can be ran at any time to set the **current rush** to be unverifiable, using a module type
        /// </summary>
        public static void SetRushUnverifiable(Type module, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return;

            otherRush.Add($"{module.Assembly.GetName().Name} {module.Name}: {status}");
            SetVerified(false);
        }

        [Flags]
        internal enum CheckVStatus
        {
            Mods = 1 << 0,
            Other = 1 << 1
        }

        internal static bool CheckVerifications(CheckVStatus check, bool print = false)
        {
            if (check.HasFlag(CheckVStatus.Mods))
            {
                unknown.Clear();
                outdated.Clear();
            }

            if (check.HasFlag(CheckVStatus.Other))
            {
                OnReset?.Invoke();
                other.Clear();
            }

            try
            {
                if (check.HasFlag(CheckVStatus.Mods))
                {
                    foreach (var mod in MelonTypeBase<MelonMod>.RegisteredMelons)
                    {
                        if (mod == null)
                            continue;

                        var name = mod.MelonAssembly.Assembly.GetName().Name;
                        var verified = verifiedMods.FirstOrDefault(x => x.name == name);
                        NeonLite.Logger.DebugMsg($"VERIFY CHECK {name} {verified}");
                        if (verified == null)
                            unknown.Add(name);
                        else if (verified.version != null && verified.version > mod.Info.SemanticVersion)
                            outdated.Add((verified, mod.Info.SemanticVersion));
                    }
                }

                if (check.HasFlag(CheckVStatus.Other))
                {
                    foreach (var kv in modules)
                    {
                        var module = kv.Key;
                        if (!(bool)kv.Value.active.GetValue(null))
                            continue;

                        NeonLite.Logger.DebugMsg($"{module} CheckVerifiable");

                        Helpers.StartProfiling($"{module} CV");

                        try
                        {
                            var ret = kv.Value.method.Invoke(null, []);
                            bool ok = false;
                            if (ret != null)
                            {
                                if (ret.GetType() == typeof(string))
                                    ok = string.IsNullOrWhiteSpace((string)ret);
                                else
                                    throw new InvalidOperationException("CheckVerifiable did not return a string or null");
                            }
                            else
                                ok = true;

                            if (!ok)
                                AddToOther($"{module.Assembly.GetName().Name} {module.Name}: {ret}");
                        }
                        catch (Exception e)
                        {
                            NeonLite.Logger.Warning($"Error in {module} CheckVerifiable:");
                            NeonLite.Logger.Error(e);
                        }

                        Helpers.EndProfiling();
                    }

                    foreach (var kv in addedAssemblies)
                    {
                        if (kv.Value == null)
                            continue;

                        var response = kv.Value();
                        if (!string.IsNullOrWhiteSpace(response))
                            AddToOther($"{kv.Key.Assembly.GetName().Name}: {kv.Value}");
                    }
                }

                const string FORCE = "Forced unverified";
                if (force.Value && !other.Contains(FORCE))
                    AddToOther(FORCE);

                if (unknown.Any() || outdated.Any() || other.Any() || otherRush.Any())
                {
                    // ohhhhhhhhhhh tough luck buddy
                    SetVerified(false);

                    if (print)
                        PrintVerifications();

                    return false;
                }

                SetVerified(true);
                return true;
            }
            catch (Exception e)
            {
                NeonLite.Logger.Error("Failed to check verification:");
                NeonLite.Logger.Error(e);
                return false;
            }
        }

        public static bool PrintVerifications()
        {
            if (Verified)
                return true;

            NeonLite.Logger.Warning("!!! BEGIN VERIFICATION LOG !!!");
            NeonLite.Logger.Error("Failed to verify for the following reasons:");
            if (unknown.Any())
            {
                StringBuilder unk = new("- The following mods are not verified: ");
                foreach (var s in unknown)
                    unk.Append($"{s}, ");

                unk.Remove(unk.Length - 2, 2);
                NeonLite.Logger.Warning(unk.ToString());
            }

            if (outdated.Any())
            {
                NeonLite.Logger.Warning("- The following mods are too out of date: ");

                foreach (var s in outdated)
                    NeonLite.Logger.Warning($"  - {s.Item1.name} (expected at least {s.Item1.version}, is {s.Item2})");
            }

            foreach (var s in other.Concat(otherRush))
                NeonLite.Logger.Warning($"- {s}");

            NeonLite.Logger.Warning("!!! END VERIFICATION LOG !!!");

            return false;
        }

        static void SetVerified(bool v)
        {
            Verified = v;
            // potentially do other stuff
        }

        class Fader : MonoBehaviour
        {
            const float SPEED = 0.25f;
            float timer = 0;

            void Update()
            {

                if (Verified || !LoadManager.currentLevel || LoadManager.currentLevel.type == LevelData.LevelType.Hub)
                {
                    timer -= Time.unscaledDeltaTime / SPEED;
                    if (timer < 0)
                        timer = 0;
                }
                else
                {
                    timer += Time.unscaledDeltaTime / SPEED;
                    if (timer > 1)
                        timer = 1;
                }


                if (icon.spriteAsset != SpriteAsset)
                    icon.spriteAsset = SpriteAsset;
                else
                    icon.alpha = AxKEasing.EaseInCirc(0, 0.9f, timer);
            }
        }

        static void SetIcon()
        {
            icon = StatusText.i.MakeText("verify", "<sprite=0 color=#FF8080><sprite=2 color=#000000AA>", 100);
            icon.alpha = 0;
            icon.transform.SetAsFirstSibling();
            icon.fontSize = 32;
            icon.geometrySortingOrder = VertexSortingOrder.Reverse;

            icon.GetOrAddComponent<Fader>();
        }


#if !XBOX
        static string OnSteamLBWrite(BinaryWriter writer, SteamLBFiles.LBType type, bool sameScore)
        {
            if (type == SteamLBFiles.LBType.Global || (type == SteamLBFiles.LBType.Rush && sameScore))
                return null;
            var semver = NeonLite.i.Info.SemanticVersion;
            writer.Write((byte)1); // VERSION
            writer.Write((byte)semver.Major);
            writer.Write((byte)semver.Minor);
            writer.Write((byte)semver.Patch);
            writer.Write(string.IsNullOrEmpty(semver.Build) ? (byte)0 : (byte)int.Parse(semver.Build));
            writer.Write(prevVerified);

            return LB_FILE;
        }

        static void OnSteamLBRead(BinaryReader reader, int length, LeaderboardScore score)
        {
            var ver = reader.ReadByte();

            bool verified;

            switch (ver)
            {
                default:
                    NeonLite.Logger.Error($"Unknown verification version {ver}");
                    return;
                case 1:
                    {
                        reader.ReadBytes(4); // major minor patch build
                        verified = reader.ReadBoolean();
                        break;
                    }
            }


            var scv = score._scoreValue;
            scv.spriteAsset = SpriteAsset;

            var pref = scv.GetPreferredValues().x;
            scv.text += $" <sprite={(verified ? 1 : 0)} tint>";
            scv.ForceMeshUpdate();

            scv.rectTransform.ResizeWithPivot(new Vector2(scv.GetPreferredValues().x - pref, 0));
        }
#endif

        static void VerifyLI(LevelInfo __instance, LevelData level)
        {
            if (awaitLevelInfo)
            {
                LevelStats stats = GameDataManager.GetLevelStats(level.levelID);
                if (stats == null)
                    return;

                // set the PB key here, since it's easiest after the time is already set
                if (stats.IsNewBest())
                    stats.SetCustom(STATS_KEY_PB, Verified);
            }

            if (!level.StatsHasCustom(currentKey))
                return;
            __instance._levelBestTimeDescription.spriteAsset = SpriteAsset;
            __instance._levelBestTimeDescription.text += $" <sprite={(level.StatsGetCustom<bool>(currentKey) ? 1 : 0)} tint>";
        }

        static void VerifyMSR(MenuScreenResults __instance)
        {
            if (LevelRush.IsLevelRush())
            {
                var nbT = __instance._levelCompleteStatsText;
                nbT.spriteAsset = SpriteAsset;
                nbT.text += $" <sprite={(prevVerified ? 1 : 0)} tint>";
                return;
            }

            LevelData level = NeonLite.Game.GetCurrentLevel();
            if (!level.StatsHasCustom(STATS_KEY))
                return;
            LevelStats stats = GameDataManager.levelStats[level.levelID];

            if (stats.IsNewBest())
            {
                var nb = __instance._levelCompleteNewBestText;
                nb.GetComponent<AxKLocalizedText>().Localize();
                var nbT = nb.GetComponent<TextMeshProUGUI>();
                nbT.spriteAsset = SpriteAsset;
                nbT.text = $"<sprite={(stats.GetCustom<bool>(STATS_KEY) ? 1 : 0)} tint> " + nbT.text;
            }
            else
            {
                // add it to the time itself
                var nbT = __instance._levelCompleteStatsText;
                nbT.spriteAsset = SpriteAsset;
                nbT.text += $" <sprite={(stats.GetCustom<bool>(STATS_KEY) ? 1 : 0)} tint>";
            }
        }

        static void VerifyMSLR(MenuScreenLevelRushComplete __instance)
        {
            if (__instance.bestTimeText.isActiveAndEnabled)
            {
                // add it to the new best test

                var nb = __instance.bestTimeText;
                nb.GetComponent<AxKLocalizedText>().Localize();
                var nbT = nb.GetComponent<TextMeshProUGUI>();
                nbT.spriteAsset = SpriteAsset;
                nbT.text = $"<sprite={(prevVerified ? 1 : 0)} tint> " + nbT.text;
            }
            else
            {
                __instance.timeText.spriteAsset = SpriteAsset;
                __instance.timeText.text = $"<sprite={(prevVerified ? 1 : 0)} tint> " + __instance.timeText.text;
            }
        }
    }
}
