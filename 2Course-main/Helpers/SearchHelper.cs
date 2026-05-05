using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _2course.Models;

namespace _2course.Helpers
{
    public static class SearchHelper
    {
        // 🔥 Проверяет: является ли запись статьёй (а не главой, параграфом и т.д.)
        public static bool IsArticle(string title) =>
            !string.IsNullOrEmpty(title) &&
            (title.Contains("Статья", StringComparison.OrdinalIgnoreCase) ||
             title.Contains("ст.", StringComparison.OrdinalIgnoreCase));

        // 🔥 Фильтрует список: оставляет только статьи
        public static List<ArticleFull> FilterArticlesOnly(List<ArticleFull>? articles)
        {
            if (articles == null) return new List<ArticleFull>();
            // Явно передаем article в проверку
            return articles.Where(article => IsArticle(article.Название)).ToList();
        }

        // 🔥 Поиск по номеру + фильтрация только статей
        public static List<ArticleFull> SearchByNumberOnlyArticles(List<ArticleFull>? allArticles, int searchNumber)
        {
            if (allArticles == null) return new List<ArticleFull>();

            return allArticles
                .Where(a => ExtractNumberFromTitle(a.Название) == searchNumber && IsArticle(a.Название))
                .ToList();
        }

        // Вспомогательный метод: извлекает число из названия ("Статья 7" → 7)
        private static int ExtractNumberFromTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return -1;
            var digits = new StringBuilder();
            foreach (char c in title) if (char.IsDigit(c)) digits.Append(c);
            return int.TryParse(digits.ToString(), out int n) ? n : -1;
        }
    }
}