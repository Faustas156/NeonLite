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

#if XBOX
        static readonly MethodInfo oglbui = Helpers.Method(typeof(LeaderboardIntegrationBitcode), "LoadLeaderboardAndConditionallySubmitScore");
#else
        static readonly MethodInfo oglbui = Helpers.Method(typeof(LeaderboardIntegrationSteam), "OnFindLeaderboardForUpload");
#endif

#if XBOX
        static readonly MethodInfo oggrsc = Helpers.Method(typeof(GhostRecorder), "SaveCompressedAsync");
#else
        static readonly MethodInfo oggrsc = Helpers.Method(typeof(GhostRecorder), "SaveCompressed");
#endif

        static void Activate(bool activate)
        {
            if (force != null)
                activate |= force.Value;

            var hm = Helpers.HM(DontUpdateTime);
            hm.priority = Priority.First;
            Patching.TogglePatch(activate, typeof(LevelStats), "UpdateTimeMicroseconds", hm, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, oglbui, UploadScoreStopper, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, oggrsc, NoSaveCompressed, Patching.PatchTarget.Prefix);
            Patching.TogglePatch(activate, typeof(MenuScreenResults), "OnSetVisible", CorrectTimers, Patching.PatchTarget.Postfix);
            Patching.TogglePatch(activate, typeof(LeaderboardsAndLevelInfo), "SetLevel", NeverNew, Patching.PatchTarget.Prefix);

            if (activate)
            {
                if (Patching.firstPass)
                    Patching.RunPatches(false);
                if (!textInstance && prefab)
                    textInstance = Utils.InstantiateUI(prefab, "AnticheatText", NeonLite.mmHolder.transform).AddComponent<Anticheat>();
            }
            else if (textInstance)
                Destroy(textInstance.gameObject);

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
            Helpers.Method(typeof(LeaderboardIntegrationSteam), "OnLeaderboardScoreUploaded2").Invoke(null, [fakeUp, false]);
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
            if (hasSetup)
                Activate(assemblies.Count > 0);
            else
                active = assemblies.Count > 0;
        }

        public static void Unregister(MelonAssembly assembly)
        {
            assemblies.Remove(assembly);
            if (hasSetup)
                Activate(assemblies.Count > 0);
            else
                active = assemblies.Count > 0;
        }

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
