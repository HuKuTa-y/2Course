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

namespace _2course
{
    public partial class MainWindow : Window
    {
        // Классы данных
        public class Codek
        {
            public string id { get; set; }
            public string Название { get; set; }
        }

        public class Law
        {
            public string id { get; set; }
            public string Название { get; set; }
        }

        public class ArticleFull
        {
            public string id { get; set; }
            public string Название { get; set; }
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

            // Загрузка text_articles.bin
            await LoadTextArticlesBinaryAsync();

            // Создаем кнопки для кодексов
            foreach (var item in codeksArticles)
            {
                var btn = new Button
                {
                    Content = item.Название,
                    Margin = new Thickness(5),
                    Tag = item
                };
                CodesPanel.Children.Add(btn);
            }

            // Создаем кнопки для законов
            foreach (var item in lawsArticles)
            {
                var btn = new Button
                {
                    Content = item.Название,
                    Margin = new Thickness(5),
                    Tag = item
                };
                LawsPanel.Children.Add(btn);
            }

            // Создаем кнопки для статей из articles_full.json
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
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                btn.Tag = item;
                ArticlesPanel.Children.Add(btn);
            }

            // Отображение текста из text_articles.bin
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

                    ContentStackPanel.Children.Add(border);
                }
            }
        }

        private async Task LoadTextArticlesBinaryAsync()
        {
            string path = "text_articles.bin";
            if (File.Exists(path))
            {
                try
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        var formatter = new BinaryFormatter();
                        var obj = formatter.Deserialize(fs);
                        textArticles = obj as List<TextArticle>;
                        if (textArticles == null)
                            textArticles = new List<TextArticle>();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при десериализации text_articles.bin: {ex.Message}");
                    textArticles = new List<TextArticle>();
                }
            }
            else
            {
                textArticles = new List<TextArticle>();
                // Можно сразу сохранить пустой файл, чтобы не было ошибок при следующем запуске
                await SaveTextArticlesBinaryAsync();
            }
        }

        private async Task SaveTextArticlesBinaryAsync()
        {
            string path = "text_articles.bin";
            try
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fs, textArticles);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сериализации text_articles.bin: {ex.Message}");
            }
        }

        // Метод для добавления нового элемента в textArticles и сохранения
        private async Task AddTextArticleAsync(TextArticle newItem)
        {
            if (textArticles == null)
                textArticles = new List<TextArticle>();
            textArticles.Add(newItem);
            await SaveTextArticlesBinaryAsync();
        }
    }
}
