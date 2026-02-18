using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _2course
{
    public partial class MainWindow : Window
    {

        public class Codek
        {
            public string id { get; set; }
            public string Название { get; set; }
            public string Ссылка { get; set; }
            public string Номер { get; set; }
        }

        public class Law
        {
            public string id { get; set; }
            public string Название { get; set; }
            public string Ссылка { get; set; }
            public string Номер { get; set; }
        }

        public class ArticleFull
        {
            public string id { get; set; }
            public string Название { get; set; }
            public string Ссылка { get; set; }
            public string Номер_источника_статьи { get; set; }
        }

        public class TextArticle
        {
            public string Название { get; set; }
            public string Контент { get; set; }
        }


        // Списки ТОЛЬКО для кнопок (кодексы и законы)
        private List<Codek> codeksList;
        private List<Law> lawsList;
        private List<ArticleFull> articlesFull;  // Добавлено для гибридного подхода

        // HttpClient для запросов к API
        private static readonly HttpClient httpClient = new HttpClient();

        // Базовый URL API (без /api в конце — добавляем в запросах)
        private const string ApiBaseUrl = "http://192.168.133.20:5000";

        // Настройки JSON
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };


        public MainWindow()
        {
            InitializeComponent();

            // Настройка HTTP-клиента
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            // Инициализация CacheManager
            CacheManager.Initialize(httpClient, ApiBaseUrl);
            // 🔥 Гибридная загрузка: essentials + articles_full
            _ = LoadHybridDataAsync();
            // Фоновая очистка старого кэша
            _ = CacheManager.CleanupOldCacheAsync();

            

        }



        private async Task LoadHybridDataAsync()
        {
            try
            {
                //  ЭТАП 1: Обязательные маленькие файлы (всегда загружаем)
                // isEssential=true → ошибка если нет интернета и нет кэша

                codeksList = await CacheManager.GetDataAsync<List<Codek>>(
                    "/api/codeks",
                    "codeks.cache.json",
                    isEssential: true);  // 🔥 Обязательно для работы UI

                lawsList = await CacheManager.GetDataAsync<List<Law>>(
                    "/api/laws",
                    "laws.cache.json",
                    isEssential: true);  // 🔥 Обязательно для работы UI

                //  ЭТАП 2: articles_full (3 MB) — нужно для поиска, но не критично
                // isEssential=false → если нет интернета, вернёт null, но приложение продолжит работу

                try
                {
                    articlesFull = await CacheManager.GetDataAsync<List<ArticleFull>>(
                        "/api/articles_full",
                        "articles_full.cache.json",
                        isEssential: false);  // 🔥 Не критично, можно работать без
                }
                catch (OfflineException)
                {
                    // Нет интернета и нет кэша → продолжаем без articles_full
                    // Поиск будет недоступен, но кодексы/законы работают
                    articlesFull = new List<ArticleFull>();
                }

                //  ЭТАП 3: Создаём UI (кнопки кодексов и законов)
                BuildCodeksButtons();
                BuildLawsButtons();

                //  ЭТАП 4: Обновляем статус (опционально)
                UpdateStatus();

            }
            catch (OfflineException ex)
            {
                //  Критическая ошибка: нет интернета и нет кэша для обязательных данных
                MessageBox.Show(
                    $"Не удалось загрузить данные.\n\n{ex.Message}\n\n" +
                    $"Подключитесь к интернету для первого запуска приложения.",
                    "Ошибка загрузки",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //  Вспомогательные методы для построения UI
        private void BuildCodeksButtons()
        {
            if (codeksList == null) return;

            CodesPanel.Children.Clear();
            foreach (var item in codeksList)
            {
                var btn = new Button
                {
                    Content = item.Название,
                    Margin = new Thickness(5),
                    Tag = item,
                    ToolTip = $"Номер: {item.Номер}"
                };
                btn.Click += CodeOrLawButton_Click;
                CodesPanel.Children.Add(btn);
            }
        }

        private void BuildLawsButtons()
        {
            if (lawsList == null) return;

            LawsPanel.Children.Clear();
            foreach (var item in lawsList)
            {
                var btn = new Button
                {
                    Content = item.Название,
                    Margin = new Thickness(5),
                    Tag = item,
                    ToolTip = $"Номер: {item.Номер}"
                };
                btn.Click += CodeOrLawButton_Click;
                LawsPanel.Children.Add(btn);
            }
        }

        private void UpdateStatus()
        {
            // Опционально: показать статус кэша в StatusBar
            var hasCodeks = CacheManager.HasCache("codeks.cache.json");
            var hasArticles = CacheManager.HasCache("articles_full.cache.json");

            var age = CacheManager.GetCacheAge("articles_full.cache.json");
            var ageText = age.HasValue ? $" (обновлено {age.Value:hh\\:mm} назад)" : "";

            // Если есть StatusBar в XAML:
            // StatusText.Text = hasCodeks && hasArticles 
            //     ? $" Данные загружены{ageText}" 
            //     : " Онлайн-режим";
        }

        

        private async void CodeOrLawButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            string sourceNumber = null;
            if (btn.Tag is Codek codek)
                sourceNumber = codek.Номер;
            else if (btn.Tag is Law law)
                sourceNumber = law.Номер;

            if (string.IsNullOrEmpty(sourceNumber))
                return;

            await LoadArticlesFromServerAsync(sourceNumber);
        }

        private async Task LoadArticlesFromServerAsync(string sourceNumber)
        {
            try
            {
                //  Запрос статей по источнику (кэшируется)
                string encodedSource = Uri.EscapeDataString(sourceNumber);

                var articles = await CacheManager.GetDataAsync<List<ArticleFull>>(
                    $"/api/articles/by-source?source_number={encodedSource}",
                    $"articles_{Uri.EscapeDataString(sourceNumber)}.cache.json",
                    isEssential: false);  // Не критично, если не загрузится

                ArticlesPanel.Children.Clear();

                if (articles == null || articles.Count == 0)
                {
                    ArticlesPanel.Children.Add(new TextBlock
                    {
                        Text = "Статьи не найдены",
                        FontStyle = FontStyles.Italic,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(10)
                    });
                    return;
                }

                foreach (var article in articles)
                {
                    var textBlock = new TextBlock
                    {
                        Text = article.Название,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 200
                    };

                    var btn = new Button
                    {
                        Content = textBlock,
                        Margin = new Thickness(5),
                        Width = 390,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Tag = article,
                        ToolTip = article.Ссылка
                    };
                    btn.Click += ArticleButton_Click;
                    ArticlesPanel.Children.Add(btn);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

    

        private async void ArticleButton_Click(object sender, RoutedEventArgs e)
        {
            ContentStackPanel.Children.Clear();

            var btn = sender as Button;
            if (btn?.Tag is not ArticleFull article)
                return;

            try
            {
                //  ГИБРИДНЫЙ ПОДХОД: текст загружается ТОЛЬКО при клике
                // isEssential=false → если нет интернета, вернёт null, покажем заглушку

                string encodedName = Uri.EscapeDataString(article.Название);

                var textArticle = await CacheManager.GetDataAsync<TextArticle>(
                    $"/api/article/text?article_name={encodedName}",
                    $"text_{encodedName}.cache.json",
                    isEssential: false);  // 🔥 Lazy load: не критично

                var textBlock = new TextBlock
                {
                    Text = textArticle?.Контент ?? "Текст не загружен (проверьте подключение)",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(5),
                    FontSize = 12,
                    Foreground = textArticle == null ? Brushes.Gray : Brushes.Black
                };

                var border = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(5),
                    Width = 400,
                    Padding = new Thickness(10),
                    Background = textArticle == null ? Brushes.LightGray : Brushes.LightYellow,
                    Child = textBlock
                };

                ContentStackPanel.Children.Add(border);

                // Прокрутка к новому контенту (если есть ScrollViewer)
                // ContentScrollViewer?.ScrollToBottom();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить текст: {ex.Message}");
            }
        }



        private async void FindArticlesButton_Click(object sender, RoutedEventArgs e)
        {
            string searchNumber = ArticleNumberTextBox.Text.Trim();

            if (searchNumber == "Введите номер статьи" || string.IsNullOrEmpty(searchNumber))
            {
                MessageBox.Show("Пожалуйста, введите номер статьи для поиска.");
                return;
            }

            await SearchByNumberAsync(searchNumber);
        }

        private async Task SearchByNumberAsync(string numberString)
        {
            try
            {
                if (!int.TryParse(numberString, out int searchNumber))
                {
                    MessageBox.Show("Введите допустимый числовой номер.");
                    return;
                }

                //  Вариант A: Если articlesFull уже загружен (гибридный подход) — ищем локально
                if (articlesFull != null && articlesFull.Count > 0)
                {
                    var matchingArticles = articlesFull
                        .Where(a => ExtractNumberFromTitle(a.Название) == searchNumber)
                        .ToList();

                    DisplaySearchResults(matchingArticles);
                    return;
                }

                //  Вариант B: Если articlesFull нет — запрашиваем поиск на сервере
                string url = $"{ApiBaseUrl}/api/search/by-number?number={searchNumber}";
                var articles = await CacheManager.GetDataAsync<List<ArticleFull>>(
                    $"/api/search/by-number?number={searchNumber}",
                    $"search_number_{searchNumber}.cache.json",
                    isEssential: false);

                DisplaySearchResults(articles ?? new List<ArticleFull>());

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}");
            }
        }

        

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchText) || searchText == "Поиск...")
            {
                MessageBox.Show("Пожалуйста, введите слова для поиска.");
                return;
            }

            await SearchByTextAsync(searchText);
        }

        private async Task SearchByTextAsync(string searchText)
        {
            try
            {
                //  Вариант A: Локальный поиск в articlesFull (если загружен)
                if (articlesFull != null && articlesFull.Count > 0)
                {
                    var searchWords = searchText.ToLower().Split(new[] { ' ', ',', '.', ';', ':' },
                        StringSplitOptions.RemoveEmptyEntries);

                    var matchingArticles = articlesFull
                        .Where(a => searchWords.Any(w => a.Название.ToLower().Contains(w)))
                        .ToList();

                    DisplaySearchResults(matchingArticles);
                    return;
                }

                //  Вариант B: Поиск на сервере
                string encodedQuery = Uri.EscapeDataString(searchText);
                var articles = await CacheManager.GetDataAsync<List<ArticleFull>>(
                    $"/api/search/by-text?query={encodedQuery}",
                    $"search_text_{encodedQuery}.cache.json",
                    isEssential: false);

                DisplaySearchResults(articles ?? new List<ArticleFull>());

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}");
            }
        }

        //  Вспомогательный метод для отображения результатов поиска
        private void DisplaySearchResults(List<ArticleFull> articles)
        {
            ArticlesPanel.Children.Clear();

            if (articles.Count == 0)
            {
                MessageBox.Show("Статьи не найдены.");
                return;
            }

            foreach (var article in articles)
            {
                var textBlock = new TextBlock
                {
                    Text = article.Название,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 200
                };

                var btn = new Button
                {
                    Content = textBlock,
                    Margin = new Thickness(5),
                    Width = 390,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Tag = article
                };
                btn.Click += ArticleButton_Click;
                ArticlesPanel.Children.Add(btn);
            }
        }

      

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && (textBox.Text == "Введите номер статьи" || textBox.Text == "Поиск..."))
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox.Name == "ArticleNumberTextBox")
                {
                    textBox.Text = "Введите номер статьи";
                    textBox.Foreground = Brushes.Gray;
                }
                else if (textBox.Name == "SearchTextBox")
                {
                    textBox.Text = "Поиск...";
                    textBox.Foreground = Brushes.Gray;
                }
            }
        }

        private void CancelSearchButton_Click(object sender, RoutedEventArgs e)
        {
            ArticleNumberTextBox.Text = "Введите номер статьи";
            ArticleNumberTextBox.Foreground = Brushes.Gray;
            ArticlesPanel.Children.Clear();
            ContentStackPanel.Children.Clear();
        }

    

        private int ExtractNumberFromTitle(string title)
        {
            var digits = new StringBuilder();
            foreach (char c in title)
            {
                if (char.IsDigit(c))
                    digits.Append(c);
            }
            return int.TryParse(digits.ToString(), out int number) ? number : -1;
        }
    }
}