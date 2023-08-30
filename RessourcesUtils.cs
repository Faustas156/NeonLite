using Steamworks;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

namespace NeonLite
{
    static internal class RessourcesUtils
    {
        private static readonly DataContractJsonSerializerSettings _jsonSettings = new() { UseSimpleDictionaryFormat = true };

        internal static bool GetDirectoryPath(out string path)
        {
            path = null;
            if (!SteamManager.Initialized) return false;

            path = Application.persistentDataPath + "/" + SteamUser.GetSteamID().m_SteamID.ToString() + "/NeonLite/";
            return true;
        }

        public static void SaveToFile<T>(string path, string filename, object data)
        {
            Task.Run(() =>
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                Stream stream = File.Open(path + filename, FileMode.Create);
                XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  ");
                DataContractJsonSerializer serializer = new(typeof(T), _jsonSettings);
                serializer.WriteObject(writer, data);
                writer.Flush();
                writer.Close();
                stream.Close();
            });
        }

        public static T ReadFile<T>(string path, string filename)
        {
            StreamReader streamReader = new(path + filename, Encoding.UTF8);
            string data = streamReader.ReadToEnd();
            streamReader.Close();
            DataContractJsonSerializer deserializer = new(typeof(T), _jsonSettings);
            MemoryStream memoryStream = new(Encoding.UTF8.GetBytes(data));
            T result = (T)deserializer.ReadObject(memoryStream);
            memoryStream.Close();
            return result;
        }

        public static T ReadFile<T>(byte[] ressource)
        {
            DataContractJsonSerializer deserializer = new(typeof(T), _jsonSettings);
            MemoryStream memoryStream = new(ressource);
            T result = (T)deserializer.ReadObject(new MemoryStream(ressource));
            memoryStream.Close();
            return result;
        }

        internal static void DownloadRessource<T>(string url, Action<DownloadResult> callback)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.SendWebRequest().completed +=
                result =>
                {
                    if (webRequest.result != UnityWebRequest.Result.Success)
                        callback(new DownloadResult() { success = false });
                    else
                    {
                        T data;
                        try
                        {
                            data = ReadFile<T>(webRequest.downloadHandler.data);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                            return;
                        }
                        callback(new DownloadResult() { success = true, data = data });
                    }
                };
        }

        internal struct DownloadResult
        {
            public bool success;
            public object data;
        }
    }
}
