#if !DEBUG
// #define ENABLE_PROFILER
#endif

using HarmonyLib;
using MelonLoader;
using NeonLite.Modules.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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

        static readonly Dictionary<Type, Dictionary<string, MethodInfo>> cachedMethods = new(200);

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
                    method = type.GetMethod(name, AccessTools.allDeclared, null, param, []) ?? type.GetMethod(name, AccessTools.all, null, param, []);

                if (method != null)
                {
                    if (generics != null)
                        method = method.MakeGenericMethod(generics);
                    if (cachedMethods.ContainsKey(type))
                        cachedMethods[type][key] = method;
                    else
                        cachedMethods[type] = new(1)
                        {
                            [key] = method
                        };
                }
                else
                    NeonLite.Logger.DebugMsg($"Failed to find method {type} {name}!!!!");
            }
            return method;
        }
        static readonly Dictionary<Type, Dictionary<string, FieldInfo>> cachedFields = new(200);
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

        static readonly Stack<ProfilerMarker> currentMarkers = [];
        static readonly Stack<Tuple<string, Stopwatch>> currentWatches = [];

        static bool profiling = true;

        [Conditional("ENABLE_PROFILER")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnableProfiling(bool enable) => profiling = enable;

        [Conditional("ENABLE_PROFILER")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartProfiling(string name)
        {
            if (!profiling)
                return;
            if (NeonLite.DEBUG)
                currentWatches.Push(new(name, new Stopwatch()));
            currentMarkers.Push(new(ProfilerCategory.Scripts, name));
            currentMarkers.Peek().Begin();
            if (NeonLite.DEBUG)
            {
                NeonLite.Logger.Msg($"{name} - START");
                currentWatches.Peek().Item2.Start();
            }
        }
#if ENABLE_PROFILER
        public static IEnumerable<T> ProfileLoop<T>(this IEnumerable<T> loop, string name)
        {
            StartProfiling(name);
            int i = 0;
            foreach (T t in loop)
            {
                StartProfiling($"{name}#{++i}");
                yield return t;
                EndProfiling();
            }
            EndProfiling();
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> ProfileLoop<T>(this IEnumerable<T> loop, string _) => loop;
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_PROFILER")]
        public static void EndProfiling(string _) => EndProfiling();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_PROFILER")]
        public static void EndProfiling()
        {
            if (!profiling)
                return;

            currentMarkers.Pop().End();
            if (NeonLite.DEBUG)
            {
                (var name, var watch) = currentWatches.Pop();
                watch.Stop();
                NeonLite.Logger.Msg($"{name} - {watch.Elapsed.TotalMilliseconds}ms");
            }
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DebugMsg(this MelonLogger.Instance log, string msg)
        {
            if (NeonLite.DEBUG)
            {
                log.Msg(msg);
                UnityEngine.Debug.Log($"[NeonLite] {msg}");
            }
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DebugMsg(this MelonLogger.Instance log, object obj) => DebugMsg(log, obj.ToString());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValue<T>(this FieldInfo fieldInfo, object instance) => (T)fieldInfo.GetValue(instance);
    }
}
