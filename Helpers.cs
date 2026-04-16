// #define ENABLE_PROFILER

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using MelonLoader;
using NeonLite.Modules;
using NeonLite.Modules.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Networking;

namespace NeonLite
{
    static public class Helpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public MethodInfo MI(Delegate func) => func.Method;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public HarmonyMethod HM(Delegate func) => new(func.Method);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use SetPriority instead.")]
        static public HarmonyMethod Set(this HarmonyMethod hm, int? priority = null)
        {
            if (priority.HasValue)
                hm.priority = priority.Value;
            return hm;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public HarmonyMethod SetPriority(this HarmonyMethod hm, int priority)
        {
            hm.priority = priority;
            return hm;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal bool HasModulesInAssembly(Assembly assembly)
        {
            var types = assembly.GetTypes();

            // get the newtype module attrib
            var attrib = types.Where(static x => x.GetCustomAttribute<ModuleAttribute>() != null)
                .OrderByDescending(static x => x.GetCustomAttribute<ModuleAttribute>().priority);

            // get the old interface style
            var inter = types.Where(static t => typeof(IModule).IsAssignableFrom(t) && t != typeof(IModule));

            return attrib.Union(inter).Any();
        }
        static internal List<Type> GetModulesInAssembly(Assembly assembly, bool onlyNew)
        {
            if (!HasModulesInAssembly(assembly))
                return [];

            var types = assembly.GetTypes();

            // get the newtype module attrib
            var attrib = types.Where(static x => x.GetCustomAttribute<ModuleAttribute>() != null)
                .OrderByDescending(static x => x.GetCustomAttribute<ModuleAttribute>().priority);

            // get the old interface style
            var inter = types.Where(static t => typeof(IModule).IsAssignableFrom(t) && t != typeof(IModule));

            var final = attrib.Union(inter);
            if (onlyNew)
                return [.. final.Where(x => !NeonLite.modules.Contains(x))];

            return [.. final];
        }


#if DEBUG
        static internal bool GetModulePrio(Type module)
        {
            bool r = (bool)Field(module, "priority").GetValue(null);
            bool normal = r;
            // if (module.FullName.Contains("Optimization"))
            //     r = false;
            NeonLite.Logger.DebugMsg($"prioforce {r != normal} {module} {r} {normal}");
            return r;
        }

#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal bool GetModulePrio(Type module) => (bool)Helpers.Field(module, "priority").GetValue(null);
#endif

        static readonly Dictionary<Type, Dictionary<string, MethodInfo>> cachedMethods = new(200);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public MethodInfo Method<T>(string name, Type[] param = null, Type[] generics = null) =>
            Method(typeof(T), name, param, generics);

        static public MethodInfo Method(Type type, string name, Type[] param = null, Type[] generics = null)
        {
            var key = $"{name}|{param?.Join()}|{generics?.Join()}";
            if (!cachedMethods.TryGetValue(type, out var names) || !names.TryGetValue(key, out var method))
            {
                method = null;

                if (param == null)
                {
                    try
                    {
                        method = type.GetMethod(name, AccessTools.allDeclared) ?? type.GetMethod(name, AccessTools.all);
                    }
                    catch
                    {
                    }
                    if (method == null)
                        param ??= [];
                }

                if (method == null)
                {
                    try
                    {
                        method = type.GetMethod(name, AccessTools.allDeclared, null, param, []) ?? type.GetMethod(name, AccessTools.all, null, param, []);
                    }
                    catch (AmbiguousMatchException)
                    {
                        // great, 2 functions have the same params and name but one is generic and one isn't.
                        // now we have to do the annoying thing of iterating them

                        method = type.GetMethods(AccessTools.all).FirstOrDefault(x =>
                            x.Name == name &&
                            x.GetParameters().Select(p => p.ParameterType).SequenceEqual(param) &&
                            x.IsGenericMethod == (generics != null));
                    }
                }

                if (method == null)
                    NeonLite.Logger.DebugMsg($"Failed to find method {type} {name}!!!!");

                if (method != null && generics != null)
                    method = method.MakeGenericMethod(generics);
                if (cachedMethods.ContainsKey(type))
                    cachedMethods[type][key] = method;
                else
                    cachedMethods[type] = new(1)
                    {
                        [key] = method
                    };

            }

            return method;
        }
        static readonly Dictionary<Type, Dictionary<string, FieldInfo>> cachedFields = new(200);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public FieldInfo Field<T>(string name) =>
            Field(typeof(T), name);
        static public FieldInfo Field(Type type, string name)
        {
            if (!cachedFields.TryGetValue(type, out var names) || !names.TryGetValue(name, out var field))
            {
                field = type.GetField(name, AccessTools.allDeclared) ?? type.GetField(name, AccessTools.all);

                if (field != null)
                {
                    if (cachedFields.ContainsKey(type))
                        cachedFields[type][name] = field;
                    else
                        cachedFields[type] = new(1)
                        {
                            [name] = field
                        };
                }
                else
                    NeonLite.Logger.DebugMsg($"Failed to find field {type} {name}!!!!");
            }
            return field;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo MoveNext(this MethodInfo method) => AccessTools.EnumeratorMoveNext(method);

        static readonly MethodInfo gdmSave = Method(typeof(GameDataManager), "GetPlayerSaveDataPath");
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public string GetSaveDirectory() => Path.Combine(Application.persistentDataPath, Path.GetDirectoryName((string)gdmSave.Invoke(null, null)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void CreateDirectories(string path) => Directory.CreateDirectory(Path.GetDirectoryName(path));

        static readonly StringBuilder timerBuilder = new();
        public static string FormatTime(long timeMS, bool? three = null, char split = ':', bool cutoff = false, bool msAtAll = true)
        {
            timerBuilder.Clear();
            int num = (int)(timeMS / 60000L);
            int num2 = (int)(timeMS / 1000L) % 60;
            int num3 = (int)(timeMS - (num * 60000L) - (num2 * 1000L));

            if (num >= (cutoff ? 10 : 100))
                for (int i = (int)Math.Log10(num); i >= (cutoff ? 1 : 2); --i)
                    timerBuilder.Append((char)(num / (int)Math.Pow(10, i) % 10 + '0'));

            num %= 100;
            if (!cutoff)
                timerBuilder.Append((char)((num / 10) + '0'));
            timerBuilder.Append((char)((num % 10) + '0'));

            timerBuilder.Append(':');
            timerBuilder.Append((char)((num2 / 10) + '0'));
            timerBuilder.Append((char)((num2 % 10) + '0'));
            if (msAtAll)
            {
                timerBuilder.Append(split);
                timerBuilder.Append((char)((num3 / 100) + '0'));
                num3 %= 100;
                timerBuilder.Append((char)((num3 / 10) + '0'));
                if (three ?? ShowMS.setting.Value)
                    timerBuilder.Append((char)((num3 % 10) + '0'));
            }

            return timerBuilder.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ScreenToCanvasPosition(this Canvas canvas, Vector3 screenPosition)
        {
            var viewportPosition = new Vector3(screenPosition.x / Screen.width,
                                               screenPosition.y / Screen.height,
                                               0);
            return canvas.ViewportToCanvasPosition(viewportPosition);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ViewportToCanvasPosition(this Canvas canvas, Vector3 viewportPosition)
        {
            var centerBasedViewPortPosition = viewportPosition - new Vector3(0.5f, 0.5f, 0);
            var canvasRect = canvas.GetComponent<RectTransform>();
            var scale = canvasRect.sizeDelta;
            return Vector3.Scale(centerBasedViewPortPosition, scale);
        }

        public static Texture2D LoadTexture(byte[] image, FilterMode filterMode = FilterMode.Trilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            Texture2D texture2D = new(1, 1, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(texture2D, image, true);
            texture2D.wrapMode = wrapMode;
            texture2D.filterMode = filterMode;
            return texture2D;
        }

        // load a sprite with a default centered pivot
        public static Sprite LoadSprite(byte[] image, FilterMode filterMode = FilterMode.Trilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp, Vector2? pivot = null)
        {
            var tex = LoadTexture(image, filterMode, wrapMode);
            if (!pivot.HasValue)
                pivot = Vector2.one / 2;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width, tex.height) * pivot.Value);
        }

        public static void DownloadURL(string url, Action<UnityWebRequest> callback)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.SendWebRequest().completed += _ => callback(webRequest);
        }

        public static void ResizeWithPivot(this RectTransform transform, Vector2 diff)
        {
            var upos = transform.localPosition;
            var d = diff * transform.pivot;
            upos += new Vector3(d.x, d.y, 0);
            transform.localPosition = upos;

            var usize = transform.sizeDelta;
            usize += diff;
            transform.sizeDelta = usize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FpLn([CallerFilePath] string fp = "", [CallerLineNumber] int ln = 0) => $"[{Path.GetFileName(fp)}:{ln}]";

        static readonly Stack<ProfilerMarker> currentMarkers = [];
        static readonly Stack<Tuple<string, Stopwatch>> currentWatches = [];

        static bool profiling = true;

        const bool FORCE_PROFILING = false;

        [Conditional("ENABLE_PROFILER")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnableProfiling(bool enable) => profiling = enable;

        [Conditional("ENABLE_PROFILER")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartProfiling(string name, [CallerFilePath] string fp = "", [CallerLineNumber] int ln = 0)
        {
            if (!profiling)
                return;
            if (NeonLite.DEBUG || FORCE_PROFILING)
                currentWatches.Push(new(name, new Stopwatch()));
            currentMarkers.Push(new(ProfilerCategory.Scripts, name));
            currentMarkers.Peek().Begin();
            if (NeonLite.DEBUG || FORCE_PROFILING)
            {
                NeonLite.Logger.Msg($"{FpLn(fp, ln)} {name} - START");
                currentWatches.Peek().Item2.Start();
            }
        }
#if ENABLE_PROFILER
        public static IEnumerable<T> ProfileLoop<T>(this IEnumerable<T> loop, string name, [CallerFilePath] string fp = "", [CallerLineNumber] int ln = 0)
        {
            StartProfiling(name, fp, ln);
            int i = 0;
            foreach (T t in loop)
            {
                StartProfiling($"{name}#{++i}", fp, ln);
                yield return t;
                EndProfiling(fp, ln);
            }
            EndProfiling(fp, ln);
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> ProfileLoop<T>(this IEnumerable<T> loop, string _) => loop;
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_PROFILER")]
        public static void EndProfiling(string _, [CallerFilePath] string fp = "", [CallerLineNumber] int ln = 0) => EndProfiling();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_PROFILER")]
        public static void EndProfiling([CallerFilePath] string fp = "", [CallerLineNumber] int ln = 0)
        {
            if (!profiling)
                return;

            currentMarkers.Pop().End();
            if (NeonLite.DEBUG || FORCE_PROFILING)
            {
                (var name, var watch) = currentWatches.Pop();
                watch.Stop();
                NeonLite.Logger.Msg($"[{FpLn(fp, ln)}] {name} - {watch.Elapsed.TotalMilliseconds}ms");
            }
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DebugMsg(this MelonLogger.Instance log, string msg, [CallerFilePath] string fp = "", [CallerLineNumber] int ln = 0)
        {
            if (NeonLite.DEBUG)
            {
                log.Msg($"{FpLn(fp, ln)} {msg}");
                UnityEngine.Debug.Log($"[NeonLite] {FpLn(fp, ln)} {msg}");
            }
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DebugMsg(this MelonLogger.Instance log, object obj) => DebugMsg(log, obj.ToString());

        [Conditional("BETA"), Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BetaMsg(this MelonLogger.Instance log, string msg)
        {
            log.Msg(msg);
            UnityEngine.Debug.Log($"[NeonLite] {msg}");
        }

        [Conditional("BETA"), Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BetaMsg(this MelonLogger.Instance log, object obj) => DebugMsg(log, obj.ToString());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValue<T>(this FieldInfo fieldInfo, object instance) => (T)fieldInfo.GetValue(instance);
    }
}
