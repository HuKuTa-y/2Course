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
    using System.Threading.Tasks;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Xml.Serialization;
    using System.Linq;


namespace _2course
{
    public partial class MainWindow : Window
    {
        // Классы данных
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

        // Поля для хранения данных
        private List<Codek> codeksArticles;
        private List<Law> lawsArticles;
        private List<ArticleFull> articlesFull;
        private List<TextArticle> textArticles;

        // HttpClient для запросов к API
        private static readonly HttpClient httpClient = new HttpClient();

        // Базовый URL вашего API
        private const string ApiBaseUrl = "http://192.168.133.20:5000/api";

        public MainWindow()
        {
            InitializeComponent();

            // Настройка таймаута для HTTP запросов
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            _ = LoadDataAsync(); // запуск асинхронной загрузки
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // Загрузка codeks.json через API
                try
                {
                    string jsonCodeks = await httpClient.GetStringAsync($"{ApiBaseUrl}/codeks");
                    codeksArticles = JsonSerializer.Deserialize<List<Codek>>(jsonCodeks, options);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке кодексов: {ex.Message}\nПроверьте, запущен ли Docker контейнер с API.");
                    return;
                }

                // Загрузка laws.json через API
                try
                {
                    string jsonLaws = await httpClient.GetStringAsync($"{ApiBaseUrl}/laws");
                    lawsArticles = JsonSerializer.Deserialize<List<Law>>(jsonLaws, options);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке законов: {ex.Message}");
                    return;
                }

                // Загрузка articles_full.json через API
                try
                {
                    string jsonArticlesFull = await httpClient.GetStringAsync($"{ApiBaseUrl}/articles_full");
                    articlesFull = JsonSerializer.Deserialize<List<ArticleFull>>(jsonArticlesFull, options);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке статей: {ex.Message}");
                    return;
                }

                // Загрузка text_articles.json через API
                try
                {
                    string jsonTextArticles = await httpClient.GetStringAsync($"{ApiBaseUrl}/text_articles");
                    textArticles = JsonSerializer.Deserialize<List<TextArticle>>(jsonTextArticles, options);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке текстов статей: {ex.Message}");
                    textArticles = new List<TextArticle>();
                }

                // Создаем кнопки для кодексов и цепляем обработчики
                foreach (var item in codeksArticles)
                {
                    var btn = new Button
                    {
                        Content = item.Название,
                        Margin = new Thickness(5),
                        Tag = item
                    };
                    btn.Click += CodeOrLawButton_Click;
                    CodesPanel.Children.Add(btn);
                }

                // Создаем кнопки для законов и цепляем обработчики
                foreach (var item in lawsArticles)
                {
                    var btn = new Button
                    {
                        Content = item.Название,
                        Margin = new Thickness(5),
                        Tag = item
                    };
                    btn.Click += CodeOrLawButton_Click;
                    LawsPanel.Children.Add(btn);
                }

                // Создаем кнопки для статей
                foreach (var item in articlesFull)
                {
                    var textBlock = new TextBlock
                    {
                        Text = item.Название,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 200
                    };

                    var btn = new Button
                    {
                        Content = textBlock,
                        Margin = new Thickness(5),
                        Width = 390,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Tag = item,
                        Visibility = Visibility.Collapsed // по умолчанию скрыты
                    };
                    btn.Click += ArticleButton_Click; // добавляем обработчик
                    ArticlesPanel.Children.Add(btn);
                }

                // Тексты статей из text_articles.json
                if (textArticles != null && textArticles.Count > 0)
                {
                    foreach (var item in textArticles)
                    {
                        var textBlock = new TextBlock
                        {
                            Text = $"{item.Название}\n{item.Контент}",
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(5)
                        };

                        var border = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(5),
                            Margin = new Thickness(5),
                            Padding = new Thickness(5),
                            Background = Brushes.LightYellow,
                            Child = textBlock
                        };

                        // Изначально скрыт
                        border.Visibility = Visibility.Collapsed;
                        ContentStackPanel.Children.Add(border);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка инициализации: {ex.Message}");
            }
        }

        // Обработчик нажатия на кнопку кодекса или закона
        private void CodeOrLawButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            // Получаем связанный объект
            var item = btn.Tag;
            string номер = null;

            if (item is Codek codek)
                номер = codek.Номер;
            else if (item is Law law)
                номер = law.Номер;

            if (string.IsNullOrEmpty(номер))
                return;

            // Ищем статьи по номеру источника
            ShowArticlesBySourceNumber(номер);
        }

        private void ShowArticlesBySourceNumber(string sourceNumber)
        {
            // Скрываем все статьи и тексты
            foreach (Button btn in ArticlesPanel.Children)
            {
                btn.Visibility = Visibility.Collapsed;
            }

            foreach (var child in ContentStackPanel.Children)
            {
                if (child is Border border)
                    border.Visibility = Visibility.Collapsed;
            }

            // Находим статьи по номеру
            var matchingArticles = articlesFull.FindAll(a => a.Номер_источника_статьи == sourceNumber);

            // Показываем найденные статьи
            foreach (var article in matchingArticles)
            {
                // Ищем кнопку статьи по названию или создаем, если не найдена
                bool found = false;
                foreach (Button btn in ArticlesPanel.Children)
                {
                    if (btn.Tag is ArticleFull a && a.Название == article.Название)
                    {
                        btn.Visibility = Visibility.Visible;
                        found = true;
                        break;
                    }
                }
                // Если не найдено, создаем новую кнопку
                if (!found)
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
                        Visibility = Visibility.Visible
                    };
                    btn.Click += ArticleButton_Click; // добавляем обработчик
                    ArticlesPanel.Children.Add(btn);
                }
            }

            // Также показываем связанные тексты статей, если есть
            for (int i = 0; i < textArticles.Count; i++)
            {
                var t = textArticles[i];
                if (t.Название == sourceNumber) // или по другой логике
                {
                    if (i < ContentStackPanel.Children.Count && ContentStackPanel.Children[i] is Border border)
                        border.Visibility = Visibility.Visible;
                }
            }
        }

        // Обработчик для кнопки "Отмена" (CancelSearchButton_Click)
        private void CancelSearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Сброс текста поиска
            ArticleNumberTextBox.Text = "Введите номер статьи";
            ArticleNumberTextBox.Foreground = Brushes.Gray;
        }

        // Обработчик для кнопки "Найти статьи" (FindArticlesButton_Click)
        private async void FindArticlesButton_Click(object sender, RoutedEventArgs e)
        {
            string searchNumber = ArticleNumberTextBox.Text.Trim();
            if (searchNumber == "Введите номер статьи" || string.IsNullOrEmpty(searchNumber))
            {
                MessageBox.Show("Пожалуйста, введите номер статьи для поиска.");
                return;
            }
            await ShowArticlesByNumberAsync(searchNumber);
        }

        // Обработчик для TextBox при получении фокуса (TextBox_GotFocus)
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text == "Введите номер статьи")
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        // Обработчик для TextBox при потере фокуса (TextBox_LostFocus)
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Введите номер статьи";
                textBox.Foreground = Brushes.Gray;
            }
        }

        // Обработчик для отображения текста статьи
        private void ArticleButton_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем все предыдущие отображения
            foreach (var child in ContentStackPanel.Children)
            {
                if (child is Border border)
                    border.Visibility = Visibility.Collapsed;
            }

            var btn = sender as Button;
            if (btn == null) return;

            if (btn.Tag is ArticleFull article)
            {
                // Ищем текст по названию
                var matchingText = textArticles?.Find(t => t.Название == article.Название);
                if (matchingText != null)
                {
                    // Создаем блок текста
                    var textBlock = new TextBlock
                    {
                        Text = matchingText.Контент,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(5)
                    };

                    var border = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(5),
                        Margin = new Thickness(5),
                        Width = 300,
                        Padding = new Thickness(5),
                        Background = Brushes.LightYellow,
                        Child = textBlock
                    };

                    ContentStackPanel.Children.Add(border);
                    border.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show($"Текст для статьи '{article.Название}' не найден.");
                }
            }
        }

        private async Task ShowArticlesByNumberAsync(string sourceNumber)
        {
            // Очистка текущих отображений
            foreach (Button btn in ArticlesPanel.Children)
            {
                btn.Visibility = Visibility.Collapsed;
            }

            foreach (var child in ContentStackPanel.Children)
            {
                if (child is Border border)
                    border.Visibility = Visibility.Collapsed;
            }

            int searchNumber;
            if (!int.TryParse(sourceNumber, out searchNumber))
            {
                MessageBox.Show("Введите допустимый числовой номер.");
                return;
            }

            var matchingArticles = new List<ArticleFull>();

            foreach (var article in articlesFull)
            {
                // Предположим, что название может содержать число, например "Статья 123"
                string title = article.Название;
                int articleNumber = ExtractNumberFromTitle(title);
                if (articleNumber == searchNumber)
                {
                    matchingArticles.Add(article);
                }
            }

            if (matchingArticles.Count == 0)
            {
                MessageBox.Show("Статьи по данному номеру не найдены.");
                return;
            }

            // Отображение найденных статей
            foreach (var article in matchingArticles)
            {
                bool found = false;
                foreach (Button btn in ArticlesPanel.Children)
                {
                    if (btn.Tag is ArticleFull a && a.Название == article.Название)
                    {
                        btn.Visibility = Visibility.Visible;
                        found = true;
                        break;
                    }
                }
                if (!found)
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
                        Visibility = Visibility.Visible
                    };
                    btn.Click += ArticleButton_Click;
                    ArticlesPanel.Children.Add(btn);
                }
            }

            // Показываем связанные тексты, если есть
            for (int i = 0; i < textArticles.Count; i++)
            {
                var t = textArticles[i];
                if (t.Название == sourceNumber)
                {
                    if (i < ContentStackPanel.Children.Count && ContentStackPanel.Children[i] is Border border)
                        border.Visibility = Visibility.Visible;
                }
            }
        }

        private async Task ShowArticlesByTextAsync(string searchText)
        {
            // Очистка текущих отображений
            foreach (Button btn in ArticlesPanel.Children)
            {
                btn.Visibility = Visibility.Collapsed;
            }

            foreach (var child in ContentStackPanel.Children)
            {
                if (child is Border border)
                    border.Visibility = Visibility.Collapsed;
            }

            // Разделяем введённый текст на слова
            var searchWords = searchText.Split(new char[] { ' ', ',', '.', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(w => w.ToLower()).ToList();

            var matchingArticles = new List<ArticleFull>();

            foreach (var article in articlesFull)
            {
                string titleLower = article.Название.ToLower();

                // Проверяем, содержит ли название хотя бы одно слово поиска
                bool containsWord = searchWords.Any(word => titleLower.Contains(word));

                if (containsWord)
                {
                    matchingArticles.Add(article);
                }
            }

            if (matchingArticles.Count == 0)
            {
                MessageBox.Show("Статьи по данному тексту не найдены.");
                return;
            }

            // Отображение найденных статей (аналогично предыдущему примеру)
            foreach (var article in matchingArticles)
            {
                bool found = false;
                foreach (Button btn in ArticlesPanel.Children)
                {
                    if (btn.Tag is ArticleFull a && a.Название == article.Название)
                    {
                        btn.Visibility = Visibility.Visible;
                        found = true;
                        break;
                    }
                }
                if (!found)
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
                        Visibility = Visibility.Visible
                    };
                    btn.Click += ArticleButton_Click;
                    ArticlesPanel.Children.Add(btn);
                }
            }

            // Показываем связанные тексты, если есть
            for (int i = 0; i < textArticles.Count; i++)
            {
                var t = textArticles[i];
                if (searchWords.Any(w => t.Название.ToLower().Contains(w)))
                {
                    if (i < ContentStackPanel.Children.Count && ContentStackPanel.Children[i] is Border border)
                        border.Visibility = Visibility.Visible;
                }
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                await ShowArticlesByTextAsync(searchText);
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите слова для поиска.");
            }
        }

        // Вспомогательная функция для извлечения числа из строки
        private int ExtractNumberFromTitle(string title)
        {
            var digits = new StringBuilder();
            foreach (char c in title)
            {
                if (char.IsDigit(c))
                    digits.Append(c);
            }
            if (int.TryParse(digits.ToString(), out int number))
                return number;
            return -1; // Возвращает -1, если число не найдено
        }
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> 2a920f526f6e44f0e1b838896eee053933ad1822
