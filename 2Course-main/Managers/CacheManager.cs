using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Collections.Concurrent;

namespace _2course.Managers
{
    public static class CacheManager
    {
        private static readonly string CacheFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LawApp", "Cache");

        private static readonly TimeSpan DefaultCacheLifetime = TimeSpan.FromHours(24);
        public const string CurrentSchemaVersion = "1.0";

        public static HttpClient? HttpClient { get; set; }
        public static string? ApiBaseUrl { get; set; }

        private static readonly ConcurrentDictionary<string, DateTime> _refreshQueue = new();

        public static void Initialize(HttpClient httpClient, string baseUrl)
        {
            HttpClient = httpClient;
            ApiBaseUrl = baseUrl;
            if (!Directory.Exists(CacheFolder)) Directory.CreateDirectory(CacheFolder);
        }

        public static string GetSafeCacheFileName(string originalName, string extension = ".cache.json")
        {
            if (string.IsNullOrEmpty(originalName))
                return $"empty_{Guid.NewGuid():N}{extension}";

            byte[] hashBytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(originalName));
            string hash = string.Concat(hashBytes.Select(b => b.ToString("x2")));
            return $"{hash.Substring(0, Math.Min(64, hash.Length))}{extension}";
        }

        public static async Task<T?> GetDataAsync<T>(
            string endpoint,
            string cacheFileName,
            bool forceRefresh = false,
            bool isEssential = false,
            TimeSpan? customLifetime = null,
            string? schemaVersion = null)
        {
            var lifetime = customLifetime ?? DefaultCacheLifetime;
            var expectedSchema = schemaVersion ?? CurrentSchemaVersion;
            var cachePath = Path.Combine(CacheFolder, cacheFileName);
            var metadataPath = cachePath + ".meta";

            if (!forceRefresh && File.Exists(cachePath) && File.Exists(metadataPath))
            {
                try
                {
                    var metaJson = await File.ReadAllTextAsync(metadataPath);
                    var meta = JsonSerializer.Deserialize<CacheMetadata>(metaJson);

                    if (meta?.SchemaVersion != expectedSchema)
                    {
                        File.Delete(cachePath);
                        File.Delete(metadataPath);
                    }
                    else if (meta != null && DateTime.UtcNow - meta.CachedAt < lifetime)
                    {
                        var cachedJson = await File.ReadAllTextAsync(cachePath);
                        var result = JsonSerializer.Deserialize<T>(cachedJson, GetJsonOptions());
                        _ = RefreshCacheInBackgroundAsync(endpoint, cacheFileName, isEssential);
                        return result;
                    }
                }
                catch { try { File.Delete(cachePath); File.Delete(metadataPath); } catch { } }
            }

            try
            {
                if (HttpClient == null || string.IsNullOrEmpty(ApiBaseUrl))
                    throw new InvalidOperationException("CacheManager not initialized");

                var url = $"{ApiBaseUrl}{endpoint}";
                var json = await HttpClient.GetStringAsync(url);
                await File.WriteAllTextAsync(cachePath, json);

                var meta = new CacheMetadata
                {
                    CachedAt = DateTime.UtcNow,
                    SourceUrl = url,
                    FileSize = json.Length,
                    SchemaVersion = expectedSchema
                };
                await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(meta));

                return JsonSerializer.Deserialize<T>(json, GetJsonOptions());
            }
            catch (HttpRequestException)
            {
                if (File.Exists(cachePath))
                {
                    try
                    {
                        var cachedJson = await File.ReadAllTextAsync(cachePath);
                        return JsonSerializer.Deserialize<T>(cachedJson, GetJsonOptions());
                    }
                    catch { }
                }
                if (isEssential)
                    throw new OfflineException("Нет подключения к серверу и нет кэша для обязательных данных");
                return default;
            }
        }

        private static async Task RefreshCacheInBackgroundAsync(string endpoint, string cacheFileName, bool isEssential)
        {
            if (_refreshQueue.TryGetValue(cacheFileName, out var lastRefresh) &&
                DateTime.UtcNow - lastRefresh < TimeSpan.FromMinutes(5))
                return;

            _refreshQueue[cacheFileName] = DateTime.UtcNow;

            try
            {
                await Task.Delay(2000);
                if (HttpClient == null || string.IsNullOrEmpty(ApiBaseUrl)) return;

                var url = $"{ApiBaseUrl}{endpoint}";
                var json = await HttpClient.GetStringAsync(url);

                var cachePath = Path.Combine(CacheFolder, cacheFileName);
                var metadataPath = cachePath + ".meta";

                await File.WriteAllTextAsync(cachePath, json);

                var meta = new CacheMetadata
                {
                    CachedAt = DateTime.UtcNow,
                    SourceUrl = url,
                    FileSize = json.Length,
                    SchemaVersion = CurrentSchemaVersion
                };
                await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(meta));
            }
            catch { _refreshQueue.TryRemove(cacheFileName, out _); }
        }

        public static bool HasCache(string cacheFileName) =>
            File.Exists(Path.Combine(CacheFolder, cacheFileName));

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
            catch { return null; }
        }

        public static async Task<bool> ForceRefreshAsync<T>(string endpoint, string cacheFileName, string? schemaVersion = null)
        {
            try
            {
                if (HttpClient == null || string.IsNullOrEmpty(ApiBaseUrl)) return false;

                var expectedSchema = schemaVersion ?? CurrentSchemaVersion;
                var url = $"{ApiBaseUrl}{endpoint}";
                var json = await HttpClient.GetStringAsync(url);

                var cachePath = Path.Combine(CacheFolder, cacheFileName);
                var metadataPath = cachePath + ".meta";

                await File.WriteAllTextAsync(cachePath, json);

                var meta = new CacheMetadata
                {
                    CachedAt = DateTime.UtcNow,
                    SourceUrl = url,
                    FileSize = json.Length,
                    SchemaVersion = expectedSchema
                };
                await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(meta));
                return true;
            }
            catch { return false; }
        }

        public static void ClearCache()
        {
            if (Directory.Exists(CacheFolder)) Directory.Delete(CacheFolder, true);
            Directory.CreateDirectory(CacheFolder);
            _refreshQueue.Clear();
        }

        public static async Task CleanupOldCacheAsync(TimeSpan? customLifetime = null)
        {
            if (!Directory.Exists(CacheFolder)) return;
            var lifetime = (customLifetime ?? DefaultCacheLifetime) * 2;

            foreach (var file in Directory.GetFiles(CacheFolder, "*.cache.json"))
            {
                var metaPath = file + ".meta";
                if (!File.Exists(metaPath)) continue;
                try
                {
                    var metaJson = await File.ReadAllTextAsync(metaPath);
                    var meta = JsonSerializer.Deserialize<CacheMetadata>(metaJson);
                    if (meta != null && DateTime.UtcNow - meta.CachedAt > lifetime)
                    {
                        File.Delete(file);
                        File.Delete(metaPath);
                    }
                }
                catch { }
            }
        }

        private static JsonSerializerOptions GetJsonOptions() =>
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private class CacheMetadata
        {
            public DateTime CachedAt { get; set; }
            public string SourceUrl { get; set; } = "";
            public int FileSize { get; set; }
            public string SchemaVersion { get; set; } = CurrentSchemaVersion;
        }
    }

    public class OfflineException : Exception
    {
        public OfflineException(string message) : base(message) { }
    }
}