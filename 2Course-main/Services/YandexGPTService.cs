using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace _2course.Services
{
    // 🔥 Сообщение чата для истории
    public class ChatMessage
    {
        public string Role { get; set; } = ""; // "user", "assistant", "system"
        public string Text { get; set; } = "";
    }

    public class YandexGptAnalysisResult
    {
        public string Text { get; set; } = "";
        public bool Success { get; set; }
        public string Error { get; set; } = "";
    }

    public interface IYandexGptService
    {
        Task<YandexGptAnalysisResult> AnalyzeLegalDocumentAsync(string text, int maxTokens = 2000);
        Task<YandexGptAnalysisResult> AnalyzeLegalDocumentFromFileAsync(string filePath, int maxTokens = 2000);
        // 🔥 НОВЫЙ МЕТОД: с историей диалога
        Task<YandexGptAnalysisResult> ChatWithHistoryAsync(List<ChatMessage> history, string newUserMessage, int maxTokens = 2000);
    }

    public class YandexGptService : IYandexGptService
    {
        private readonly HttpClient _httpClient;
        private readonly string _iamToken;
        private readonly string _folderId;
        private const string ApiUrl = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

        public YandexGptService(string iamToken, string folderId, HttpClient? httpClient = null)
        {
            _iamToken = iamToken ?? throw new ArgumentNullException(nameof(iamToken));
            _folderId = folderId ?? throw new ArgumentNullException(nameof(folderId));
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(2);
        }

        public async Task<YandexGptAnalysisResult> AnalyzeLegalDocumentAsync(string text, int maxTokens = 2000)
        {
            try
            {
                string trimmedText = text.Length > 4000 ? text.Substring(0, 4000) + "\n...(обрезано)" : text;
                string prompt = "Ты — юрист. Просто проанализируй текст ниже и напиши 2-4 предложения о том:\n" +
                                "1. Что это за документ/статья и из какого закона.\n" +
                                "2. К какой отрасли права относится.\n" +
                                "3. В чём его суть.\n\n" +
                                "ВАЖНО: ПИШИ СВЯЗНЫМ ТЕКСТОМ. НЕ ИСПОЛЬЗУЙ СПИСКИ И ФРАЗЫ ТИПА \"НЕ УКАЗАНО\". Сделай логический вывод из контекста. Обязательно упомяни название закона и номер статьи в первом предложении.\n\n" +
                                "Текст:\n---\n" + trimmedText + "\n---\n\n" +
                                "Твой анализ:";

                var requestBody = new
                {
                    modelUri = "gpt://" + _folderId + "/yandexgpt-lite/latest",
                    completionOptions = new { stream = false, temperature = 0.3, maxTokens = maxTokens },
                    messages = new[]
                    {
                        new { role = "system", text = "Отвечай кратко, по делу, обычным текстом." },
                        new { role = "user", text = prompt }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _iamToken);
                _httpClient.DefaultRequestHeaders.Add("x-folder-id", _folderId);

                var response = await _httpClient.PostAsync(ApiUrl, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new YandexGptAnalysisResult { Success = false, Error = "API error: " + response.StatusCode };

                var result = JsonSerializer.Deserialize<YandexGptResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var altText = result?.Result?.Alternatives?.FirstOrDefault()?.Message?.Text?.Trim() ?? "";

                return new YandexGptAnalysisResult { Text = altText, Success = !string.IsNullOrEmpty(altText) };
            }
            catch (Exception ex)
            {
                return new YandexGptAnalysisResult { Success = false, Error = ex.Message };
            }
        }

        // 🔥 НОВЫЙ МЕТОД: чат с полной историей
        public async Task<YandexGptAnalysisResult> ChatWithHistoryAsync(List<ChatMessage> history, string newUserMessage, int maxTokens = 2000)
        {
            try
            {
                // Формируем массив сообщений для API
                var messages = new List<object>();
                messages.Add(new { role = "system", text = "Ты юрист-практик. Отвечай кратко, по делу, обычным текстом. Помни контекст диалога." });
                
                // Добавляем историю (последние 10 сообщений, чтобы не превысить лимит токенов)
                var limitedHistory = history.Count > 10 ? history.Skip(history.Count - 10).ToList() : history;
                foreach (var msg in limitedHistory)
                {
                    messages.Add(new { role = msg.Role, text = msg.Text });
                }
                
                // Добавляем новое сообщение пользователя
                messages.Add(new { role = "user", text = newUserMessage });

                var requestBody = new
                {
                    modelUri = "gpt://" + _folderId + "/yandexgpt-lite/latest",
                    completionOptions = new { stream = false, temperature = 0.3, maxTokens = maxTokens },
                    messages = messages.ToArray()
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _iamToken);
                _httpClient.DefaultRequestHeaders.Add("x-folder-id", _folderId);

                var response = await _httpClient.PostAsync(ApiUrl, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new YandexGptAnalysisResult { Success = false, Error = "API error: " + response.StatusCode };

                var result = JsonSerializer.Deserialize<YandexGptResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var altText = result?.Result?.Alternatives?.FirstOrDefault()?.Message?.Text?.Trim() ?? "";

                return new YandexGptAnalysisResult { Text = altText, Success = !string.IsNullOrEmpty(altText) };
            }
            catch (Exception ex)
            {
                return new YandexGptAnalysisResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<YandexGptAnalysisResult> AnalyzeLegalDocumentFromFileAsync(string filePath, int maxTokens = 2000)
        {
            if (!File.Exists(filePath))
                return new YandexGptAnalysisResult { Success = false, Error = "Файл не найден" };
            try
            {
                string text = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                return await AnalyzeLegalDocumentAsync(text, maxTokens);
            }
            catch (Exception ex)
            {
                return new YandexGptAnalysisResult { Success = false, Error = "Ошибка чтения: " + ex.Message };
            }
        }

        private class YandexGptResponse { public CompletionResult? Result { get; set; } }
        private class CompletionResult { public Alternative[]? Alternatives { get; set; } }
        private class Alternative { public Message? Message { get; set; } }
        private class Message { public string? Text { get; set; } }
    }
}