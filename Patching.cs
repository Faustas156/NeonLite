using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MelonLoader;
using NeonLite.Modules.Optimization;

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
            ILManip,
            Count
        }

        class PatchInfo
        {
            public HarmonyMethod patch;
            public int target;
            public bool registered = false;

            public override bool Equals(object obj) => obj is PatchInfo info && patch?.method == info.patch?.method;

            // compiler would not shut up
            public override int GetHashCode() => -1372007919 + patch?.method.GetHashCode() ?? 0;

            public static bool operator ==(PatchInfo obj1, PatchInfo obj2) => obj1?.patch?.method == obj2?.patch?.method;
            public static bool operator !=(PatchInfo obj1, PatchInfo obj2) => obj1?.patch?.method != obj2?.patch?.method;
        }

        static readonly Dictionary<MethodInfo, LinkedList<PatchInfo>> patches = new(256); // this should be enough


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
        public static bool AddPatch(MethodInfo method, HarmonyMethod patch, PatchTarget target, bool instant = false)
        {
            if (method == null)
            {
                NeonLite.Logger.Error($"Tried to add patch {patch.method.Name} from module {patch.method.DeclaringType}, but target method is null!");
                return false;
            }

            if (!patches.ContainsKey(method))
                patches[method] = new();
            var patchlist = patches[method];

            if (patchlist.Any(x => x.patch.method == patch.method))
                return false;

            var info = new PatchInfo
            {
                patch = patch,
                target = (int)target,
            };

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

                try
                {
                    processor.Patch();
                }
                catch (Exception e)
                {
                    NeonLite.Logger.Warning($"Error performing patch for {method.Name} from {patch.method.Name} from module {patch.method.DeclaringType}:");
                    NeonLite.Logger.Error(e);
                }

                Helpers.EndProfiling();

                info.registered = true;

                patchRunner?.Join();
                patchlist.AddLast(info);
            }
            else
            {
                patchRunner?.Join();
                patchlist.AddFirst(info);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemovePatch(Type type, string name, Delegate patch) => RemovePatch(Helpers.Method(type, name), patch.Method);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemovePatch(MethodInfo method, Delegate patch) => RemovePatch(method, patch.Method);
        public static bool RemovePatch(MethodInfo method, MethodInfo patch)
        {
            if (!patches.ContainsKey(method))
                return false;
            var patchlist = patches[method];
            var p = patchlist.FirstOrDefault(x => x.patch.method == patch);
            if (p != default)
            {
                patchRunner?.Join();

                patchlist.Remove(p);
                if (p.registered)
                    NeonLite.Harmony.Unpatch(method, patch);

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RunPatches() => RunPatches(true);

        internal static Thread patchRunner;
        internal static bool firstPass = false;

        public static void RunPatches(bool parallel)
        {
            // if, somehow, something else is wanting to patch
            // tell it to wait please
            patchRunner?.Join();

            NeonLite.Logger.DebugMsg("RunPatches");

            GCManager.DisableGC(GCManager.GCType.Patching);
            Helpers.StartProfiling($"Setup Patches ({parallel})");

            List<PatchJob> bag = new(patches.Count(static x => x.Value.Any(static y => !y.registered)));

            if (bag.Capacity != 0)
            {
                foreach (var kv in patches.Where(static x => x.Value.Any(static y => !y.registered)))
                {

                    var curJob = new PatchJob()
                    {
                        method = kv.Key,
                        processor = NeonLite.Harmony.CreateProcessor(kv.Key),
                        patchInfos = [.. kv.Value.TakeWhile(static x => !x.registered)]
                    };

                    bag.Add(curJob);
                }

                patchRunner = new Thread(() =>
                {
                    NeonLite.Logger.DebugMsg("Starting parallel patching...");
                    var sw = new Stopwatch();
                    sw.Start();
                    Parallel.ForEach(bag, static x => x.Execute());

                    GCManager.EnableGC(GCManager.GCType.Patching);
                    NeonLite.Logger.Msg($"Ran {bag.Sum(x => x.total)} patches for {bag.Count} functions in parallel ({bag.Sum(x => x.errors)} errors, {sw.ElapsedMilliseconds}ms).");
                });

                patchRunner.Start();
                firstPass = true;

                if (!parallel)
                    patchRunner.Join();
            }
            Helpers.EndProfiling();
        }

        public static void PerformHarmonyPatches(Type type)
        {
            foreach (var method in type.GetMethods(AccessTools.allDeclared).Where(x => x.IsStatic && x.CustomAttributes.Any(x => x.AttributeType == typeof(HarmonyPatch))))
            {
                var prefix = method.GetCustomAttribute<HarmonyPrefix>() != null;
                var postfix = method.GetCustomAttribute<HarmonyPostfix>() != null;
                var transfix = method.GetCustomAttribute<HarmonyTranspiler>() != null;

                foreach (var patch in method.GetCustomAttributes<HarmonyPatch>())
                {
                    var pm = Helpers.Method(patch.info.declaringType, patch.info.methodName, patch.info.argumentTypes);
                    if (patch.info.methodType == MethodType.Enumerator)
                        pm = pm.MoveNext();

                    if (prefix)
                        AddPatch(pm, method.ToNewHarmonyMethod(), PatchTarget.Prefix);
                    if (postfix)
                        AddPatch(pm, method.ToNewHarmonyMethod(), PatchTarget.Postfix);
                    if (transfix)
                        AddPatch(pm, method.ToNewHarmonyMethod(), PatchTarget.Transpiler);
                }
            }
        }

        class PatchJob
        {
            internal static readonly object locker = new();
            public MethodInfo method;
            public PatchProcessor processor;
            public PatchInfo[] patchInfos;

            public int errors = 0;
            public int total = 0;

            public void Execute()
            {
                var current = new HarmonyMethod[(int)PatchTarget.Count];
                Array.Reverse(patchInfos);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                void Patch()
                {
                    try
                    {
                        processor.AddPrefix(current[(int)PatchTarget.Prefix]);
                        processor.AddPostfix(current[(int)PatchTarget.Postfix]);
                        processor.AddTranspiler(current[(int)PatchTarget.Transpiler]);
                        processor.AddFinalizer(current[(int)PatchTarget.Finalizer]);
                        processor.AddILManipulator(current[(int)PatchTarget.ILManip]);

                        processor.Patch();
                    }
                    catch (Exception e)
                    {
                        errors++;

                        lock (locker)
                        {
                            NeonLite.Logger.Warning($"Error performing patches for {method.DeclaringType.FullName}.{method.Name}:");
                            NeonLite.Logger.Error(e);
                            NeonLite.Logger.Warning($"Error came from one of:");

                            foreach (var p in current.Where(x => x != null))
                            {
                                NeonLite.Logger.Warning($"- {p.method.Name} from module {p.method.DeclaringType}");
                            }
                        }
                    }
                }

                foreach (var patch in patchInfos)
                {
#if DEBUG
                    if (NeonLite.DEBUG)
                    {
                        lock (locker)
                        {
                            try
                            {
                                NeonLite.Logger.Msg($"patch {method.DeclaringType.FullName}.{method.Name}:{patch.patch.method.Name} on #{Thread.CurrentThread.ManagedThreadId}");
                            }
                            catch { }
                        }
                    }
#endif

                    if (current[patch.target] != null)
                    {
                        Patch();
                        Array.Clear(current, 0, (int)PatchTarget.Count);
                    }

                    current[patch.target] = patch.patch;
                    patch.registered = true;
                    total++;
                }

                if (current.Any(static x => x != null))
                    Patch();
            }
        }

        public static CodeMatcher CloneInPlace(this CodeMatcher matcher, out CodeMatcher clone)
        {
            clone = matcher.Clone();
            return matcher;
        }

        public static CodeMatcher Do(this CodeMatcher matcher, Action f)
        {
            f();
            return matcher;
        }

        public static CodeMatcher Do(this CodeMatcher matcher, Action<CodeMatcher> f)
        {
            f(matcher);
            return matcher;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CodeMatcher Print(this CodeMatcher matcher, string prefix = "")
        {
#if DEBUG
            foreach (var c in matcher.Instructions())
                NeonLite.Logger.DebugMsg($"{prefix}{c}");
#endif
            return matcher;
        }
    }
}
