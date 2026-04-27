using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using _2course.Models;
using _2course.Managers;

namespace _2course.Services
{
    public class LawService : ILawService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        private List<Codek> _codeks = new();
        private List<Law> _laws = new();
        private List<ArticleFull> _articlesFull = new();

        public LawService(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        public async Task<List<Codek>> GetCodeksAsync()
        {
            _codeks = await CacheManager.GetDataAsync<List<Codek>>(
                "/api/codeks",
                CacheManager.GetSafeCacheFileName("codeks"),
                isEssential: false);
            return _codeks ?? new List<Codek>();
        }

        public async Task<List<Law>> GetLawsAsync()
        {
            _laws = await CacheManager.GetDataAsync<List<Law>>(
                "/api/laws",
                CacheManager.GetSafeCacheFileName("laws"),
                isEssential: true);
            return _laws ?? new List<Law>();
        }

        public async Task<List<ArticleFull>> GetArticlesFullAsync()
        {
            _articlesFull = await CacheManager.GetDataAsync<List<ArticleFull>>(
                "/api/articles_full",
                CacheManager.GetSafeCacheFileName("articles_full"),
                isEssential: false);
            return _articlesFull ?? new List<ArticleFull>();
        }

        public async Task<List<ArticleFull>> GetArticlesBySourceAsync(string sourceNumber)
        {
            if (string.IsNullOrEmpty(sourceNumber)) return new List<ArticleFull>();
            string encoded = Uri.EscapeDataString(sourceNumber);
            var result = await CacheManager.GetDataAsync<List<ArticleFull>>(
                $"/api/articles/by-source?source_number={encoded}",
                CacheManager.GetSafeCacheFileName($"articles_{sourceNumber}"),
                isEssential: false);
            return result ?? new List<ArticleFull>();
        }

        public async Task<TextArticle> GetArticleTextAsync(string articleName)
        {
            if (string.IsNullOrEmpty(articleName)) return null;
            string encoded = Uri.EscapeDataString(articleName);
            return await CacheManager.GetDataAsync<TextArticle>(
                $"/api/article/text?article_name={encoded}",
                CacheManager.GetSafeCacheFileName($"text_{articleName}"),
                isEssential: false);
        }

        public async Task<List<ArticleFull>> SearchByNumberAsync(int number)
        {
            if (_articlesFull?.Count > 0)
            {
                return _articlesFull
                    .Where(a => ExtractNumberFromTitle(a.Название) == number)
                    .ToList();
            }
            var result = await CacheManager.GetDataAsync<List<ArticleFull>>(
                $"/api/search/by-number?number={number}",
                CacheManager.GetSafeCacheFileName($"search_number_{number}"),
                isEssential: false);
            return result ?? new List<ArticleFull>();
        }

        public async Task<List<ArticleFull>> SearchByTextAsync(string query)
        {
            if (string.IsNullOrEmpty(query)) return new List<ArticleFull>();

            if (_articlesFull?.Count > 0)
            {
                var words = query.ToLower().Split(new[] { ' ', ',', '.', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
                return _articlesFull
                    .Where(a => words.Any(w => a.Название?.ToLower().Contains(w) == true))
                    .ToList();
            }

            string encoded = Uri.EscapeDataString(query);
            var result = await CacheManager.GetDataAsync<List<ArticleFull>>(
                $"/api/search/by-text?query={encoded}",
                CacheManager.GetSafeCacheFileName($"search_text_{encoded}"),
                isEssential: false);
            return result ?? new List<ArticleFull>();
        }

        public List<ArticleFull> GetLoadedArticlesFull() => _articlesFull;

        public async Task RefreshAllDataAsync()
        {
            await CacheManager.ForceRefreshAsync<List<Codek>>("/api/codeks", CacheManager.GetSafeCacheFileName("codeks"));
            await CacheManager.ForceRefreshAsync<List<Law>>("/api/laws", CacheManager.GetSafeCacheFileName("laws"));
            await CacheManager.ForceRefreshAsync<List<ArticleFull>>("/api/articles_full", CacheManager.GetSafeCacheFileName("articles_full"));

            _ = await GetCodeksAsync();
            _ = await GetLawsAsync();
            _ = await GetArticlesFullAsync();
        }

        private int ExtractNumberFromTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return -1;
            var digits = new System.Text.StringBuilder();
            foreach (char c in title) if (char.IsDigit(c)) digits.Append(c);
            return int.TryParse(digits.ToString(), out int n) ? n : -1;
        }
    }
}