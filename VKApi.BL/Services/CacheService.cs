using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using VKApi.BL.Interfaces;

namespace VKApi.BL.Services
{
    public class CacheService : ICacheService
    {

        private readonly string cacheFolderName = "cache";

        public CacheService()
        {

        }

        public void Create(object data, string key)
        {
            try
            {
                var filePath = GetCahceFileName(key);
                CreateCacheDir();
                using (StreamWriter file = File.CreateText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public T Get<T>(string key)
        {
            var fileName = GetCahceFileName(key);

            if (!File.Exists(fileName))
            {
                return default;
            }

            var text = File.ReadAllText(fileName);
            var data = JsonConvert.DeserializeObject<T>(text);
            return data;
        }

        public void Append<T>(T valueToAppend, string key)
        {
            try
            {
                var data = Get<List<T>>(key);

                if (data != null)
                {
                    data.Add(valueToAppend);
                    Create(data, key);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        private void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private void CreateFile(string path)
        {
            DeleteFile(path);
            CreateCacheDir();
            File.Create(path);
        }

        private void CreateCacheDir()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var cahceFolder = $"{appDir}{cacheFolderName}";
            if (!Directory.Exists(cahceFolder))
            {
                Directory.CreateDirectory(cahceFolder);
            }
        }

        private string GetCahceFileName(string key)
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var cahceFileName = $"{appDir}{cacheFolderName}\\{key}.json";
            return cahceFileName;
        }
    }
}
