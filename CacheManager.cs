using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace _2course
{
    /// <summary>
    /// Менеджер кэша с поддержкой офлайн-режима и гибридной загрузки
    /// </summary>
    public static class CacheManager
    {
        //  Папка для кэша: %LocalAppData%\LawApp\Cache
        private static readonly string CacheFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LawApp", "Cache");

        //  Время жизни кэша (24 часа — можно изменить)
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);

        //  HTTP-клиент и базовый URL (задаются при инициализации)
        public static HttpClient HttpClient { get; set; }
        public static string ApiBaseUrl { get; set; }

        /// <summary>
        /// Инициализация менеджера кэша
        /// </summary>
        public static void Initialize(HttpClient httpClient, string baseUrl)
        {
            HttpClient = httpClient;
            ApiBaseUrl = baseUrl;

            if (!Directory.Exists(CacheFolder))
                Directory.CreateDirectory(CacheFolder);
        }

        /// <summary>
        ///  ГИБРИДНАЯ ЗАГРУЗКА:
        /// - Если есть свежий кэш → загружаем из него (быстро, офлайн)
        /// - Если кэш устарел или нет → скачиваем с сервера и сохраняем
        /// - Если нет интернета, но есть старый кэш → используем его (деградация)
        /// </summary>
        public static async Task<T> GetDataAsync<T>(
            string endpoint,
            string cacheFileName,
            bool forceRefresh = false,
            bool isEssential = false)  // 🔥 Ключевой параметр для гибридного подхода
        {
            var cachePath = Path.Combine(CacheFolder, cacheFileName);
            var metadataPath = cachePath + ".meta";

            //  Шаг 1: Проверяем кэш (если не форсируем обновление)
            if (!forceRefresh && File.Exists(cachePath) && File.Exists(metadataPath))
            {
                try
                {
                    var metaJson = await File.ReadAllTextAsync(metadataPath);
                    var meta = JsonSerializer.Deserialize<CacheMetadata>(metaJson);

                    // Если кэш ещё свежий → используем его
                    if (meta != null && DateTime.UtcNow - meta.CachedAt < CacheLifetime)
                    {
                        var cachedJson = await File.ReadAllTextAsync(cachePath);
                        return JsonSerializer.Deserialize<T>(cachedJson, GetJsonOptions());
                    }
                }
                catch
                {
                    // Кэш повреждён → скачаем заново
                }
            }

            //  Шаг 2: Пробуем скачать с сервера
            try
            {
                var url = $"{ApiBaseUrl}{endpoint}";
                var json = await HttpClient.GetStringAsync(url);

                // Сохраняем данные в кэш
                await File.WriteAllTextAsync(cachePath, json);

                // Сохраняем метаданные
                var meta = new CacheMetadata
                {
                    CachedAt = DateTime.UtcNow,
                    SourceUrl = url,
                    FileSize = json.Length
                };
                await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(meta));

                return JsonSerializer.Deserialize<T>(json, GetJsonOptions());
            }
            catch (HttpRequestException)
            {
                //  Нет интернета
                if (isEssential)
                {
                    // Для важных данных: если нет кэша → ошибка
                    if (File.Exists(cachePath))
                    {
                        var cachedJson = await File.ReadAllTextAsync(cachePath);
                        return JsonSerializer.Deserialize<T>(cachedJson, GetJsonOptions());
                    }
                    throw new OfflineException("Нет подключения к серверу и нет кэша для обязательных данных");
                }
                else
                {
                    // Для необязательных данных: если есть старый кэш → используем его
                    if (File.Exists(cachePath))
                    {
                        var cachedJson = await File.ReadAllTextAsync(cachePath);
                        return JsonSerializer.Deserialize<T>(cachedJson, GetJsonOptions());
                    }
                    // Если нет кэша → возвращаем default (пустой список и т.п.)
                    return default;
                }
            }
        }

        /// <summary>
        /// Проверить, есть ли кэш для файла
        /// </summary>
        public static bool HasCache(string cacheFileName)
        {
            return File.Exists(Path.Combine(CacheFolder, cacheFileName));
        }

        /// <summary>
        /// Получить возраст кэша (для отображения статуса)
        /// </summary>
        public static TimeSpan? GetCacheAge(string cacheFileName)
        {
            var metaPath = Path.Combine(CacheFolder, cacheFileName + ".meta");
            if (!File.Exists(metaPath)) return null;

            try
            {
                var metaJson = File.ReadAllText(metaPath);
                var meta = JsonSerializer.Deserialize<CacheMetadata>(metaJson);
                return meta != null ? DateTime.UtcNow - meta.CachedAt : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Очистить весь кэш
        /// </summary>
        public static void ClearCache()
        {
            if (Directory.Exists(CacheFolder))
                Directory.Delete(CacheFolder, true);
            Directory.CreateDirectory(CacheFolder);
        }

        /// <summary>
        /// Очистить устаревший кэш (старше CacheLifetime × 2)
        /// </summary>
        public static async Task CleanupOldCacheAsync()
        {
            if (!Directory.Exists(CacheFolder)) return;

            var maxAge = CacheLifetime * 2;

            foreach (var file in Directory.GetFiles(CacheFolder, "*.cache.json"))
            {
                var metaPath = file + ".meta";
                if (!File.Exists(metaPath)) continue;

                try
                {
                    var metaJson = await File.ReadAllTextAsync(metaPath);
                    var meta = JsonSerializer.Deserialize<CacheMetadata>(metaJson);

                    if (meta != null && DateTime.UtcNow - meta.CachedAt > maxAge)
                    {
                        File.Delete(file);
                        File.Delete(metaPath);
                    }
                }
                catch { /* Игнорируем ошибки при очистке */ }
            }
        }

        //  Вспомогательные методы
        private static JsonSerializerOptions GetJsonOptions() =>
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        //  Метаданные кэша
        private class CacheMetadata
        {
            public DateTime CachedAt { get; set; }
            public string SourceUrl { get; set; }
            public int FileSize { get; set; }
        }
    }

    /// <summary>
    /// Исключение для офлайн-режима
    /// </summary>
    public class OfflineException : Exception
    {
        public OfflineException(string message) : base(message) { }
    }
}