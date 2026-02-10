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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

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
            public string id { get; set; }
            public string Контент { get; set; }
        }

        // Поля для хранения данных
        private List<Codek> codeksArticles;
        private List<Law> lawsArticles;
        private List<ArticleFull> articlesFull;
        private List<TextArticle> textArticles;

        public MainWindow()
        {
            InitializeComponent();
            _ = LoadDataAsync(); // запуск асинхронной загрузки
        }

        private async Task LoadDataAsync()
        {
            // Загрузка codeks.json
            try
            {
                string pathCodeks = "codeks.json";
                string jsonCodeks = await File.ReadAllTextAsync(pathCodeks);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                codeksArticles = JsonSerializer.Deserialize<List<Codek>>(jsonCodeks, options);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке codeks.json: {ex.Message}");
                return;
            }

            // Загрузка laws.json
            try
            {
                string pathLaws = "laws.json";
                string jsonLaws = await File.ReadAllTextAsync(pathLaws);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                lawsArticles = JsonSerializer.Deserialize<List<Law>>(jsonLaws, options);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке laws.json: {ex.Message}");
                return;
            }

            // Загрузка articles_full.json
            try
            {
                string pathArticlesFull = "articles_full.json";
                string jsonArticlesFull = await File.ReadAllTextAsync(pathArticlesFull);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                articlesFull = JsonSerializer.Deserialize<List<ArticleFull>>(jsonArticlesFull, options);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке articles_full.json: {ex.Message}");
                return;
            }

            // Потоковая загрузка text_articles.json
            try
            {
                using (var fs = new FileStream("text_articles.json", FileMode.Open, FileAccess.Read))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    textArticles = await JsonSerializer.DeserializeAsync<List<TextArticle>>(fs, options);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке text_articles.json: {ex.Message}");
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
                    Tag = item
                };
                // Можно оставить, чтобы статьи были скрыты изначально
                btn.Visibility = Visibility.Collapsed; 
                ArticlesPanel.Children.Add(btn);
            }

            // Тексты статей из text_articles.json
            if (textArticles != null && textArticles.Count > 0)
            {
                foreach (var item in textArticles)
                {
                    var textBlock = new TextBlock
                    {
                        Text = $"ID: {item.id}\n{item.Контент}",
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
                    if (btn.Tag is ArticleFull a && a.id == article.id)
                    {
                        btn.Visibility = Visibility.Visible;
                        found = true;
                        break;
                    }
                }
                // Если не найдено, можно создать новую кнопку (по желанию)
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
                    ArticlesPanel.Children.Add(btn);
                }
            }

            // Также показываем связанные тексты статей
            for (int i = 0; i < textArticles.Count; i++)
            {
                var t = textArticles[i];
                if (t.id == sourceNumber) // или по другой логике
                {
                    var border = ContentStackPanel.Children[i] as Border;
                    if (border != null)
                        border.Visibility = Visibility.Visible;
                }
            }
        }
    }
}