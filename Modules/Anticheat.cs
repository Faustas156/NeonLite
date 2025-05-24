using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using static GhostUtils;

#if XBOX
using System.Threading.Tasks;
#else
using Steamworks;
#endif

namespace NeonLite.Modules
{
    public class Anticheat : MonoBehaviour, IModule
    {
#pragma warning disable CS0414
        const bool priority = false;
        static bool active = true;
        public static bool Active { get { return active; } }

        static bool hasSetup = false;
        static readonly HashSet<MelonAssembly> assemblies = [];

        static GameObject prefab;
        static Anticheat textInstance;

        static MelonPreferences_Entry<bool> force;
        static MelonPreferences_Entry<string> ghost;

        static void Setup()
        {
            NeonLite.OnBundleLoad += static bundle =>
            {
                prefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/AnticheatText.prefab");
                if (hasSetup)
                    Activate(assemblies.Count > 0);
            };

            force = Settings.Add(Settings.h, "Misc", "anticheatOn", "Force Anticheat", null, false, true);
            force.SetupForModule(MaybeActivate, static (_, after) => after);
            active = force.Value || assemblies.Count > 0;

            ghost = Settings.Add(Settings.h, "Misc", "overrideGhost", "Ghost Name", null, "", true);
            ghost.OnEntryValueChanged.Subscribe(static (_, after) => SetGhostName(NeonLite.i.MelonAssembly, after));
            SetGhostName(NeonLite.i.MelonAssembly, ghost.Value);
        }
        static void MaybeActivate(bool _) => Activate(assemblies.Count > 0);

#if XBOX
        static readonly MethodInfo oglbui = Helpers.Method(typeof(LeaderboardIntegrationBitcode), "LoadLeaderboardAndConditionallySubmitScore");
        static readonly MethodInfo oggrsc = Helpers.Method(typeof(GhostRecorder), "SaveCompressedAsync");
        static readonly MethodInfo ogsvgm = Helpers.Method(typeof(GameDataManager), "SaveGameAsync");
#else
        static readonly MethodInfo oglbui = Helpers.Method(typeof(LeaderboardIntegrationSteam), "OnFindLeaderboardForUpload");
        static readonly MethodInfo oggrsc = Helpers.Method(typeof(GhostRecorder), "SaveCompressed");
        static readonly MethodInfo ogsvgm = Helpers.Method(typeof(GameDataManager), "SaveGame");
#endif

        static readonly MethodInfo oggtp = Helpers.Method(typeof(GhostUtils), "GetPath", [typeof(string), typeof(GhostType), typeof(string).MakeByRefType()]);

        static void Activate(bool activate)
        {
            if (force != null)
                activate |= force.Value;

            Patching.TogglePatch(activate, typeof(MenuScreenTitle), "OnSetVisible", PreventNew, Patching.PatchTarget.Postfix);

            Patching.TogglePatch(activate, oglbui, UploadScoreStopper, Patching.PatchTarget.Prefix);
            //Patching.TogglePatch(activate, typeof(MenuScreenResults), "OnSetVisible", CorrectTimers, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(LeaderboardsAndLevelInfo), "SetLevel", NeverNew, Patching.PatchTarget.Prefix);

            Patching.TogglePatch(activate, typeof(LevelStats), "UpdateTimeMicroseconds", Helpers.HM(DontUpdateTime).SetPriority(Priority.First), Patching.PatchTarget.Prefix);

            Patching.TogglePatch(activate, oggrsc, NoSaveGhostCompressed, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, ogsvgm, NoSaveGame, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(GhostRecorder), "GetCompressedSavePath", GetCompressedSavePath, Patching.PatchTarget.Prefix);
#if XBOX
            Patching.TogglePatch(activate, Helpers.Method(typeof(GhostUtils), "LoadLevelTotalTimeCompressedAsync"), OverrideTimeAsync, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, Helpers.Method(typeof(GhostUtils), "LoadLevelDataCompressedAsync"), OverrideCompressedAsync, Patching.PatchTarget.Prefix);
#else
            Patching.TogglePatch(activate, Helpers.Method(typeof(GhostUtils), "LoadLevelTotalTimeCompressed"), AddPhantCheck, Patching.PatchTarget.Transpiler);
            Patching.TogglePatch(activate, Helpers.Method(typeof(GhostUtils), "LoadLevelDataCompressed"), AddPhantCheck, Patching.PatchTarget.Transpiler);
#endif
            Patching.TogglePatch(activate, oggtp, GetGhostPath, Patching.PatchTarget.Postfix);

            if (activate)
            {
                if (Patching.firstPass)
                    Patching.RunPatches(false);
                if (!textInstance && prefab && NeonLite.mmHolder)
                    textInstance = Utils.InstantiateUI(prefab, "AnticheatText", NeonLite.mmHolder.transform).AddComponent<Anticheat>();
            }
            else
            {
                if (textInstance)
                    Destroy(textInstance.gameObject);
                ClearTimes();
                GameDataManager.LoadGame(null);
            }

            hasSetup = true;

            active = activate;

            if (MainMenu.Instance()?._screenTitle)
                PreventNew(MainMenu.Instance()._screenTitle as MenuScreenTitle);
        }

        static void PreventNew(MenuScreenTitle __instance) => __instance.newGameButton.GetComponent<MenuButtonHolder>().ShouldBeInteractable = !active;

        static Dictionary<string, LevelStats> oldStats;
        internal static readonly HashSet<LevelStats> modified = [];

        static bool DontUpdateTime(LevelStats __instance, long newTime, ref long ____timeBestMicroseconds)
        {
            oldStats ??= GameDataManager.levelStats;

            if (!modified.Contains(__instance))
                ____timeBestMicroseconds = newTime + 1;

            modified.Add(__instance);
            return true;
        }

        public static void ClearTimes()
        {
            if (oldStats != null)
                GameDataManager.levelStats = oldStats;
            oldStats = null;
            modified.Clear();
        }

        static List<(MelonAssembly, string)> ghostNames = [];
        internal static bool fetchingGhost = false;


        static void GetGhostPath(GhostType ghostType, ref string filePath)
        {
            if (!fetchingGhost)
                return;            
            fetchingGhost = false;
            if (ghostType != GhostType.PersonalGhost || ghostNames.Count <= 0)
                return;
#if XBOX
            filePath += "_" + ghostNames.First().Item2.Replace(" ", "") + ".phant";
#else
            filePath = Path.Combine(filePath, ghostNames.First().Item2 + ".phant");
#endif
        }

        static bool GetCompressedSavePath(ref string __result)
        {
            if (ghostNames.Count <= 0)
                return true;
            fetchingGhost = true;
            GhostUtils.GetPath(GhostType.PersonalGhost, ref __result);
            return false;
        }

#if XBOX
        static void UploadScoreStopper(ref bool uploadScore) => uploadScore = false;
        static bool NoSaveGhostCompressed(Task __result) {
            if (ghostNames.Count > 0)
                return true;

            __result = Task.CompletedTask;
            return false;
        }

        static bool NoSaveGame(Task __result) {
            __result = Task.CompletedTask;
            return false;
        }

        static bool OverrideCompressedAsync(GhostUtils.GhostType ghostType, ref Task<GhostSave> __result) {
            if (ghostType != GhostType.PersonalGhost || ghostNames.Count <= 0)
                return true;

            static async Task<GhostSave> Override()
            {
                GhostSave ghostSave = new();
                fetchingGhost = true;
                string path = "";
                if (GhostUtils.GetPath(GhostType.PersonalGhost, ref path))
                {
                    string text = await (Task<string>)Helpers.Method(typeof(GhostUtils), "TryLoadGhost").Invoke(null, [path]);
                    if (text != null)
                    {
                        object[] args = [ghostSave, text, null];
                        Helpers.Method(typeof(GhostUtils), "DeserializeLevelDataCompressed").Invoke(null, args);
                        ghostSave = (GhostSave)args[0];
                    }
                }
                return ghostSave;
            }

            __result = Override();
            return false;
        }

        static bool OverrideTimeAsync(GhostUtils.GhostType ghostType, ref Task<float> __result)
        {
            if (ghostType != GhostType.PersonalGhost || ghostNames.Count <= 0)
                return true;

            static async Task<float> Override()
            {
                float totalTime = float.MaxValue;
                fetchingGhost = true;
                string path = "";
                if (GhostUtils.GetPath(GhostType.PersonalGhost, ref path))
                {
                    if (await TFBGames.FileManagement.FileExistsAsync(path, true))
                        totalTime = GhostUtils.ParseLevelTotalTimeCompressed(await TFBGames.FileManagement.ReadAllGhostTextAsync(path));
                }
                return totalTime;
            }

            __result = Override();
            return false;
        }
#else
        static IEnumerable<CodeInstruction> AddPhantCheck(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            string ldlocN = "";

            return new CodeMatcher(instructions, generator)
                .MatchForward(false, new CodeMatch(OpCodes.Call, Helpers.Method(typeof(File), "Exists"))) // go to file exists
                .MatchBack(false, new CodeMatch(static x => x.IsLdloc())) // go backwards to the stloc
                .CloneInPlace(out var ldloc)
                .Do(() => ldlocN = ldloc.Opcode.Name.Last().ToString())
                .MatchBack(true, new CodeMatch(x => x.IsStloc() && x.opcode.Name.EndsWith(ldlocN))) // go back to the **matching** stloc
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldnull)) // add an extra null
                .Insert(new CodeInstruction(OpCodes.Pop)) // pop the og/null off the stack
                .CreateLabel(out var after) // create a label at the pop
                .MatchBack(true, new CodeMatch(x => x.IsLdloc() && x.opcode.Name.EndsWith(ldlocN))) // go back to a **matching** ldloc
                .Advance(1)
                .Insert(
                    new CodeInstruction(OpCodes.Dup), // duplicate the path
                    Transpilers.EmitDelegate<Func<string, bool>>(static x => x.EndsWith(".phant")), // check if we end with phant
                    new CodeInstruction(OpCodes.Brtrue, after)) // if we do, branch forward
                .MatchBack(true, new CodeMatch(x => x.IsStloc() && x.opcode.Name.EndsWith(ldlocN))) // go back to the *other* matching stloc
                .Advance(1)
                .Insert(
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    CodeInstruction.StoreField(typeof(Anticheat), "fetchingGhost")
                )
                .InstructionEnumeration();
        }

        static bool NoSaveGame() => false;
        static bool NoSaveGhostCompressed()
        {
            if (ghostNames.Count > 0)
                return true;
            return false;
        }
        static bool UploadScoreStopper(LeaderboardFindResult_t pCallback, bool bIOFailure, ref SteamLeaderboard_t ___currentLeaderboard)
        {
            if (pCallback.m_bLeaderboardFound != 1 || bIOFailure)
                return false;

            ___currentLeaderboard = pCallback.m_hSteamLeaderboard;
            LeaderboardScoreUploaded_t fakeUp = new()
            {
                m_bScoreChanged = 0
            };
            Helpers.Method(typeof(LeaderboardIntegrationSteam), "OnLeaderboardScoreUploaded2").Invoke(null, [fakeUp, false]);
            return false;
        }
#endif

        static void NeverNew(ref bool isNewScore) => isNewScore = false;

        public static void Register(MelonAssembly assembly)
        {
            assemblies.Add(assembly);
            Activate(assemblies.Count > 0);
        }

        public static void Register(MelonAssembly assembly, string ghostName)
        {
            SetGhostName(assembly, ghostName);
            Register(assembly);
        }

        public static void SetGhostName(MelonAssembly assembly, string ghostName)
        {
            int i = 0;
            foreach ((var a, var n) in ghostNames)
            {
                if (a == assembly)
                {
                    if (string.IsNullOrEmpty(ghostName))
                        ghostNames.RemoveAt(i);
                    else
                        ghostNames[i] = (a, ghostName);
                    return;
                }
                ++i;
            }

            if (!string.IsNullOrEmpty(ghostName))
                ghostNames.Add((assembly, ghostName));
        }

        public static void Unregister(MelonAssembly assembly)
        {
            SetGhostName(assembly, null);
            assemblies.Remove(assembly);
        }

        static void OnLevelLoad(LevelData _) => Activate(assemblies.Count > 0);

        TextMeshProUGUI text;
        Canvas c;
        void Awake()
        {
            text = GetComponent<TextMeshProUGUI>();
            text.alpha = .7f;

            c = GetComponentInParent<Canvas>();
            Update();
        }

        void Update() => transform.localPosition = c.ViewportToCanvasPosition(new Vector3(0f, 0f, 0));
    }
}
