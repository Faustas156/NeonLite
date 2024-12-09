#if DEBUG
// #define ENABLE_PROFILER
#else
// #define ENABLE_PROFILER
#endif

using HarmonyLib;
using NeonLite.Modules.UI;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Profiling;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace NeonLite
{
    static public class Helpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public MethodInfo MI(Delegate func) => func.GetMethodInfo();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public HarmonyMethod HM(Delegate func) => new(MI(func));

        static readonly MethodInfo gdmSave = AccessTools.Method(typeof(GameDataManager), "GetPlayerSaveDataPath");
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

        public static Vector3 ScreenToCanvasPosition(this Canvas canvas, Vector3 screenPosition)
        {
            var viewportPosition = new Vector3(screenPosition.x / Screen.width,
                                               screenPosition.y / Screen.height,
                                               0);
            return canvas.ViewportToCanvasPosition(viewportPosition);
        }
        public static Vector3 ViewportToCanvasPosition(this Canvas canvas, Vector3 viewportPosition)
        {
            var centerBasedViewPortPosition = viewportPosition - new Vector3(0.5f, 0.5f, 0);
            var canvasRect = canvas.GetComponent<RectTransform>();
            var scale = canvasRect.sizeDelta;
            return Vector3.Scale(centerBasedViewPortPosition, scale);
        }

        public static void DownloadURL(string url, Action<UnityWebRequest> callback)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.SendWebRequest().completed += _ => callback(webRequest);
        }

#if ENABLE_PROFILER
        static readonly Stack<ProfilerMarker> currentMarkers = [];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartProfiling(string name) 
        {
            currentMarkers.Push(new(ProfilerCategory.Scripts, name));
            currentMarkers.Peek().Begin();
        }
        public static IEnumerable<T> ProfileLoop<T>(IEnumerable<T> loop, string name)
        {
            currentMarkers.Push(new(ProfilerCategory.Scripts, $"{name}"));
            currentMarkers.Peek().Begin();
            int i = 0;
            foreach (T t in loop)
            {
                StartProfiling($"{name}#{++i}");
                yield return t;
                EndProfiling();
            }
            EndProfiling();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EndProfiling() => currentMarkers.Pop().End();
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartProfiling(string _) { }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> ProfileLoop<T>(IEnumerable<T> loop, string _) => loop;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EndProfiling() { }

#endif
    }
}
