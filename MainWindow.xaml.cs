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

namespace _2course
{
    public partial class MainWindow : Window
    {
        private List<Article> codeksArticles;
        private List<Article> lawsArticles;
        private List<ArticleFull> articlesFull;
        

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            // Загрузка codeks.json
            try
            {
                string codeksPath = "codeks.json";
                string codeksJson = File.ReadAllText(codeksPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                codeksArticles = JsonSerializer.Deserialize<List<Article>>(codeksJson, options);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке codeks.json: {ex.Message}");
                return;
            }

            // Загрузка laws.json
            try
            {
                string lawsPath = "laws.json";
                string lawsJson = File.ReadAllText(lawsPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                lawsArticles = JsonSerializer.Deserialize<List<Article>>(lawsJson, options);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке laws.json: {ex.Message}");
                return;
            }

            // Загрузка articles_full.json
            try
            {
                string articlesPath = "articles_full.json";
                string articlesJson = File.ReadAllText(articlesPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                articlesFull = JsonSerializer.Deserialize<List<ArticleFull>>(articlesJson, options);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке articles_full.json: {ex.Message}");
                return;
            }

            // Создаем кнопки для кодексов
            foreach (var article in codeksArticles)
            {
                var btn = new Button
                {
                    Content = article.Название,
                    Margin = new Thickness(5),
                    Tag = article
                };
                CodesPanel.Children.Add(btn);
            }

            // Создаем кнопки для законов
            foreach (var article in lawsArticles)
            {
                var btn = new Button
                {
                    Content = article.Название,
                    Margin = new Thickness(5),
                    Tag = article
                };
                LawsPanel.Children.Add(btn);
            }

            // Создаем кнопки для статей из articles_full.json
            foreach (var article in articlesFull)
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
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };

                btn.Tag = article;
                ArticlesPanel.Children.Add(btn);
            }
        }
    }

    // Общий класс для статей (закон, кодекс)
    public class Article
    {
        public string id { get; set; }
        public string Название { get; set; }
        public string Ссылка { get; set; }
        public string Номер { get; set; }
    }

    // Для статей из articles_full.json
    public class ArticleFull
    {
        public string id { get; set; }
        public string Название { get; set; }
        public string Ссылка { get; set; }
        public string Номер_источника_статьи { get; set; }
    }
}