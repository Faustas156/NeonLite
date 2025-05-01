using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NeonLite
{
    public static class Patching
    {
        public enum PatchTarget
        {
            Prefix,
            Postfix,
            Transpiler,
            Finalizer,
            ILManip
        }
        struct PatchInfo
        {
            public HarmonyMethod patch;
            public PatchTarget target;
            public bool registered;

            public override readonly bool Equals(object obj) => obj is PatchInfo info && patch?.method == info.patch?.method;

            // compiler would not shut up
            public override readonly int GetHashCode() => -1372007919 + patch?.method.GetHashCode() ?? 0;

            public static bool operator ==(PatchInfo obj1, PatchInfo obj2) => obj1.patch?.method == obj2.patch?.method;
            public static bool operator !=(PatchInfo obj1, PatchInfo obj2) => obj1.patch?.method != obj2.patch?.method;
        }

        static readonly Dictionary<MethodInfo, List<PatchInfo>> patches = [];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TogglePatch(bool active, Type type, string name, Delegate patch, PatchTarget target, bool instant = false) => active ? AddPatch(Helpers.Method(type, name), Helpers.HM(patch), target, instant) : RemovePatch(Helpers.Method(type, name), patch.Method);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TogglePatch(bool active, Type type, string name, HarmonyMethod patch, PatchTarget target, bool instant = false) => active ? AddPatch(Helpers.Method(type, name), patch, target, instant) : RemovePatch(Helpers.Method(type, name), patch.method);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TogglePatch(bool active, MethodInfo method, Delegate patch, PatchTarget target, bool instant = false) => active ? AddPatch(method, Helpers.HM(patch), target, instant) : RemovePatch(method, patch.Method);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TogglePatch(bool active, MethodInfo method, HarmonyMethod patch, PatchTarget target, bool instant = false) => active ? AddPatch(method, patch, target, instant) : RemovePatch(method, patch.method);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddPatch(Type type, string name, Delegate patch, PatchTarget target, bool instant = false) => AddPatch(Helpers.Method(type, name), Helpers.HM(patch), target, instant);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddPatch(Type type, string name, HarmonyMethod patch, PatchTarget target, bool instant = false) => AddPatch(Helpers.Method(type, name), patch, target, instant);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddPatch(MethodInfo method, Delegate patch, PatchTarget target, bool instant = false) => AddPatch(method, Helpers.HM(patch), target, instant);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddPatch(MethodInfo method, HarmonyMethod patch, PatchTarget target, bool instant = false)
        {
            if (!patches.ContainsKey(method))
                patches[method] = [];
            var patchlist = patches[method];
            var info = new PatchInfo
            {
                patch = patch,
                target = target,
                registered = instant
            };
            if (patchlist.FirstOrDefault(x => x == info) != default)
                return false;
            patchlist.Add(info);

            if (instant)
            {
                Helpers.StartProfiling($"Instant-patch {patch.methodName}");
                var processor = NeonLite.Harmony.CreateProcessor(method);
                switch (target)
                {
                    case PatchTarget.Prefix:
                        processor.AddPrefix(patch);
                        break;
                    case PatchTarget.Postfix:
                        processor.AddPostfix(patch);
                        break;
                    case PatchTarget.Transpiler:
                        processor.AddTranspiler(patch);
                        break;
                    case PatchTarget.Finalizer:
                        processor.AddFinalizer(patch);
                        break;
                    case PatchTarget.ILManip:
                        processor.AddILManipulator(patch);
                        break;
                }

                processor.Patch();
                Helpers.EndProfiling();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemovePatch(Type type, string name, Delegate patch) => RemovePatch(Helpers.Method(type, name), patch.Method);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemovePatch(MethodInfo method, Delegate patch) => RemovePatch(method, patch.Method);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemovePatch(MethodInfo method, MethodInfo patch)
        {
            if (!patches.ContainsKey(method))
                return false;
            var patchlist = patches[method];
            var p = patchlist.FirstOrDefault(x => x.patch.method == patch);
            if (p != default)
            {
                patchlist.Remove(p);
                if (p.registered)
                    NeonLite.Harmony.Unpatch(method, patch);

                return true;
            }

            return false;
        }

        public static void RunPatches() => RunPatches(false);

        internal static Thread patchRunner;
        internal static bool firstPass = false;
        internal static void RunPatches(bool parallel)
        {
            Helpers.StartProfiling($"Run Patches ({parallel})");

            ConcurrentBag<PatchJob> bag = null;
            if (parallel)
                bag = [];
            foreach (var kv in patches)
            {
                if (kv.Value.All(x => x.registered))
                    continue;

                if (!parallel)
                    Helpers.StartProfiling($"{kv.Key.DeclaringType.FullName}.{kv.Key.Name}");

                var curJob = new PatchJob()
                {
                    methodName = NeonLite.DEBUG ? $"{kv.Key.DeclaringType.FullName}.{kv.Key.Name}" : "",
                    processor = NeonLite.Harmony.CreateProcessor(kv.Key),
                    patchInfos = kv.Value.Where(x => !x.registered).ToArray()
                };
                if (parallel)
                    bag.Add(curJob);
                else
                {
                    Helpers.StartProfiling($"Instant-patch all");
                    curJob.Execute();
                    Helpers.EndProfiling();
                }

                for (int i = 0; i < kv.Value.Count; ++i)
                {
                    var patch = kv.Value[i];
                    patch.registered = true;
                    kv.Value[i] = patch;
                }
                if (!parallel)
                    Helpers.EndProfiling();
            }

            if (parallel)
            {
                patchRunner = new Thread(() => Parallel.ForEach(bag, x => x.Execute()));
                patchRunner.Start();
            }
            firstPass = true;
            Helpers.EndProfiling();
        }

        struct PatchJob
        {
            internal static readonly object locker = new();
            public string methodName;
            public PatchProcessor processor;
            public PatchInfo[] patchInfos;

            public void Execute()
            {
                var current = new Dictionary<PatchTarget, bool>() {
                    { PatchTarget.Prefix, false },
                    { PatchTarget.Postfix, false },
                    { PatchTarget.Transpiler, false },
                    { PatchTarget.Finalizer, false },
                    { PatchTarget.ILManip, false }
                };

                HarmonyMethod hmNull = null;

                foreach (var patch in patchInfos)
                {
                    if (NeonLite.DEBUG)
                    {
                        lock (locker)
                            NeonLite.Logger.Msg($"{methodName}:{patch.patch.method.Name} on #{Thread.CurrentThread.ManagedThreadId}");
                    }

                    if (current[patch.target])
                    {
                        processor.Patch();
                        processor.AddPrefix(hmNull);
                        processor.AddPostfix(hmNull);
                        processor.AddTranspiler(hmNull);
                        processor.AddFinalizer(hmNull);
                        processor.AddILManipulator(hmNull);
                    }

                    switch (patch.target)
                    {
                        case PatchTarget.Prefix:
                            processor.AddPrefix(patch.patch);
                            break;
                        case PatchTarget.Postfix:
                            processor.AddPostfix(patch.patch);
                            break;
                        case PatchTarget.Transpiler:
                            processor.AddTranspiler(patch.patch);
                            break;
                        case PatchTarget.Finalizer:
                            processor.AddFinalizer(patch.patch);
                            break;
                        case PatchTarget.ILManip:
                            processor.AddILManipulator(patch.patch);
                            break;
                    }
                    current[patch.target] = true;
                }

                if (current.Any(kv => kv.Value))
                    processor.Patch();
            }
        }
    }
}
