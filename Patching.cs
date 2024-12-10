using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

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

            public override bool Equals(object obj) => obj is PatchInfo info && patch == info.patch;

            public static bool operator ==(PatchInfo obj1, PatchInfo obj2) => obj1.patch == obj2.patch;
            public static bool operator !=(PatchInfo obj1, PatchInfo obj2) => obj1.patch != obj2.patch;
        }
        struct ProcessorPass(PatchProcessor p)
        {
            public PatchProcessor p = p;
        }

        static readonly Dictionary<MethodInfo, List<PatchInfo>> patches = [];

        public static bool AddPatch(MethodInfo method, Delegate patch, PatchTarget target) => AddPatch(method, Helpers.HM(patch), target);
        public static bool AddPatch(MethodInfo method, HarmonyMethod patch, PatchTarget target)
        {
            if (!patches.ContainsKey(method))
                patches[method] = [];
            var patchlist = patches[method];
            if (patchlist.FirstOrDefault(x => x.patch == patch).patch == patch)
                return false;
            patchlist.Add(new PatchInfo
            {
                patch = patch,
                target = target
            });

            return true;
        }

        public static bool RemovePatch(MethodInfo method, Delegate patch) => RemovePatch(method, Helpers.MI(patch));
        public static bool RemovePatch(MethodInfo method, MethodInfo patch)
        {
            if (!patches.ContainsKey(method))
                return false;
            var patchlist = patches[method];
            var p = patchlist.FirstOrDefault(x => x.patch.method == patch);
            if (p != default)
            {
                patchlist.Remove(p);
                return true;
            }

            return false;
        }

        internal static void RunPatches(bool parallel = true)
        {
            ConcurrentBag<PatchJob> bag = null;
            if (parallel)
                bag = [];
            foreach (var kv in patches)
            {
                if (kv.Value.All(x => x.registered))
                    continue;
                var curJob = new PatchJob()
                {
                    methodName = $"{kv.Key.DeclaringType.FullName}.{kv.Key.Name}",
                    processor = NeonLite.Harmony.CreateProcessor(kv.Key),
                    patchInfos = new(kv.Value.Where(x => !x.registered).ToArray(), Allocator.Persistent)
                };
                if (parallel)
                    bag.Add(curJob);
                else
                    curJob.Execute();

                for (int i = 0; i < kv.Value.Count; ++i)
                {
                    var patch = kv.Value[i];
                    patch.registered = true;
                    kv.Value[i] = patch;
                }
            }

            if (parallel)
                Parallel.ForEach(bag, x => x.Execute());
        }

        struct PatchJob
        {
            public string methodName;
            public PatchProcessor processor;
            public NativeArray<PatchInfo> patchInfos;

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
                        NeonLite.Logger.Msg($"{methodName}:{patch.patch.method.Name} on #{Thread.CurrentThread.ManagedThreadId}");

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

                patchInfos.Dispose();
            }
        }
    }
}
