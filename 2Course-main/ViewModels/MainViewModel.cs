using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using _2course.Models;
using _2course.Services;

namespace _2course.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ILawService _lawService;

        private List<Codek>? _codeks;
        private List<Law>? _laws;
        private List<ArticleFull>? _currentArticles;
        private ArticleFull? _selectedArticle;
        private TextArticle? _articleText;
        private string? _statusMessage;
        private bool _isLoading;

        public List<Codek>? Codeks { get => _codeks; set { _codeks = value; OnPropertyChanged(); } }
        public List<Law>? Laws { get => _laws; set { _laws = value; OnPropertyChanged(); } }
        public List<ArticleFull>? CurrentArticles { get => _currentArticles; set { _currentArticles = value; OnPropertyChanged(); } }
        public ArticleFull? SelectedArticle { get => _selectedArticle; set { _selectedArticle = value; OnPropertyChanged(); } }
        public TextArticle? ArticleText { get => _articleText; set { _articleText = value; OnPropertyChanged(); } }
        public string? StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MainViewModel(ILawService lawService)
        {
            _lawService = lawService ?? throw new ArgumentNullException(nameof(lawService));
        }

        public async System.Threading.Tasks.Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                Codeks = await _lawService.GetCodeksAsync();
                Laws = await _lawService.GetLawsAsync();
                _ = await _lawService.GetArticlesFullAsync();
                StatusMessage = "Готово";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async System.Threading.Tasks.Task LoadArticlesAsync(string sourceNumber)
        {
            CurrentArticles = await _lawService.GetArticlesBySourceAsync(sourceNumber);
        }

        public async System.Threading.Tasks.Task LoadArticleTextAsync(ArticleFull article)
        {
            if (article == null) return;
            SelectedArticle = article;
            ArticleText = await _lawService.GetArticleTextAsync(article.Название);
        }

        public async System.Threading.Tasks.Task SearchByNumberAsync(string numberString)
        {
            if (!int.TryParse(numberString, out int number)) return;
            CurrentArticles = await _lawService.SearchByNumberAsync(number);
        }

        public async System.Threading.Tasks.Task SearchByTextAsync(string query)
        {
            CurrentArticles = await _lawService.SearchByTextAsync(query);
        }

        public async System.Threading.Tasks.Task RefreshDataAsync()
        {
            await _lawService.RefreshAllDataAsync();
            await InitializeAsync();
        }
    }
}