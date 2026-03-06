#if !XBOX
#pragma warning disable IDE0130

using System.Reflection;
using System.Text;
using Steamworks;
using UnityEngine;

namespace NeonLite.Modules
{
    [Module]
    public static class SteamLBFiles
    {
        const bool priority = false;
        const bool active = true;

        public enum LBType
        {
            Level,
            Rush,
            Global
        }

        public delegate string LBWriteFunc(BinaryWriter writer, LBType type, bool sameScore);
        public delegate void LBLoadFunc(BinaryReader reader, int length, LeaderboardScore score);

        public static event LBWriteFunc OnLBWrite;
        static readonly Dictionary<string, List<LBLoadFunc>> onScoreLoad = [];

        static CallResult<RemoteStorageFileShareResult_t> cb_onShare;

        static CallResult<LeaderboardUGCSet_t> cb_onUGCSet;
        static bool skip;
        static LeaderboardScoreUploaded_t uploadT;

        static readonly MethodInfo[] toPatchC = [
            Helpers.Method(typeof(LeaderboardIntegrationSteam), "OnLeaderboardScoreDownloadGlobalResult2"),
            Helpers.Method(typeof(LeaderboardIntegrationSteam), "DownloadEntries"),
        ];

        static void Setup()
        {
            // setup callbacks needed
            cb_onUGCSet = CallResult<LeaderboardUGCSet_t>.Create(OnUGCSetCB);
            cb_onShare = CallResult<RemoteStorageFileShareResult_t>.Create(OnShareCB);
        }

        static void Activate(bool _)
        {
            Patching.AddPatch(typeof(LeaderboardIntegrationSteam), "OnLeaderboardScoreUploaded2", OnLBUploaded, Patching.PatchTarget.Prefix);

            foreach (var p in toPatchC)
            {
                Patching.AddPatch(p, StartCollecting, Patching.PatchTarget.Prefix);
                Patching.AddPatch(p, StopCollecting, Patching.PatchTarget.Postfix);
            }

            Patching.AddPatch(typeof(SteamUserStats), "GetDownloadedLeaderboardEntry", CollectUGCDown, Patching.PatchTarget.Postfix);
            Patching.AddPatch(typeof(Leaderboards), "DisplayScores_AsyncRecieve", DownloadUGC, Patching.PatchTarget.Postfix);

            cacheDir = Path.Combine(Helpers.GetSaveDirectory(), "NeonLite", "UGCCACHE");
            new Thread(ClearCache).Start();
        }

        public static void RegisterForLoad(string file, LBLoadFunc func)
        {
            if (!onScoreLoad.TryGetValue(file, out var funcs))
                onScoreLoad.Add(file, [func]);
            else
                onScoreLoad[file].Add(func);
        }

        public static void DeregisterForLoad(string file, LBLoadFunc func)
        {
            if (!onScoreLoad.TryGetValue(file, out var funcs))
                return;
            onScoreLoad[file].Remove(func);
        }

        const ulong k_UGCHandleInvalid = 0xfffffffffffffffful;
        const int UGC_LIMIT = 1;

        static BinaryWriter selfCache;
        const string TMP_FILE = "nllastugc.bin";

        static bool OnLBUploaded(LeaderboardScoreUploaded_t pCallback, bool bIOFailure, Leaderboards ___leaderboardsRef, bool ___globalNeonRankingsRequest)
        {
            if (!skip)
            {
                NeonLite.Logger.BetaMsg($"OnLBUploaded called: Success status? {(int)pCallback.m_bSuccess} bIOFailure? {bIOFailure} Score changed? {(int)pCallback.m_bScoreChanged}");
                NeonLite.Logger.BetaMsg($"Are we global? {___globalNeonRankingsRequest} Are we rushing? {LevelRush.IsLevelRush()}");
            }

            const bool DEBUG = false;

            if (bIOFailure || (pCallback.m_bSuccess == 0 && !DEBUG) || skip)
                return true;

            var list = OnLBWrite?.GetInvocationList();

            if (list == null)
                return true;

            var rushtype = LevelRush.IsLevelRush() ?
                LevelRush.GetCurrentLevelRush().levelRushType.ToString() + "_" +
                    (LevelRush.IsHellRush() ? "Hell" : "Heaven") : null;

            var level = (LevelData)Helpers.Field(typeof(Leaderboards), "currentLevelData").GetValue(___leaderboardsRef);
#if DEBUG
            string filepath = ___globalNeonRankingsRequest ? Path.Combine("NeonLite", "globallbugc.bin")
                                : (rushtype != null ? Path.Combine("NeonLite", "Rush", rushtype, "lbugc.bin") :
                                    Path.Combine("NeonLite", "Levels", level.levelID, "lbugc.bin"));
#else
            string filepath = TMP_FILE;
#endif

            LBType type = ___globalNeonRankingsRequest ? LBType.Global : (rushtype != null ? LBType.Rush : LBType.Level);

            NeonLite.Logger.BetaMsg($"Outputting to {filepath}. Type? {type}");

            var fc = SteamRemoteStorage.GetFileCount();

            NeonLite.Logger.BetaMsg($"Current file count: {fc}/{UGC_LIMIT}");

            while (fc >= UGC_LIMIT)
            {
                // uhoh
                var fn = SteamRemoteStorage.GetFileNameAndSize(0, out var size);
                NeonLite.Logger.BetaMsg($"File count over UGC limit. Name to remove: {fn}");
                SteamRemoteStorage.FileDelete(fn);
                fc = SteamRemoteStorage.GetFileCount();

                NeonLite.Logger.BetaMsg($"Current file count: {fc}/{UGC_LIMIT}");
            }


            var handle = SteamRemoteStorage.FileWriteStreamOpen(filepath);

            if (handle.m_UGCFileWriteStreamHandle == k_UGCHandleInvalid)
            {
                NeonLite.Logger.Error("Error opening file stream for Steam UGC.");
                return true;
            }

            int count = 0;

            selfCache = new(File.OpenWrite(Path.Combine(cacheDir, "self.read")));

            foreach (var dg in list.Cast<LBWriteFunc>())
            {
                using MemoryStream final = new();
                using MemoryStream file = new();
                string filename = null;

                try
                {
                    using BinaryWriter writer = new(file, Encoding.UTF8, true);
                    filename = dg.Invoke(writer, type, pCallback.m_bScoreChanged == 0);

                    if (filename == null)
                        continue;
                }
                catch (Exception e)
                {
                    NeonLite.Logger.Warning("Error writing to LB file (will continue to next function):");
                    NeonLite.Logger.Error(e);
                    continue;
                }

                if (filename.Length > 0xFF)
                {
                    NeonLite.Logger.Error($"Steam UGC filename {filename} too long.");
                    continue;
                }

                int size = (int)(filename.Length + 1 + sizeof(int) + file.Length);
                if (size > 100 * 1024 * 1024)
                {
                    NeonLite.Logger.Error($"Steam UGC file {filename} is over 100MiB (+ {filename.Length + 1 + sizeof(int)} header.)");
                    continue;
                }

                {
                    using BinaryWriter finalWrite = new(final, Encoding.UTF8, true);
                    finalWrite.Write((byte)filename.Length);
                    finalWrite.Write(Encoding.UTF8.GetBytes(filename));
                    finalWrite.Write((int)file.Length);

                    final.Position = final.Length;
                    file.Seek(0, SeekOrigin.Begin);
                    file.CopyTo(final);

                }

                selfCache.Write(final.GetBuffer(), 0, size);
                SteamRemoteStorage.FileWriteStreamWriteChunk(handle, final.GetBuffer(), size);
                NeonLite.Logger.BetaMsg($"Wrote file {filename}, size {file.Length} to UGC.");

                count++;
            }

            selfCache.Close();

            if (count == 0)
            {
                SteamRemoteStorage.FileWriteStreamCancel(handle);
                return true;
            }

            SteamRemoteStorage.FileWriteStreamClose(handle);

            if (!DEBUG)
            {
                var apicall = SteamRemoteStorage.FileShare(filepath);
                cb_onShare.Set(apicall);
                uploadT = pCallback;
            }

            return DEBUG;
        }

        static UGCHandle_t selfHandle;

        static void OnShareCB(RemoteStorageFileShareResult_t pCallback, bool bIOfailure)
        {
            if (pCallback.m_eResult != EResult.k_EResultOK || bIOfailure)
            {
                NeonLite.Logger.Error($"Failed to share the UGC for the Steam LB. Error: {pCallback.m_eResult}");

                skip = true;
                Helpers.Method(typeof(LeaderboardIntegrationSteam), "OnLeaderboardScoreUploaded2").Invoke(null, [uploadT, false]);
                skip = false;
                return;
            }

            selfHandle = pCallback.m_hFile;
            var apicall = SteamUserStats.AttachLeaderboardUGC(uploadT.m_hSteamLeaderboard, pCallback.m_hFile);
            cb_onUGCSet.Set(apicall);
        }

        static void OnUGCSetCB(LeaderboardUGCSet_t pCallback, bool bIOfailure)
        {
            if (pCallback.m_eResult != EResult.k_EResultOK || bIOfailure)
                NeonLite.Logger.Error("Failed to set the UGC for the Steam LB.");
            else
            {
                if (selfCache != null)
                {
                    File.Move(Path.Combine(cacheDir, "self.read"), Path.Combine(cacheDir, $"{selfHandle}.bin"));
                    selfCache.Dispose();
                    selfCache = null;
                }
            }

            skip = true;
            Helpers.Method(typeof(LeaderboardIntegrationSteam), "OnLeaderboardScoreUploaded2").Invoke(null, [uploadT, false]);
            skip = false;
        }

        class UGCDown(ref LeaderboardEntry_t entry)
        {
            public LeaderboardScore score = null;
            public CSteamID steamID = entry.m_steamIDUser;
            public UGCHandle_t ugcH = entry.m_hUGC;
            public CallResult<RemoteStorageDownloadUGCResult_t> cb =
                            (ulong)entry.m_hUGC == k_UGCHandleInvalid ? null : CallResult<RemoteStorageDownloadUGCResult_t>.Create(OnUGCDownCB);

            public BinaryReader cache;
        }
        static readonly List<UGCDown> ugcDowns = new(10);
        static bool collecting = false;

        static string cacheDir;

        static void ClearCache()
        {
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
                return;
            }

            try
            {
                new DirectoryInfo(cacheDir).Attributes |= FileAttributes.Hidden;
            }
            catch { } // whatever

            var current = DateTime.UtcNow;

            // iterate over all .read files that haven't been touched in the last 7 days
            var iter = Directory.EnumerateFiles(cacheDir, "*.read")
                .Select(f => (f, File.GetLastWriteTimeUtc(f)))
                .OrderBy(t => t.Item2).TakeWhile(t => t.Item2 < current.AddDays(-7));

            foreach ((var f, var t) in iter)
            {
                File.Delete(f);
                string real = Path.Combine(cacheDir, Path.GetFileNameWithoutExtension(f)) + ".bin";
                if (File.Exists(real))
                    File.Delete(real);
            }
        }

        static void StartCollecting()
        {
            collecting = true;
            ugcDowns.Clear();
        }
        static void StopCollecting() => collecting = false;

        static void CollectUGCDown(ref LeaderboardEntry_t pLeaderboardEntry) => ugcDowns.Add(new(ref pLeaderboardEntry));

        static void DownloadUGC(bool atleastOneEntry, List<GameObject> ___createdScores)
        {
            if (!atleastOneEntry)
                return;

            foreach (var (lbc, ugcd) in ___createdScores.Select(x => x.GetComponent<LeaderboardScore>()).Zip(ugcDowns, static (x, y) => (x, y)))
            {
                ugcd.score = lbc;
                if (ugcd.cb != null)
                {
                    NeonLite.Logger.DebugMsg($"UGCHANDLE {ugcd.ugcH}");
                    var cache = Path.Combine(cacheDir, ugcd.ugcH.ToString());
                    if (File.Exists(cache + ".bin"))
                    {
                        // ok we're gonna setup cache instead
                        using BinaryReader reader = new(File.OpenRead(cache + ".bin"));

                        RemoteStorageDownloadUGCResult_t fake = new()
                        {
                            m_eResult = EResult.k_EResultOK,
                            m_ulSteamIDOwner = (ulong)ugcd.steamID,
                            m_nSizeInBytes = (int)reader.BaseStream.Length
                        };

                        ugcd.cache = reader;
                        OnUGCDownCB(fake, false);
                    }
                    else
                    {
                        var apicall = SteamRemoteStorage.UGCDownload(ugcd.ugcH, 0);
                        ugcd.cb.Set(apicall);
                    }
                }
            }
        }

        static byte[] ugcBuffer = new byte[1024];
        static MemoryStream ugcStream = new(ugcBuffer);
        static BinaryReader ugcReader = new(ugcStream);

        static void OnUGCDownCB(RemoteStorageDownloadUGCResult_t pCallback, bool bIOfailure)
        {
            if (pCallback.m_eResult != EResult.k_EResultOK || bIOfailure)
                NeonLite.Logger.Error("Failed to download UGC for Steam LB.");

            var ugcdown = ugcDowns.FirstOrDefault(x => x.steamID == (CSteamID)pCallback.m_ulSteamIDOwner);
            if (ugcdown == default(UGCDown) || !ugcdown.score)
                return;

            uint offset = 0;

            var cache = Path.Combine(cacheDir, ugcdown.ugcH.ToString());

            BinaryWriter cacheWrite = null;
            if (ugcdown.cache == null)
                cacheWrite = new(File.OpenWrite(cache + ".bin"));

            // write the "last read date"
            {
                using BinaryWriter writer = new(File.OpenWrite(cache + ".read"));
                writer.Write(DateTime.UtcNow.ToBinary());
            }

            void ReadIntoStream(int len)
            {
                if (ugcBuffer.Length < len)
                {
                    ugcBuffer = new byte[len];
                    ugcStream = new(ugcBuffer);
                    ugcReader = new(ugcStream);
                }

                if (ugcdown.cache != null)
                {
                    ugcStream.Seek(0, SeekOrigin.Begin);
                    ugcdown.cache.BaseStream.Seek(offset, SeekOrigin.Begin);
                    ugcdown.cache.BaseStream.CopyTo(ugcStream, len);
                }
                else
                {
                    SteamRemoteStorage.UGCRead(pCallback.m_hFile, ugcBuffer, len, offset, EUGCReadAction.k_EUGCRead_ContinueReadingUntilFinished);
                    cacheWrite.Write(ugcBuffer, 0, len);
                }

                offset += (uint)len;
                ugcStream.Seek(0, SeekOrigin.Begin);
            }

            try
            {
                while (offset < pCallback.m_nSizeInBytes)
                {
                    ReadIntoStream(1);
                    var fnLen = ugcReader.ReadByte();
                    ReadIntoStream(fnLen + sizeof(int));

                    var filename = Encoding.UTF8.GetString(ugcReader.ReadBytes(fnLen));
                    var length = ugcReader.ReadInt32();

                    ReadIntoStream(length);

                    if (onScoreLoad.TryGetValue(filename, out var funcs))
                    {
                        foreach (var f in funcs)
                        {
                            ugcStream.Seek(0, SeekOrigin.Begin);

                            f.Invoke(ugcReader, length, ugcdown.score);
                        }
                    }
                }

                cacheWrite?.Close();
            }
            catch (Exception e)
            {
                // awwww Shit
                NeonLite.Logger.Warning("Error while reading Steam LB UGC:");
                NeonLite.Logger.Error(e);

                if (cacheWrite != null)
                {
                    SteamRemoteStorage.UGCRead(pCallback.m_hFile, ugcBuffer, 0, 0, EUGCReadAction.k_EUGCRead_Close);

                    cacheWrite.Close();
                    File.Delete(cache + ".read");
                    File.Delete(cache + ".bin");
                }
            }

            // ugcdown.free = true;
        }
    }
}

#endif
