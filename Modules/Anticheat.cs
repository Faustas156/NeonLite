using HarmonyLib;
using MelonLoader;
using NeonLite.Modules.UI;
using Steamworks;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

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

        static void Setup()
        {
            NeonLite.OnBundleLoad += bundle =>
            {
                prefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/AnticheatText.prefab");
                if (hasSetup)
                    Activate(assemblies.Count > 0);
            };

            force = Settings.Add(Settings.h, "Misc", "anticheatOn", "Force Anticheat", null, false, true);
            force.SetupForModule(MaybeActivate, (_, after) => after);
            active = force.Value || assemblies.Count > 0;
        }
        static void MaybeActivate(bool _) => Activate(assemblies.Count > 0);

        static readonly MethodInfo ogutms = AccessTools.Method(typeof(LevelStats), "UpdateTimeMicroseconds");

#if XBOX
        static readonly MethodInfo oglbui = AccessTools.Method(typeof(LeaderboardIntegrationBitcode), "LoadLeaderboardAndConditionallySubmitScore");
#else
        static readonly MethodInfo oglbui = AccessTools.Method(typeof(LeaderboardIntegrationSteam), "OnFindLeaderboardForUpload");
#endif

#if XBOX
        static readonly MethodInfo oggrsc = AccessTools.Method(typeof(GhostRecorder), "SaveCompressedAsync");
#else
        static readonly MethodInfo oggrsc = AccessTools.Method(typeof(GhostRecorder), "SaveCompressed");
#endif

        static readonly MethodInfo ogmsrosv = AccessTools.Method(typeof(MenuScreenResults), "OnSetVisible");
        static readonly MethodInfo oglaliset = AccessTools.Method(typeof(LeaderboardsAndLevelInfo), "SetLevel");

        static void Activate(bool activate)
        {
            activate |= force.Value;
            if (activate)
            {
                var hm = Helpers.HM(DontUpdateTime);
                hm.priority = Priority.First;
                Patching.AddPatch(ogutms, hm, Patching.PatchTarget.Prefix);
                Patching.AddPatch(oglbui, UploadScoreStopper, Patching.PatchTarget.Prefix);
                Patching.AddPatch(oggrsc, NoSaveCompressed, Patching.PatchTarget.Prefix);
                Patching.AddPatch(ogmsrosv, CorrectTimers, Patching.PatchTarget.Postfix);
                Patching.AddPatch(oglaliset, NeverNew, Patching.PatchTarget.Prefix);

                if (Patching.firstPass)
                    Patching.RunPatches(false);
            }
            else
            {
                Patching.RemovePatch(ogutms, DontUpdateTime);
                Patching.RemovePatch(oglbui, UploadScoreStopper);
                Patching.RemovePatch(oggrsc, NoSaveCompressed);
                Patching.RemovePatch(ogmsrosv, CorrectTimers);
                Patching.RemovePatch(oglaliset, NeverNew);

                if (textInstance)
                    Destroy(textInstance.gameObject);
            }

            if (activate && !textInstance && prefab)
                textInstance = Utils.InstantiateUI(prefab, "AnticheatText", NeonLite.mmHolder.transform).AddComponent<Anticheat>();

            hasSetup = true;
            active = activate;
        }

        static long lastTime;
        static bool DontUpdateTime(long newTime, ref bool ____newBest, long ____timeBestMicroseconds)
        {
            lastTime = newTime;
            ____newBest = newTime < ____timeBestMicroseconds;
            return false;
        }
#if XBOX
        static void UploadScoreStopper(ref bool uploadScore) => uploadScore = false;
        static bool NoSaveCompressed(Task __result) {
            __result = Task.CompletedTask;
            return false;
        }
#else
        static bool NoSaveCompressed() => false;
        static bool UploadScoreStopper(LeaderboardFindResult_t pCallback, bool bIOFailure, ref SteamLeaderboard_t ___currentLeaderboard)
        {
            if (pCallback.m_bLeaderboardFound != 1 || bIOFailure)
                return false;

            ___currentLeaderboard = pCallback.m_hSteamLeaderboard;
            LeaderboardScoreUploaded_t fakeUp = new()
            {
                m_bScoreChanged = 0
            };
            AccessTools.Method(typeof(LeaderboardIntegrationSteam), "OnLeaderboardScoreUploaded2").Invoke(null, [fakeUp, false]);
            return false;
        }
#endif
        static void CorrectTimers(MenuScreenResults __instance)
        {
            __instance._resultsScreenLevelTime.SetText(Helpers.FormatTime(lastTime / 1000, true, '.', true, true));
            __instance._levelCompleteStatsText.SetText(Helpers.FormatTime(lastTime / 1000, ShowMS.forceAll || ShowMS.extended.Value, '.', true, true));
        }
        static void NeverNew(ref bool isNewScore) => isNewScore = false;

        public static void Register(MelonAssembly assembly)
        {
            assemblies.Add(assembly);
            Activate(assemblies.Count > 0);
        }

        public static void Unregister(MelonAssembly assembly)
        {
            assemblies.Remove(assembly);
            Activate(assemblies.Count > 0);
        }

        TextMeshProUGUI text;
        Canvas c;

        void Start()
        {
            text = GetComponent<TextMeshProUGUI>();
            text.alpha = .7f;

            c = GetComponentInParent<Canvas>();
        }

        void Update() => transform.localPosition = c.ViewportToCanvasPosition(new Vector3(0f, 0f, 0));
    }
}
