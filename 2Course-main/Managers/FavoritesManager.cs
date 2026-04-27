using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace _2course.Managers
{
    public static class FavoritesManager
    {
        private static readonly string FavoritesFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LawApp", "favorites.json");

        private static List<string> _favorites;
        private static readonly object _lock = new object();

        static FavoritesManager() => LoadFavorites();

        private static void LoadFavorites()
        {
            try
            {
                if (File.Exists(FavoritesFile))
                {
                    var json = File.ReadAllText(FavoritesFile);
                    _favorites = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
                else
                {
                    _favorites = new List<string>();
                }
            }
            catch { _favorites = new List<string>(); }
        }

        private static void SaveFavorites()
        {
            try
            {
                var dir = Path.GetDirectoryName(FavoritesFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(_favorites, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FavoritesFile, json);
            }
            catch { }
        }

        public static bool IsFavorite(string articleName)
        {
            if (string.IsNullOrEmpty(articleName)) return false;
            lock (_lock) return _favorites.Contains(articleName);
        }

        public static void ToggleFavorite(string articleName)
        {
            if (string.IsNullOrEmpty(articleName)) return;
            lock (_lock)
            {
                if (_favorites.Contains(articleName))
                    _favorites.Remove(articleName);
                else
                    _favorites.Add(articleName);
                SaveFavorites();
            }
        }

        public static List<string> GetFavorites()
        {
            lock (_lock) return new List<string>(_favorites);
        }

        public static int GetFavoritesCount()
        {
            lock (_lock) return _favorites.Count;
        }
    }
}