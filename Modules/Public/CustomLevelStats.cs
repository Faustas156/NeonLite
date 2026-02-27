#pragma warning disable IDE0130
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json;

namespace NeonLite.Modules
{
    [Module]
    public static class CustomLevelStats
    {
#pragma warning disable CS0414
        const bool priority = true;
        const bool active = true;

        static readonly ConditionalWeakTable<LevelStats, Dictionary<string, object>> customs = new();

        static readonly MethodInfo[] toPatchW = [
            Helpers.Method(typeof(GameDataManager), "SaveGame"),
            Helpers.Method(typeof(GameDataManager), "SaveGameBackup"),
            Helpers.Method(typeof(GameDataManager), "SaveLevelStats"),
        ];


        static readonly MethodInfo[] toPatchR = [
            Helpers.Method(typeof(GameDataManager), "DeserializePlayerSaveData"),
        ];


        static void Activate(bool _)
        {
            foreach (var m in toPatchW)
                Patching.AddPatch(m, AddConverterW, Patching.PatchTarget.Transpiler);
            foreach (var m in toPatchR)
                Patching.AddPatch(m, AddConverterR, Patching.PatchTarget.Transpiler);
        }

        public static T GetCustom<T>(this LevelStats stats, string key)
        {
            var dict = customs.GetOrCreateValue(stats);

            if (!dict.TryGetValue(key, out var val))
            {
                if (typeof(T).IsPrimitive)
                    val = default(T);
                else
                    val = Activator.CreateInstance<T>();
                dict.Add(key, val);
            }

            if (val.GetType() == typeof(Unclaimed))
            {
                if (((Unclaimed)val).ConvertTo<T>(out var ret))
                    dict[key] = ret;
                return ret;
            }

            return (T)val;
        }

        // For values that aren't primitives.
        public static void SetCustom(this LevelStats stats, string key, object val)
        {
            var dict = customs.GetOrCreateValue(stats);

            if (!dict.ContainsKey(key))
                dict.Add(key, val);
            else
                dict[key] = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasCustom(this LevelStats stats, string key)
        {
            var dict = customs.GetOrCreateValue(stats);
            return dict.ContainsKey(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T StatsGetCustom<T>(this LevelData level, string key)
        {
            var stats = GameDataManager.GetLevelStats(level.levelID);
            if (stats == null)
                return default;
            return stats.GetCustom<T>(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StatsSetCustom(this LevelData level, string key, object val)
        {
            var stats = GameDataManager.GetLevelStats(level.levelID);
            if (stats == null)
                return;
            stats.SetCustom(key, val);
        }

        public static bool StatsHasCustom(this LevelData level, string key)
        {
            var stats = GameDataManager.GetLevelStats(level.levelID);
            if (stats == null)
                return false;
            return stats.HasCustom(key);
        }


        static readonly MethodInfo ogjsonso = Helpers.Method(typeof(JsonConvert), "SerializeObject", [typeof(object), typeof(Formatting)]);
        static readonly MethodInfo ogjsondo = Helpers.Method(typeof(JsonConvert), "DeserializeObject", [typeof(string)], [typeof(PlayerSaveData)]);
        // static readonly MethodInfo nwjsonso = Helpers.Method(typeof(JsonConvert), "SerializeObject", [typeof(object), typeof(Formatting), typeof(JsonConverter[])]);

        static IEnumerable<CodeInstruction> AddConverterW(IEnumerable<CodeInstruction> instructions, ILGenerator ilgenerator)
        {
            return new CodeMatcher(instructions, ilgenerator)
                .MatchForward(false, new CodeMatch(x => x.Calls(ogjsonso)))
                .Repeat(m =>
                {
                    m.RemoveInstruction();
                    m.InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldc_I4_1),
                            new CodeInstruction(OpCodes.Newarr, typeof(CustomStatsSerializer)),
                            new CodeInstruction(OpCodes.Dup),
                            new CodeInstruction(OpCodes.Ldc_I4_0),
                            new CodeInstruction(OpCodes.Newobj, typeof(CustomStatsSerializer).GetConstructor(Type.EmptyTypes)),
                            new CodeInstruction(OpCodes.Stelem_Ref),

                            CodeInstruction.Call(typeof(JsonConvert), "SerializeObject", [typeof(object), typeof(Formatting), typeof(JsonConverter[])])
                        );
                })
                .InstructionEnumeration();
        }
        static IEnumerable<CodeInstruction> AddConverterR(IEnumerable<CodeInstruction> instructions, ILGenerator ilgenerator)
        {
            return new CodeMatcher(instructions, ilgenerator)
                .MatchForward(false, new CodeMatch(x => x.Calls(ogjsondo)))
                .Repeat(m =>
                {
                    m.RemoveInstruction();
                    m.InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldc_I4_1),
                            new CodeInstruction(OpCodes.Newarr, typeof(CustomStatsSerializer)),
                            new CodeInstruction(OpCodes.Dup),
                            new CodeInstruction(OpCodes.Ldc_I4_0),
                            new CodeInstruction(OpCodes.Newobj, typeof(CustomStatsSerializer).GetConstructor(Type.EmptyTypes)),
                            new CodeInstruction(OpCodes.Stelem_Ref),

                            CodeInstruction.Call(typeof(JsonConvert), "DeserializeObject", [typeof(string), typeof(JsonConverter[])], [typeof(PlayerSaveData)])
                        );
                })
                .InstructionEnumeration();
        }

        class CustomStatsSerializer : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(LevelStats);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                existingValue ??= new LevelStats();

                int startDepth = reader.Depth + 1;
                NeonLite.Logger.DebugMsg($"read start depth {startDepth}");

                try
                {
                    while (reader.Read() && reader.Depth >= startDepth)
                    {
                        NeonLite.Logger.DebugMsg($"type {reader.TokenType} value {reader.Value} depth {reader.Depth}");

                        // it should ALWAYS start at propertyname
                        var prop = (string)reader.Value;
                        if (prop == "mod")
                        {
                            // we are in MOD MODE.
                            var dict = customs.GetOrCreateValue((LevelStats)existingValue);
                            int depth = reader.Depth + 1;
                            reader.Read(); // read the start object serializer
                            while (reader.Read() && reader.Depth >= depth)
                            {
                                // once again, always start with the name of the custom
                                var customName = (string)reader.Value;
                                reader.Read(); // start array
                                var p = reader.ReadAsBoolean().Value;

                                if (p)
                                {
                                    reader.Read();
                                    dict.Add(customName, serializer.Deserialize(reader));
                                }
                                else
                                {
                                    reader.Read();
                                    dict.Add(customName, new Unclaimed(serializer.Deserialize<string>(reader)));
                                }

                                reader.Read(); //end array
                            }
                        }
                        else
                        {
                            // use helpers field and see if we hit
                            var f = Helpers.Field(typeof(LevelStats), prop);

                            if (f != null)
                            {
                                NeonLite.Logger.DebugMsg($"{f} {f.FieldType}");
                                reader.Read();
                                var r = serializer.Deserialize(reader, f.FieldType);
                                NeonLite.Logger.DebugMsg($"{r}");
                                f.SetValue(existingValue, r);
                            }
                            else
                            {
                                reader.Read();
                                reader.Skip();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    NeonLite.Logger.Warning("Error parsing LevelStats");
                    NeonLite.Logger.Error(e);
                }

                NeonLite.Logger.DebugMsg($"end read");

                return existingValue;
            }


            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                // i don't know how to write this the "correct" way so this is my way
                writer.WriteStartObject();

                var fields = typeof(LevelStats).GetFields(BindingFlags.Public | BindingFlags.Instance).Where(x => !x.IsNotSerialized);

                foreach (var f in fields)
                {
                    writer.WritePropertyName(f.Name);
                    serializer.Serialize(writer, f.GetValue(value));
                }

                if (customs.TryGetValue((LevelStats)value, out var dict))
                {
                    writer.WritePropertyName("mod");
                    writer.WriteStartObject();

                    foreach (var kv in dict)
                    {
                        writer.WritePropertyName(kv.Key);

                        var p = kv.Value.GetType().IsPrimitive || kv.Value is string;

                        writer.WriteStartArray();
                        writer.WriteValue(p);

                        if (p)
                            writer.WriteValue(p);
                        else
                            writer.WriteValue(JsonConvert.SerializeObject(kv.Value));

                        writer.WriteEndArray();
                    }

                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
        }


        [JsonConverter(typeof(Serializer))]
        class Unclaimed(string d)
        {
            // literally just raw data
            public string data = d;

            public bool ConvertTo<T>(out T val)
            {
                try
                {
                    val = JsonConvert.DeserializeObject<T>(data);
                    return true;
                }
                catch
                {
                    val = default;
                    return false;
                }
            }

            class Serializer : JsonConverter
            {
                public override bool CanConvert(Type objectType) => objectType == typeof(Unclaimed);

                public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

                public override void WriteJson(JsonWriter writer, object value, JsonSerializer _) => writer.WriteRawValue(((Unclaimed)value).data);
            }
        }


        static void OnLevelLoad(LevelData level)
        {
            if (!level || level.type == LevelData.LevelType.Hub)
                return;
            var stats = GameDataManager.GetLevelStats(level.levelID);
            if (stats == null)
                return;

            var mtd = stats.GetCustom<Metadata>("metadata");
            mtd.lastPlayed = DateTime.UtcNow;
            mtd.nlVer = NeonLite.i.Info.Version;
        }

        [Serializable]
        public class Metadata
        {
            public DateTime lastPlayed;
            public DateTime lastFinished;
            public DateTime lastPBd;

            public string nlVer;
        }
    }
}
