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
        private List<Article> allArticles = new List<Article>();
        private List<Article> articlesCodeks = new List<Article>();
        private List<Article> articlesLaws = new List<Article>();

        public MainWindow()
        {
            InitializeComponent();
            LoadAllJsonFiles();
        }

        private void LoadAllJsonFiles()
        {
            var allArticlesFromFiles = new List<Article>();

            string folderPath = @"C:\Users\Academy\source\repos\2course"; // замените путь

            string lawsFilePath = System.IO.Path.Combine(folderPath, "laws.json");
            string codeksFilePath = System.IO.Path.Combine(folderPath, "codeks.json");
            string articlesCodeksPath = System.IO.Path.Combine(folderPath, "articles_codeks.json");
            ;

            // Загрузка законов
            if (File.Exists(lawsFilePath))
            {
                try
                {
                    string jsonLaws = File.ReadAllText(lawsFilePath);
                    var laws = JsonSerializer.Deserialize<List<Article>>(jsonLaws);
                    if (laws != null)
                        allArticlesFromFiles.AddRange(laws);
                }
                catch { }
            }

            // Загрузка кодексов
            if (File.Exists(codeksFilePath))
            {
                try
                {
                    string jsonCodeks = File.ReadAllText(codeksFilePath);
                    var codeks = JsonSerializer.Deserialize<List<Article>>(jsonCodeks);
                    if (codeks != null)
                        allArticlesFromFiles.AddRange(codeks);
                }
                catch { }
            }

            // Загрузка статей кодексов
            if (File.Exists(articlesCodeksPath))
            {
                try
                {
                    string jsonArticlesCodeks = File.ReadAllText(articlesCodeksPath);
                    var articlesCodeksData = JsonSerializer.Deserialize<List<Article>>(jsonArticlesCodeks);
                    if (articlesCodeksData != null)
                        articlesCodeks = articlesCodeksData;
                }
                catch { }
            }

            // Загрузка статей законов


            allArticles = allArticlesFromFiles;

            GenerateArticleButtons();
        }

        private void GenerateArticleButtons()
        {
            ArticlesPanel.Children.Clear();

            foreach (var item in allArticles)
            {
                var btn = new Button
                {
                    Content = item.Название,
                    Margin = new Thickness(3),
                    Tag = item
                };
                btn.Click += SourceButton_Click;
                ArticlesPanel.Children.Add(btn);
            }
        }

        // Эта панель - для отображения статей, создадим её в XAML
        // например, добавьте в MainWindow.xaml:
        // <StackPanel x:Name="ArticlesListPanel" Orientation="Vertical" />

        private void SourceButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Article sourceItem)
            {
                // Очистить предыдущие статьи
                ArticlesListPanel.Children.Clear();

                // Найти статьи, принадлежащие выбранному источнику по Номер_источника_статьи
                string sourceNumber = sourceItem.Номер_источника_статьи;

                // Искать в кодексах и законах
                var relatedArticles = new List<Article>();
                if (!string.IsNullOrEmpty(sourceNumber))
                {
                    relatedArticles.AddRange(articlesCodeks.FindAll(a => a.Номер_источника_статьи == sourceNumber));
                    relatedArticles.AddRange(articlesLaws.FindAll(a => a.Номер_источника_статьи == sourceNumber));
                }

                if (relatedArticles.Count > 0)
                {
                    // Для каждого статьи создаем кнопку
                    foreach (var article in relatedArticles)
                    {
                        var articleBtn = new Button
                        {
                            Content = $"{article.Номер} - {article.Название}",
                            Margin = new Thickness(2),
                            Tag = article
                        };
                        articleBtn.Click += ArticleButton_Click;
                        ArticlesListPanel.Children.Add(articleBtn);
                    }
                }
                else
                {
                    // Если статей нет, показываем сообщение
                    var txt = new TextBlock
                    {
                        Text = "Статьи не найдены.",
                        Margin = new Thickness(5)
                    };
                    ArticlesListPanel.Children.Add(txt);
                }
            }
        }

        private void ArticleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Article article)
            {
                MessageBox.Show($"Вы выбрали статью:\n{article.Номер} - {article.Название}", "Статья");
            }
        }
    }

    public class Article
    {
        public string id { get; set; }
        public string Название { get; set; }
        public string Ссылка { get; set; }
        public string Номер_источника_статьи { get; set; }
        public string Номер { get; set; }
    }
}