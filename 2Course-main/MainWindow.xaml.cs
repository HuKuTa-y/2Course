using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Net.Http;
using _2course.ViewModels;
using _2course.Services;
using _2course.Managers;
using _2course.Controls;
using _2course.Helpers;
using _2course.Models;

namespace _2course
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly ILawService _lawService;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://192.168.133.20:5000";

        // 🔥 Поля для навигации
        private Button _activeSourceButton;
        private ArticleCard _activeArticleCard;

        // 🔥 Поля для Яндекс GPT
        private readonly IYandexGptService? _gptService;
        private AiChatControl? _aiChatControl;

        public MainWindow()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                CodesPanel.Children.Add(new TextBlock { Text = "[Дизайн]", FontStyle = FontStyles.Italic, Foreground = Brushes.Gray, Margin = new Thickness(10) });
                return;
            }

            // 1. Инициализация HTTP и Кэша
            _httpClient.Timeout = System.TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            CacheManager.Initialize(_httpClient, ApiBaseUrl);

            // 2. Инициализация сервисов законов
            _lawService = new LawService(_httpClient, ApiBaseUrl);
            _vm = new MainViewModel(_lawService);

            // 3. 🔥 Инициализация Яндекс GPT
            // Вставь сюда свои данные из мобильного приложения
            string folderId = "b1gih7j22o930q0sp06j";
            string iamToken = "AQVNycnST4heyRx3QdVPc8jGyLYX_f5I_XWC8sSg";

            try
            {
                _gptService = new YandexGptService(iamToken, folderId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации AI: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // 4. Создаем чат сразу, чтобы вкладка не была пустой
            InitializeAiChat();

            // 5. Подписки на события
            KeyDown += MainWindow_KeyDown;
            _ = InitializeAsync();
        }

        private void InitializeAiChat()
        {
            // Создаем контрол чата
            _aiChatControl = new AiChatControl(_gptService);

            // Очищаем контейнер на всякий случай и добавляем чат
            AiChatContainer.Children.Clear();
            AiChatContainer.Children.Add(_aiChatControl);
        }

        // 🔥 Обработчик кнопки из XAML
        private void AiChatButton_Click(object sender, RoutedEventArgs e)
        {
            // Переключаем вкладку на AI
            MainTabs.SelectedIndex = 1;
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            await _vm.InitializeAsync();
            UiHelper.BuildCodeksButtons(CodesPanel, _vm.Codeks, CodeOrLawButton_Click);
            UiHelper.BuildLawsButtons(LawsPanel, _vm.Laws, CodeOrLawButton_Click);
            UpdateStatus();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                ShowFavorites();
            }
            else if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                _ = _vm.RefreshDataAsync();
                ShowStatus("Обновление...");
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CancelSearchButton_Click(null, null);
            }
            // Ctrl+G тоже открывает AI (для удобства)
            else if (e.Key == Key.G && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                MainTabs.SelectedIndex = 1;
            }
        }

        private async void CodeOrLawButton_Click(string sourceNumber)
        {
            if (string.IsNullOrEmpty(sourceNumber)) return;
            ResetActiveSourceButton();
            HighlightActiveButton(sourceNumber);
            await _vm.LoadArticlesAsync(sourceNumber);
            var articles = _vm.CurrentArticles ?? new List<ArticleFull>();
            DisplayArticles(articles, sourceNumber);
        }

        private void ResetActiveSourceButton()
        {
            if (_activeSourceButton != null)
            {
                _activeSourceButton.Background = Brushes.WhiteSmoke;
                _activeSourceButton.Foreground = Brushes.Black;
                _activeSourceButton.FontWeight = FontWeights.Normal;
                _activeSourceButton.BorderBrush = Brushes.LightGray;
                _activeSourceButton.BorderThickness = new Thickness(1);
            }
        }

        private void HighlightActiveButton(string sourceNumber)
        {
            Button? newActiveBtn = null;
            foreach (var child in CodesPanel.Children)
                if (child is Button btn && btn.Tag is Codek c && c.Номер == sourceNumber) { newActiveBtn = btn; break; }

            if (newActiveBtn == null)
                foreach (var child in LawsPanel.Children)
                    if (child is Button btn && btn.Tag is Law l && l.Номер == sourceNumber) { newActiveBtn = btn; break; }

            if (newActiveBtn != null)
            {
                _activeSourceButton = newActiveBtn;
                _activeSourceButton.Background = new SolidColorBrush(Color.FromRgb(200, 220, 240));
                _activeSourceButton.Foreground = Brushes.DarkSlateBlue;
                _activeSourceButton.FontWeight = FontWeights.SemiBold;
                _activeSourceButton.BorderBrush = Brushes.SteelBlue;
                _activeSourceButton.BorderThickness = new Thickness(2);
            }
        }

        private void DisplayArticles(List<ArticleFull> articles, string sourceNumber) =>
            UiHelper.DisplayArticlesWithTreeHeader(
                ArticlesPanel, articles, sourceNumber, _vm.Codeks, _vm.Laws,
                async a => { HighlightArticle(a); await LoadArticleTextAsync(a); },
                a => UiHelper.OpenNoteEditorWindow(this, a, () => RefreshArticleCard(a)),
                UpdateStatus,
                src => UiHelper.ResolveSourceName(src, _vm.Codeks, _vm.Laws));

        private void HighlightArticle(ArticleFull article)
        {
            if (_activeArticleCard != null)
            {
                _activeArticleCard.Background = Brushes.White;
                _activeArticleCard.BorderBrush = Brushes.LightGray;
                _activeArticleCard.BorderThickness = new Thickness(1);
            }
            // Здесь можно добавить логику поиска карточки по ID и её подсветки, 
            // но пока оставим сброс предыдущей, так как карточки пересоздаются
        }

        private async Task LoadArticleTextAsync(ArticleFull article)
        {
            ContentStackPanel.Children.Clear();
            await _vm.LoadArticleTextAsync(article);
            UiHelper.DisplayArticleTextInPanel(ContentStackPanel, _vm.ArticleText);
        }

        private void RefreshArticleCard(ArticleFull article)
        {
            for (int i = 0; i < ArticlesPanel.Children.Count; i++)
            {
                if (ArticlesPanel.Children[i] is ArticleCard card && card.Article?.Название == article.Название)
                {
                    ArticlesPanel.Children.RemoveAt(i);
                    var newCard = new ArticleCard(article);
                    newCard.ArticleClicked += async (s, e) => { HighlightArticle(article); await LoadArticleTextAsync(article); };
                    newCard.NoteClicked += (s, e) => UiHelper.OpenNoteEditorWindow(this, article, () => RefreshArticleCard(article));
                    newCard.FavoriteToggled += (s, e) => UpdateStatus();
                    ArticlesPanel.Children.Insert(i, newCard);
                    break;
                }
            }
        }

        private async void FindArticlesButton_Click(object sender, RoutedEventArgs e)
        {
            string searchNumber = ArticleNumberTextBox.Text.Trim();
            if (searchNumber == "Введите номер статьи" || string.IsNullOrEmpty(searchNumber)) return;
            await _vm.SearchByNumberAsync(searchNumber);
            var filtered = SearchHelper.FilterArticlesOnly(_vm.CurrentArticles);
            DisplayArticles(filtered, "Поиск");
            ShowStatus($"Найдено статей: {filtered.Count}");
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText) || searchText == "Поиск...") return;
            await _vm.SearchByTextAsync(searchText);
            DisplayArticles(_vm.CurrentArticles ?? new List<ArticleFull>(), "Поиск");
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e) => ShowFavorites();

        private void ShowFavorites() =>
            UiHelper.ShowFavoritesList(ArticlesPanel, _lawService,
                async a => { HighlightArticle(a); await LoadArticleTextAsync(a); }, UpdateStatus);

        private void NotesMenuButton_Click(object sender, RoutedEventArgs e) => ShowAllNotes();

        private void ShowAllNotes() =>
            UiHelper.ShowAllNotesList(ArticlesPanel,
                async name => await OpenArticleFromNote(name),
                (name, note) => UiHelper.OpenNoteEditorInline(this, name, note, ShowAllNotes));

        private async Task OpenArticleFromNote(string articleName)
        {
            var article = _lawService.GetLoadedArticlesFull()?.FirstOrDefault(a => a.Название == articleName);
            if (article == null) { MessageBox.Show($"Статья '{articleName}' не найдена"); return; }
            await LoadArticleTextAsync(article);
        }

        private void UpdateStatus()
        {
            var favCount = FavoritesManager.GetFavoritesCount();
            StatusText.Text = favCount > 0 ? $"✓ Избранное: {favCount} | Готово" : "Готово";
            StatusText.Foreground = Brushes.Green;
        }

        private void ShowStatus(string message, bool isError = false)
        {
            StatusText.Text = message;
            StatusText.Foreground = isError ? Brushes.Red : Brushes.Green;
            _ = System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ =>
                Dispatcher.Invoke(() => { if (StatusText.Text == message) StatusText.Text = "Готово"; }));
        }

        private void CancelSearchButton_Click(object sender, RoutedEventArgs e)
        {
            ArticleNumberTextBox.Text = "Введите номер статьи";
            ArticleNumberTextBox.Foreground = Brushes.Gray;
            SearchTextBox.Text = "";
            ArticlesPanel.Children.Clear();
            ContentStackPanel.Children.Clear();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Tag == null && (tb.Text == "Введите номер статьи" || tb.Text == "Поиск..."))
            {
                tb.Tag = tb.Text; tb.Text = ""; tb.Foreground = Brushes.Black;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = tb.Tag?.ToString() ?? tb.Text;
                tb.Foreground = Brushes.Gray;
            }
        }
    }
}