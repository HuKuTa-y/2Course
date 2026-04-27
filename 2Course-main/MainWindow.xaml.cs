    using System.Windows;
    using System.Windows.Media;    // ← Для Brushes, Color, SolidColorBrush и т.д.
    using System.Windows.Input;
    using Microsoft.Win32; // Для SaveFileDialog
    using System.IO;       // Для Path
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

            public MainWindow()
            {
                InitializeComponent();

                if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                {
                    CodesPanel.Children.Add(new TextBlock { Text = "[Дизайн]", FontStyle = FontStyles.Italic, Foreground = Brushes.Gray, Margin = new Thickness(10) });
                    return;
                }

                // Инициализация
                _httpClient.Timeout = System.TimeSpan.FromSeconds(30);
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                CacheManager.Initialize(_httpClient, ApiBaseUrl);

                _lawService = new LawService(_httpClient, ApiBaseUrl);
                _vm = new MainViewModel(_lawService);

                // Горячие клавиши
                KeyDown += MainWindow_KeyDown;

                // Запуск
                _ = InitializeAsync();
            }

            private async System.Threading.Tasks.Task InitializeAsync()
            {
                await _vm.InitializeAsync();
                BuildCodeksButtons();
                BuildLawsButtons();
                UpdateStatus();
            }

            private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
            {
                if (e.Key == System.Windows.Input.Key.F && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
                {
                    e.Handled = true;
                    ShowFavorites();
                }
                else if (e.Key == System.Windows.Input.Key.R && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
                {
                    e.Handled = true;
                    _ = _vm.RefreshDataAsync();
                    ShowStatus("Обновление...");
                }
                else if (e.Key == System.Windows.Input.Key.Escape)
                {
                    e.Handled = true;
                    CancelSearchButton_Click(null, null);
                }
            }

            // ==================== 📝 ОТКРЫТИЕ ОКНА РЕДАКТИРОВАНИЯ ЗАМЕТКИ ====================
            private void OpenNoteEditor(ArticleFull article)
            {
                var currentNote = AnnotationManager.GetNote(article.Название);

                var window = new Window
                {
                    Title = $"Заметка: {article.Название}",
                    Width = 400,
                    Height = 250,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize,
                    Background = Brushes.WhiteSmoke
                };

                var grid = new Grid { Margin = new Thickness(10) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Выбор типа
                var stackTypes = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
                var selectedType = currentNote?.Type ?? NoteType.Info;

                var rbInfo = new RadioButton { Content = "ℹ Инфо", IsChecked = selectedType == NoteType.Info, Margin = new Thickness(0, 0, 10, 0), Foreground = Brushes.SteelBlue, Tag = NoteType.Info };
                var rbWarn = new RadioButton { Content = "⚠ Риск", IsChecked = selectedType == NoteType.Warning, Margin = new Thickness(0, 0, 10, 0), Foreground = Brushes.Orange, Tag = NoteType.Warning };
                var rbDanger = new RadioButton { Content = "❗ Важно", IsChecked = selectedType == NoteType.Danger, Margin = new Thickness(0, 0, 10, 0), Foreground = Brushes.Red, Tag = NoteType.Danger };
                var rbOk = new RadioButton { Content = "✅ Ок", IsChecked = selectedType == NoteType.Success, Margin = new Thickness(0, 0, 10, 0), Foreground = Brushes.Green, Tag = NoteType.Success };

                rbInfo.Checked += (x, y) => selectedType = NoteType.Info;
                rbWarn.Checked += (x, y) => selectedType = NoteType.Warning;
                rbDanger.Checked += (x, y) => selectedType = NoteType.Danger;
                rbOk.Checked += (x, y) => selectedType = NoteType.Success;

                stackTypes.Children.Add(rbInfo);
                stackTypes.Children.Add(rbWarn);
                stackTypes.Children.Add(rbDanger);
                stackTypes.Children.Add(rbOk);
                Grid.SetRow(stackTypes, 0);
                grid.Children.Add(stackTypes);

                // Поле ввода
                var textBox = new TextBox
                {
                    Text = currentNote?.Text ?? "",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 13,
                    Padding = new Thickness(5)
                };
                Grid.SetRow(textBox, 1);
                grid.Children.Add(textBox);

                // Кнопки
                var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                var btnSave = new Button { Content = "💾 Сохранить", Width = 100, Margin = new Thickness(0, 0, 5, 0), IsDefault = true };
                var btnCancel = new Button { Content = "Отмена", Width = 80 };
                var btnDel = new Button { Content = "🗑 Удалить", Width = 90, Margin = new Thickness(0, 0, 5, 0), Foreground = Brushes.Red };

                btnSave.Click += (x, y) =>
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        AnnotationManager.SaveNote(article.Название, textBox.Text, selectedType);
                    }
                    else
                    {
                        AnnotationManager.DeleteNote(article.Название);
                    }
                    window.Close();

                    //  Обновляем визуал кнопки в списке
                    RefreshArticleCardVisuals(article);
                };

                btnCancel.Click += (x, y) => window.Close();

                btnDel.Click += (x, y) =>
                {
                    AnnotationManager.DeleteNote(article.Название);
                    window.Close();
                    RefreshArticleCardVisuals(article);
                };

                btnStack.Children.Add(btnDel);
                btnStack.Children.Add(btnCancel);
                btnStack.Children.Add(btnSave);
                Grid.SetRow(btnStack, 2);
                grid.Children.Add(btnStack);

                window.Content = grid;
                window.ShowDialog();
            }

        // Вспомогательный метод для обновления конкретной карточки без перерисовки всего списка
        // 🔥 Вспомогательный метод для обновления конкретной карточки
        private void RefreshArticleCardVisuals(ArticleFull article)
        {
            for (int i = 0; i < ArticlesPanel.Children.Count; i++)
            {
                if (ArticlesPanel.Children[i] is ArticleCard card && card.Article?.Название == article.Название)
                {
                    // 🔥 Сначала удаляем старую карточку
                    ArticlesPanel.Children.RemoveAt(i);

                    // Создаём новую
                    var newCard = new ArticleCard(article);

                    // Подписываем события
                    newCard.ArticleClicked += async (s, e) => await LoadArticleTextAsync(article);
                    newCard.NoteClicked += (s, e) => OpenNoteEditor(article);
                    newCard.FavoriteToggled += (s, e) => UpdateStatus();

                    // 🔥 Вставляем новую на то же место
                    ArticlesPanel.Children.Insert(i, newCard);
                    break;
                }
            }
        }

        private void BuildCodeksButtons()
            {
                CodesPanel.Children.Clear();
                if (_vm.Codeks == null) return;
                foreach (var item in _vm.Codeks)
                {
                    var btn = new Button { Content = item.Название, Margin = new Thickness(5), Tag = item, ToolTip = $"Номер: {item.Номер}", HorizontalContentAlignment = HorizontalAlignment.Left };
                    btn.Click += (s, e) => CodeOrLawButton_Click(item.Номер);
                    CodesPanel.Children.Add(btn);
                }
            }

            private void BuildLawsButtons()
            {
                LawsPanel.Children.Clear();
                if (_vm.Laws == null) return;
                foreach (var item in _vm.Laws)
                {
                    var btn = new Button { Content = item.Название, Margin = new Thickness(5), Tag = item, ToolTip = $"Номер: {item.Номер}", HorizontalContentAlignment = HorizontalAlignment.Left };
                    btn.Click += (s, e) => CodeOrLawButton_Click(item.Номер);
                    LawsPanel.Children.Add(btn);
                }
            }

            private async void CodeOrLawButton_Click(string sourceNumber)
            {
                await _vm.LoadArticlesAsync(sourceNumber);
                DisplayArticlesWithTreeHeader(_vm.CurrentArticles, sourceNumber);
            }

            private void DisplayArticlesWithTreeHeader(System.Collections.Generic.List<ArticleFull> articles, string sourceNumber)
            {
                ArticlesPanel.Children.Clear();
                string sourceName = UiHelper.ResolveSourceName(sourceNumber, _vm.Codeks, _vm.Laws);

                // Ветка-заголовок
                var connectorHeader = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(5, 0, 5, 12),
                    Padding = new Thickness(14, 10, 14, 10)
                };
                connectorHeader.Child = new TextBlock
                {
                    Text = $"└─ 📚 {sourceName} ──▶",
                    FontSize = 13.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.SteelBlue,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New")
                };
                ArticlesPanel.Children.Add(connectorHeader);
                ArticlesPanel.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(40, 180, 180, 180)), Margin = new Thickness(15, 0, 15, 8) });

                if (articles == null || articles.Count == 0)
                {
                    ArticlesPanel.Children.Add(new TextBlock { Text = "Статьи не найдены", FontStyle = FontStyles.Italic, Foreground = Brushes.Gray, Margin = new Thickness(10) });
                    return;
                }

                foreach (var article in articles)
                {
                    var card = new ArticleCard(article);
                    card.ArticleClicked += async (s, e) => await LoadArticleTextAsync(article);
                    card.NoteClicked += (s, e) => OpenNoteEditor(article);
                    card.FavoriteToggled += (s, e) => UpdateStatus();
                    ArticlesPanel.Children.Add(card);
                }
            }

            private async System.Threading.Tasks.Task LoadArticleTextAsync(ArticleFull article)
            {
                ContentStackPanel.Children.Clear();
                await _vm.LoadArticleTextAsync(article);
                DisplayArticleText(_vm.ArticleText);
            }

        // ==================== 🔥 ОТОБРАЖЕНИЕ ТЕКСТА С РЕДАКТИРОВАНИЕМ И ЭКСПОРТОМ ====================
        private void DisplayArticleText(TextArticle article)
        {
            ContentStackPanel.Children.Clear();

            // Хлебные крошки
            var breadcrumb = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 0, 0, 10) };
            breadcrumb.Children.Add(new TextBlock { Text = "📄 ", FontSize = 16 });
            breadcrumb.Children.Add(new TextBlock
            {
                Text = "Текст статьи",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.SteelBlue,
                VerticalAlignment = VerticalAlignment.Center
            });
            ContentStackPanel.Children.Add(breadcrumb);
            ContentStackPanel.Children.Add(new Border { Height = 1, Background = Brushes.LightGray, Margin = new Thickness(0, 0, 0, 10) });

            // 🔥 Переключатель режима
            var modePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var isOfficialMode = new CheckBox
            {
                Content = "📘 Официальный текст (без правок)",
                IsChecked = false,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            modePanel.Children.Add(isOfficialMode);

            // 🔥 Редактируемое поле
            var editableText = new TextBox
            {
                Text = article?.Контент ?? "Текст не загружен",
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0),
                FontSize = 12,
                Foreground = Brushes.Black,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                Padding = new Thickness(10),
                MinHeight = 300,
                IsReadOnly = false,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            // Логика переключателя
            isOfficialMode.Checked += (s, e) => { editableText.IsReadOnly = true; editableText.Background = Brushes.AliceBlue; };
            isOfficialMode.Unchecked += (s, e) => { editableText.IsReadOnly = false; editableText.Background = Brushes.White; };

            // 🔥 Кнопка экспорта
            var exportBtn = new Button
            {
                Content = "📄 Экспорт в PDF",
                Width = 160,
                Height = 36,
                Margin = new Thickness(0, 15, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = Brushes.SteelBlue,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand,
                IsEnabled = article != null
            };
            exportBtn.MouseEnter += (s, e) => exportBtn.Background = Brushes.DarkSlateBlue;
            exportBtn.MouseLeave += (s, e) => exportBtn.Background = Brushes.SteelBlue;

            exportBtn.Click += (s, e) =>
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF файлы|*.pdf",
                    Title = "Сохранить статью как PDF",
                    FileName = $"{article?.Название?.Replace(" ", "_") ?? "Article"}.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        string textToExport = editableText.Text;
                        bool isOfficial = isOfficialMode.IsChecked == true;

                        // Предупреждение при конфликте режима и правок
                        if (isOfficial && textToExport != article?.Контент)
                        {
                            if (MessageBox.Show("Вы изменили текст, но выбран режим 'Официальный PDF'.\nЭкспортировать как есть или отменить?",
                                "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                                return;
                        }

                        Helpers.PdfExportHelper.ExportToPdf(dialog.FileName, article.Название, textToExport, isOfficial);
                        ShowStatus($"✅ PDF сохранён: {System.IO.Path.GetFileName(dialog.FileName)}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        ShowStatus("Ошибка экспорта", true);
                    }
                }
            };

            // Сборка контейнера
            var contentStack = new StackPanel { Margin = new Thickness(5) };
            contentStack.Children.Add(modePanel);
            contentStack.Children.Add(editableText);
            contentStack.Children.Add(exportBtn);

            var border = new Border
            {
                BorderBrush = Brushes.SteelBlue,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(5),
                Width = 400,
                Padding = new Thickness(0),
                Background = Brushes.White,
                Child = contentStack
            };

            ContentStackPanel.Children.Add(border);
            ContentStackPanel.BringIntoView();
        }

        // ==================== ПОИСК ====================
        private async void FindArticlesButton_Click(object sender, RoutedEventArgs e)
            {
                string searchNumber = ArticleNumberTextBox.Text.Trim();
                if (searchNumber == "Введите номер статьи" || string.IsNullOrEmpty(searchNumber)) return;
                await _vm.SearchByNumberAsync(searchNumber);
                DisplayArticlesWithTreeHeader(_vm.CurrentArticles, "Поиск");
            }

            private async void SearchButton_Click(object sender, RoutedEventArgs e)
            {
                string searchText = SearchTextBox.Text.Trim();
                if (string.IsNullOrEmpty(searchText) || searchText == "Поиск...") return;
                await _vm.SearchByTextAsync(searchText);
                DisplayArticlesWithTreeHeader(_vm.CurrentArticles, "Поиск");
            }

            // ==================== ИЗБРАННОЕ ====================
            private void FavoritesButton_Click(object sender, RoutedEventArgs e) => ShowFavorites();

            private void ShowFavorites()
            {
                ArticlesPanel.Children.Clear();
                var favorites = FavoritesManager.GetFavorites();
                if (favorites.Count == 0)
                {
                    ArticlesPanel.Children.Add(new TextBlock { Text = "⭐ Нет избранных статей", FontStyle = FontStyles.Italic, Foreground = Brushes.Gray, Margin = new Thickness(10), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
                    return;
                }
                foreach (var name in favorites)
                {
                    var article = _lawService.GetLoadedArticlesFull()?.FirstOrDefault(a => a.Название == name)
                               ?? new ArticleFull { Название = name };
                    var card = new ArticleCard(article);
                    card.ArticleClicked += async (s, e) => await LoadArticleTextAsync(article);
                    ArticlesPanel.Children.Add(card);
                }
            }

            // ==================== ЗАМЕТКИ ====================
            private void NotesMenuButton_Click(object sender, RoutedEventArgs e) => ShowAllNotes();

            private void ShowAllNotes()
            {
                ArticlesPanel.Children.Clear();
                var field = typeof(AnnotationManager).GetField("_notes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var allNotes = field?.GetValue(null) as System.Collections.Generic.Dictionary<string, LawyerNote>;

                if (allNotes == null || allNotes.Count == 0)
                {
                    ArticlesPanel.Children.Add(new TextBlock { Text = "📝 Нет заметок", FontStyle = FontStyles.Italic, Foreground = Brushes.Gray, Margin = new Thickness(10), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
                    return;
                }

                foreach (var kvp in allNotes.OrderByDescending(x => x.Value.UpdatedAt))
                {
                    string articleName = kvp.Key;
                    LawyerNote note = kvp.Value;
                    Brush typeColor = UiHelper.GetNoteColor(note.Type);
                    string typeIcon = UiHelper.GetNoteIcon(note.Type);

                    var cardBorder = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Margin = new Thickness(0, 0, 0, 10),
                        Background = Brushes.White,
                        Padding = new Thickness(12)
                    };
                    var mainStack = new StackPanel();

                    // Шапка
                    var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var leftHeader = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                leftHeader.Children.Add(new TextBlock
                {
                    Text = $"{typeIcon} {note.Type}",
                    Foreground = typeColor,
                    FontWeight = FontWeights.SemiBold,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI Emoji, Segoe UI") // ← Явный шрифт с поддержкой эмодзи
                });
                leftHeader.Children.Add(new TextBlock { Text = $"  •  {note.UpdatedAt:dd.MM.yyyy HH:mm}", Foreground = Brushes.Gray, FontSize = 11 });
                    Grid.SetColumn(leftHeader, 0);
                    headerGrid.Children.Add(leftHeader);

                    var deleteBtn = new Button { Content = "✕", Width = 26, Height = 26, FontSize = 11, FontWeight = FontWeights.Bold, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brushes.Gray, Cursor = Cursors.Hand, ToolTip = "Удалить" };
                    deleteBtn.MouseEnter += (s, e) => { deleteBtn.Foreground = Brushes.White; deleteBtn.Background = Brushes.Red; };
                    deleteBtn.MouseLeave += (s, e) => { deleteBtn.Foreground = Brushes.Gray; deleteBtn.Background = Brushes.Transparent; };
                    deleteBtn.Click += (s, e) =>
                    {
                        if (MessageBox.Show("Удалить заметку?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            AnnotationManager.DeleteNote(articleName);
                            ArticlesPanel.Children.Remove(cardBorder);
                            if (ArticlesPanel.Children.Count == 0) ShowAllNotes();
                        }
                    };
                    Grid.SetColumn(deleteBtn, 1);
                    headerGrid.Children.Add(deleteBtn);

                    // Текст
                    var noteText = new TextBlock { Text = note.Text, TextWrapping = TextWrapping.Wrap, FontSize = 13, Margin = new Thickness(0, 0, 0, 8), Cursor = Cursors.Hand, Foreground = Brushes.Black, MinHeight = 40 };
                    noteText.MouseEnter += (s, e) => noteText.Background = Brushes.AliceBlue;
                    noteText.MouseLeave += (s, e) => noteText.Background = Brushes.Transparent;
                    noteText.MouseDown += (s, e) => OpenArticleFromNote(articleName);

                    // Ссылка
                    string shortName = UiHelper.ShortenText(articleName);
                    var linkStack = new StackPanel { Orientation = Orientation.Horizontal };
                    linkStack.Children.Add(new TextBlock { Text = "📄 Перейти к статье: ", Foreground = Brushes.Gray, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
                    var articleLink = new TextBlock { Text = shortName, ToolTip = articleName, Foreground = Brushes.SteelBlue, FontSize = 11, FontWeight = FontWeights.Medium, TextDecorations = TextDecorations.Underline, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center };
                    articleLink.MouseEnter += (s, e) => articleLink.Foreground = Brushes.DarkBlue;
                    articleLink.MouseLeave += (s, e) => articleLink.Foreground = Brushes.SteelBlue;
                    articleLink.MouseDown += (s, e) => OpenArticleFromNote(articleName);
                    linkStack.Children.Add(articleLink);

                    mainStack.Children.Add(headerGrid);
                    mainStack.Children.Add(noteText);
                    mainStack.Children.Add(linkStack);
                    cardBorder.Child = mainStack;
                    ArticlesPanel.Children.Add(cardBorder);
                }
            }

            private async void OpenArticleFromNote(string articleName)
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