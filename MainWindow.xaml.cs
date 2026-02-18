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

            // Загружаем только списки для кнопок
            _ = LoadBasicListsAsync();
        }

        

        private async Task LoadBasicListsAsync()
        {
            try
            {
                // 🔹 Загружаем ТОЛЬКО кодексы (для кнопок)
                string jsonCodeks = await httpClient.GetStringAsync($"{ApiBaseUrl}/api/codeks");
                codeksList = JsonSerializer.Deserialize<List<Codek>>(jsonCodeks, jsonOptions);

                // 🔹 Загружаем ТОЛЬКО законы (для кнопок)
                string jsonLaws = await httpClient.GetStringAsync($"{ApiBaseUrl}/api/laws");
                lawsList = JsonSerializer.Deserialize<List<Law>>(jsonLaws, jsonOptions);

                // 🔹 Создаём кнопки кодексов
                if (codeksList != null)
                {
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

                // 🔹 Создаём кнопки законов
                if (lawsList != null)
                {
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

                // Статьи и тексты НЕ загружаем — будем запрашивать с сервера по клику!

            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(
                    $"Не удалось подключиться к серверу API.\n\n{ex.Message}\n\n" +
                    $"Проверьте:\n• Запущен ли Docker-контейнер\n• Правильный ли IP: 192.168.133.20\n• Открыт ли порт 5000",
                    "Ошибка подключения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

            // 🔹 Запрашиваем статьи С СЕРВЕРА (не локально!)
            await LoadArticlesFromServerAsync(sourceNumber);
        }

        private async Task LoadArticlesFromServerAsync(string sourceNumber)
        {
            try
            {
                // 🔹 Запрос к серверу: получить статьи ТОЛЬКО этого источника
                string encodedSource = Uri.EscapeDataString(sourceNumber);
                string url = $"{ApiBaseUrl}/api/articles/by-source?source_number={encodedSource}";

                string json = await httpClient.GetStringAsync(url);
                var articles = JsonSerializer.Deserialize<List<ArticleFull>>(json, jsonOptions);

                // Очищаем панель статей
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

                // Создаём кнопки для полученных статей
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
                        Tag = article,  // Сохраняем статью для клика
                        ToolTip = article.Ссылка
                    };
                    btn.Click += ArticleButton_Click;
                    ArticlesPanel.Children.Add(btn);
                }

                // 🔹 Тексты статей НЕ загружаем — загрузим при клике на конкретную статью!

            }
            catch (HttpRequestException)
            {
                MessageBox.Show("Не удалось загрузить статьи. Проверьте подключение к серверу.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        

        private async void ArticleButton_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем/очищаем предыдущие тексты
            ContentStackPanel.Children.Clear();

            var btn = sender as Button;
            if (btn?.Tag is not ArticleFull article)
                return;

            try
            {
                // 🔹 Запрашиваем текст статьи С СЕРВЕРА
                string encodedName = Uri.EscapeDataString(article.Название);
                string url = $"{ApiBaseUrl}/api/article/text?article_name={encodedName}";

                string json = await httpClient.GetStringAsync(url);
                var textArticle = JsonSerializer.Deserialize<TextArticle>(json, jsonOptions);

                // Создаём и показываем текст
                var textBlock = new TextBlock
                {
                    Text = textArticle?.Контент ?? "Текст не найден",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(5),
                    FontSize = 12
                };

                var border = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(5),
                    Width = 400,
                    Padding = new Thickness(10),
                    Background = Brushes.LightYellow,
                    Child = textBlock
                };

                ContentStackPanel.Children.Add(border);

                // Прокрутка к новому контенту
                

            }
            catch (HttpRequestException)
            {
                MessageBox.Show($"Не удалось загрузить текст статьи '{article.Название}'.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
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

                // 🔹 Запрос поиска НА СЕРВЕРЕ
                string url = $"{ApiBaseUrl}/api/search/by-number?number={searchNumber}";
                string json = await httpClient.GetStringAsync(url);

                var articles = JsonSerializer.Deserialize<List<ArticleFull>>(json, jsonOptions);

                // Очищаем и заполняем панель
                ArticlesPanel.Children.Clear();

                if (articles == null || articles.Count == 0)
                {
                    MessageBox.Show($"Статьи с номером {searchNumber} не найдены.");
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
                // 🔹 Запрос поиска по тексту НА СЕРВЕРЕ
                string encodedQuery = Uri.EscapeDataString(searchText);
                string url = $"{ApiBaseUrl}/api/search/by-text?query={encodedQuery}";

                string json = await httpClient.GetStringAsync(url);
                var articles = JsonSerializer.Deserialize<List<ArticleFull>>(json, jsonOptions);

                ArticlesPanel.Children.Clear();

                if (articles == null || articles.Count == 0)
                {
                    MessageBox.Show($"Статьи по запросу '{searchText}' не найдены.");
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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}");
            }
        }


        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text == "Введите номер статьи")
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
            else if (textBox != null && textBox.Text == "Поиск...")
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