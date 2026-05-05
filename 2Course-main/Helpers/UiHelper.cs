using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;      // 🔥 ДОБАВЛЕНО: для Cursors.Hand, Cursors.IBeam и т.д.
using System.Windows.Media;
using Microsoft.Win32;
using _2course.Services;  // 🔥 ДОБАВЛЕНО: для ILawService
using _2course.Controls;
using _2course.Managers;
using _2course.Models;
using _2course.ViewModels;

namespace _2course.Helpers
{
    public static class UiHelper
    {
        // ==================== 🎨 ЦВЕТА И ИКОНКИ ====================
        public static Brush GetNoteColor(NoteType type) => type switch
        {
            NoteType.Danger => Brushes.Red,
            NoteType.Warning => Brushes.Orange,
            NoteType.Success => Brushes.Green,
            _ => Brushes.SteelBlue
        };

        public static string GetNoteIcon(NoteType type) => type switch
        {
            NoteType.Danger => "\u2757",
            NoteType.Warning => "\u26A0",
            NoteType.Success => "\u2713",
            _ => "\u2139"
        };

        public static string ResolveSourceName(string sourceNumber, List<Codek>? codeks, List<Law>? laws)
        {
            if (string.IsNullOrEmpty(sourceNumber)) return "📁 Прочее";
            var codek = codeks?.FirstOrDefault(c => c.Номер == sourceNumber);
            if (codek != null) return codek.Название;
            var law = laws?.FirstOrDefault(l => l.Номер == sourceNumber);
            if (law != null) return law.Название;
            return sourceNumber;
        }

        public static string ShortenText(string text, int maxLength = 55)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        // ==================== 📄 ОТОБРАЖЕНИЕ ТЕКСТА СТАТЬИ ====================
        public static void DisplayArticleTextInPanel(StackPanel contentPanel, TextArticle? article)
        {
            contentPanel.Children.Clear();

            var header = new TextBlock
            {
                Text = "📄 Текст статьи",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.SteelBlue,
                Margin = new Thickness(5, 0, 0, 10)
            };
            contentPanel.Children.Add(header);

            string originalText = article?.Контент ?? "Текст не загружен";
            var contentContainer = new Grid { Width = 400, HorizontalAlignment = HorizontalAlignment.Left };

            var readOnlyText = new TextBox
            {
                Text = originalText,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                FontSize = 13,
                Foreground = Brushes.Black,
                FontFamily = new FontFamily("Segoe UI"),
                Cursor = Cursors.IBeam,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MinHeight = 400,
                MaxHeight = 500
            };

            var editableText = new TextBox
            {
                Text = originalText,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0),
                FontSize = 13,
                Foreground = Brushes.Black,
                Background = Brushes.AliceBlue,
                BorderBrush = Brushes.SteelBlue,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                MinHeight = 400,
                MaxHeight = 500,
                Width = 400,
                FontFamily = new FontFamily("Segoe UI"),
                Visibility = Visibility.Collapsed
            };

            contentContainer.Children.Add(readOnlyText);
            contentContainer.Children.Add(editableText);
            contentPanel.Children.Add(contentContainer);

            var controlPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 10, 5, 5) };
            var editBtn = new Button { Content = "✏️ Редактировать", Width = 140, Height = 36, Margin = new Thickness(0, 0, 10, 0), Background = Brushes.WhiteSmoke, Foreground = Brushes.Black, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Cursor = Cursors.Hand };
            var savePdfBtn = new Button { Content = "✅ Сохранить в PDF", Width = 160, Height = 36, Background = Brushes.Green, Foreground = Brushes.White, FontWeight = FontWeights.Bold, Cursor = Cursors.Hand, Visibility = Visibility.Collapsed };
            savePdfBtn.MouseEnter += (s, e) => savePdfBtn.Background = Brushes.DarkGreen;
            savePdfBtn.MouseLeave += (s, e) => savePdfBtn.Background = Brushes.Green;

            controlPanel.Children.Add(editBtn);
            controlPanel.Children.Add(savePdfBtn);
            contentPanel.Children.Add(controlPanel);

            editBtn.Click += (s, e) =>
            {
                readOnlyText.Visibility = Visibility.Collapsed;
                editableText.Visibility = Visibility.Visible;
                editableText.Focus();
                editBtn.Visibility = Visibility.Collapsed;
                savePdfBtn.Visibility = Visibility.Visible;
            };

            savePdfBtn.Click += (s, e) =>
            {
                var dialog = new SaveFileDialog { Filter = "PDF файлы|*.pdf", Title = "Сохранить статью", FileName = $"{article?.Название?.Replace(" ", "_") ?? "Article"}_edit.pdf" };
                if (dialog.ShowDialog() == true)
                {
                    try { PdfExportHelper.ExportToPdf(dialog.FileName, article.Название, editableText.Text); }
                    catch (Exception ex) { MessageBox.Show($"Ошибка PDF: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            };
            contentPanel.BringIntoView();
        }

        // ==================== 📋 СПИСОК СТАТЕЙ С ЗАГОЛОВКОМ ====================
        public static void DisplayArticlesWithTreeHeader(
            Panel articlesPanel,
            List<ArticleFull>? articles,
            string sourceNumber,
            List<Codek>? codeks,
            List<Law>? laws,
            Action<ArticleFull> onArticleClick,
            Action<ArticleFull> onNoteClick,
            Action onFavoriteToggle,
            Func<string, string> getSourceName)
        {
            articlesPanel.Children.Clear();
            string sourceName = getSourceName(sourceNumber);

            var connectorHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(200, 220, 245)),
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
                FontFamily = new FontFamily("Consolas, Courier New")
            };
            articlesPanel.Children.Add(connectorHeader);
            articlesPanel.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(40, 180, 180, 180)), Margin = new Thickness(15, 0, 15, 8) });

            if (articles == null || articles.Count == 0)
            {
                articlesPanel.Children.Add(new TextBlock { Text = "Статьи не найдены", FontStyle = FontStyles.Italic, Foreground = Brushes.Gray, Margin = new Thickness(10) });
                return;
            }

            foreach (var article in articles)
            {
                var card = new ArticleCard(article);
                card.ArticleClicked += (s, e) => onArticleClick(article);
                card.NoteClicked += (s, e) => onNoteClick(article);
                card.FavoriteToggled += (s, e) => onFavoriteToggle();
                articlesPanel.Children.Add(card);
            }
        }

        // ==================== ⭐ ИЗБРАННОЕ ====================
        public static void ShowFavoritesList(
            Panel articlesPanel,
            ILawService lawService,
            Action<ArticleFull> onArticleClick,
            Action updateStatus)
        {
            articlesPanel.Children.Clear();
            var favorites = FavoritesManager.GetFavorites();
            if (favorites.Count == 0)
            {
                articlesPanel.Children.Add(new TextBlock { Text = "⭐ Нет избранных статей", FontStyle = FontStyles.Italic, Foreground = Brushes.Gray, Margin = new Thickness(10), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
                return;
            }
            foreach (var name in favorites)
            {
                var article = lawService.GetLoadedArticlesFull()?.FirstOrDefault(a => a.Название == name) ?? new ArticleFull { Название = name };
                var card = new ArticleCard(article);
                card.ArticleClicked += (s, e) => onArticleClick(article);
                articlesPanel.Children.Add(card);
            }
            updateStatus();
        }

        // ==================== 📝 СПИСОК ЗАМЕТОК ====================
        public static void ShowAllNotesList(
            Panel articlesPanel,
            Action<string> onArticleClick,
            Action<string, LawyerNote> onEditClick)
        {
            articlesPanel.Children.Clear();
            var field = typeof(AnnotationManager).GetField("_notes", BindingFlags.NonPublic | BindingFlags.Static);
            var allNotes = field?.GetValue(null) as Dictionary<string, LawyerNote>;

            if (allNotes == null || allNotes.Count == 0)
            {
                articlesPanel.Children.Add(new TextBlock { Text = "📝 Нет заметок", FontStyle = FontStyles.Italic, Foreground = Brushes.Gray, Margin = new Thickness(10), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
                return;
            }

            foreach (var kvp in allNotes.OrderByDescending(x => x.Value.UpdatedAt))
            {
                string articleName = kvp.Key;
                LawyerNote note = kvp.Value;
                Brush typeColor = GetNoteColor(note.Type);
                string typeIcon = GetNoteIcon(note.Type);

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
                var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var leftHeader = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                leftHeader.Children.Add(new TextBlock { Text = $"{typeIcon} {note.Type}", Foreground = typeColor, FontWeight = FontWeights.SemiBold, FontFamily = new FontFamily("Segoe UI Emoji, Segoe UI") });
                leftHeader.Children.Add(new TextBlock { Text = $"  •  {note.UpdatedAt:dd.MM.yyyy HH:mm}", Foreground = Brushes.Gray, FontSize = 11 });
                Grid.SetColumn(leftHeader, 0);
                headerGrid.Children.Add(leftHeader);

                var rightButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                var editBtn = new Button { Content = "✏️", Width = 26, Height = 26, FontSize = 11, FontWeight = FontWeights.Bold, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brushes.Gray, Cursor = Cursors.Hand, ToolTip = "Редактировать заметку", Margin = new Thickness(0, 0, 5, 0) };
                editBtn.MouseEnter += (s, e) => { editBtn.Foreground = Brushes.SteelBlue; editBtn.Background = Brushes.AliceBlue; };
                editBtn.MouseLeave += (s, e) => { editBtn.Foreground = Brushes.Gray; editBtn.Background = Brushes.Transparent; };
                editBtn.Click += (s, e) => onEditClick(articleName, note);

                var deleteBtn = new Button { Content = "✕", Width = 26, Height = 26, FontSize = 11, FontWeight = FontWeights.Bold, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brushes.Gray, Cursor = Cursors.Hand, ToolTip = "Удалить заметку" };
                deleteBtn.MouseEnter += (s, e) => { deleteBtn.Foreground = Brushes.White; deleteBtn.Background = Brushes.Red; };
                deleteBtn.MouseLeave += (s, e) => { deleteBtn.Foreground = Brushes.Gray; deleteBtn.Background = Brushes.Transparent; };
                deleteBtn.Click += (s, e) =>
                {
                    if (MessageBox.Show("Удалить заметку?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        AnnotationManager.DeleteNote(articleName);
                        articlesPanel.Children.Remove(cardBorder);
                        if (articlesPanel.Children.Count == 0) ShowAllNotesList(articlesPanel, onArticleClick, onEditClick);
                    }
                };

                rightButtons.Children.Add(editBtn); rightButtons.Children.Add(deleteBtn);
                Grid.SetColumn(rightButtons, 1); headerGrid.Children.Add(rightButtons);

                var noteText = new TextBlock { Text = note.Text, TextWrapping = TextWrapping.Wrap, FontSize = 13, Margin = new Thickness(0, 0, 0, 8), Cursor = Cursors.Hand, Foreground = Brushes.Black, MinHeight = 40 };
                noteText.MouseEnter += (s, e) => noteText.Background = Brushes.AliceBlue;
                noteText.MouseLeave += (s, e) => noteText.Background = Brushes.Transparent;
                noteText.MouseDown += (s, e) => onArticleClick(articleName);

                string shortName = ShortenText(articleName);
                var linkStack = new StackPanel { Orientation = Orientation.Horizontal };
                linkStack.Children.Add(new TextBlock { Text = "📄 Перейти к статье: ", Foreground = Brushes.Gray, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
                var articleLink = new TextBlock { Text = shortName, ToolTip = articleName, Foreground = Brushes.SteelBlue, FontSize = 11, FontWeight = FontWeights.Medium, TextDecorations = TextDecorations.Underline, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center };
                articleLink.MouseEnter += (s, e) => articleLink.Foreground = Brushes.DarkBlue;
                articleLink.MouseLeave += (s, e) => articleLink.Foreground = Brushes.SteelBlue;
                articleLink.MouseDown += (s, e) => onArticleClick(articleName);
                linkStack.Children.Add(articleLink);

                mainStack.Children.Add(headerGrid); mainStack.Children.Add(noteText); mainStack.Children.Add(linkStack);
                cardBorder.Child = mainStack; articlesPanel.Children.Add(cardBorder);
            }
        }

        // ==================== 📝 ОКНО РЕДАКТИРОВАНИЯ ЗАМЕТКИ ====================
        public static void OpenNoteEditorWindow(Window owner, ArticleFull article, Action? onSaved = null)
        {
            var currentNote = AnnotationManager.GetNote(article.Название);
            var window = new Window
            {
                Title = $"Заметка: {article.Название}",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.WhiteSmoke
            };

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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

            stackTypes.Children.Add(rbInfo); stackTypes.Children.Add(rbWarn);
            stackTypes.Children.Add(rbDanger); stackTypes.Children.Add(rbOk);
            Grid.SetRow(stackTypes, 0); grid.Children.Add(stackTypes);

            var textBox = new TextBox { Text = currentNote?.Text ?? "", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new FontFamily("Consolas"), FontSize = 13, Padding = new Thickness(5) };
            Grid.SetRow(textBox, 1); grid.Children.Add(textBox);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnSave = new Button { Content = "💾 Сохранить", Width = 100, Margin = new Thickness(0, 0, 5, 0), IsDefault = true };
            var btnCancel = new Button { Content = "Отмена", Width = 80 };
            var btnDel = new Button { Content = "🗑 Удалить", Width = 90, Margin = new Thickness(0, 0, 5, 0), Foreground = Brushes.Red };

            btnSave.Click += (x, y) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text)) AnnotationManager.SaveNote(article.Название, textBox.Text, selectedType);
                else AnnotationManager.DeleteNote(article.Название);
                window.Close();
                onSaved?.Invoke();
            };
            btnCancel.Click += (x, y) => window.Close();
            btnDel.Click += (x, y) => { AnnotationManager.DeleteNote(article.Название); window.Close(); onSaved?.Invoke(); };

            btnStack.Children.Add(btnDel); btnStack.Children.Add(btnCancel); btnStack.Children.Add(btnSave);
            Grid.SetRow(btnStack, 2); grid.Children.Add(btnStack);
            window.Content = grid;
            window.ShowDialog();
        }

        public static void OpenNoteEditorInline(Window owner, string articleName, LawyerNote currentNote, Action onSaved)
        {
            var window = new Window
            {
                Title = $"Редактирование: {articleName}",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.WhiteSmoke
            };

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var stackTypes = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var selectedType = currentNote.Type;

            var rbInfo = new RadioButton { Content = "ℹ Инфо", IsChecked = selectedType == NoteType.Info, Margin = new Thickness(0, 0, 10, 0), Foreground = Brushes.SteelBlue, Tag = NoteType.Info };
            var rbWarn = new RadioButton { Content = "⚠ Риск", IsChecked = selectedType == NoteType.Warning, Margin = new Thickness(0, 0, 10, 0), Foreground = Brushes.Orange, Tag = NoteType.Warning };
            var rbDanger = new RadioButton { Content = "❗ Важно", IsChecked = selectedType == NoteType.Danger, Margin = new Thickness(0, 0, 10, 0), Foreground = Brushes.Red, Tag = NoteType.Danger };
            var rbOk = new RadioButton { Content = "✅ Ок", IsChecked = selectedType == NoteType.Success, Margin = new Thickness(0, 0, 10, 0), Foreground = Brushes.Green, Tag = NoteType.Success };

            rbInfo.Checked += (x, y) => selectedType = NoteType.Info;
            rbWarn.Checked += (x, y) => selectedType = NoteType.Warning;
            rbDanger.Checked += (x, y) => selectedType = NoteType.Danger;
            rbOk.Checked += (x, y) => selectedType = NoteType.Success;

            stackTypes.Children.Add(rbInfo); stackTypes.Children.Add(rbWarn);
            stackTypes.Children.Add(rbDanger); stackTypes.Children.Add(rbOk);
            Grid.SetRow(stackTypes, 0); grid.Children.Add(stackTypes);

            var textBox = new TextBox { Text = currentNote.Text, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new FontFamily("Consolas"), FontSize = 13, Padding = new Thickness(5) };
            Grid.SetRow(textBox, 1); grid.Children.Add(textBox);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnSave = new Button { Content = "💾 Сохранить", Width = 100, Margin = new Thickness(0, 0, 5, 0), IsDefault = true };
            var btnCancel = new Button { Content = "Отмена", Width = 80 };

            btnSave.Click += (x, y) => { if (!string.IsNullOrWhiteSpace(textBox.Text)) { AnnotationManager.SaveNote(articleName, textBox.Text, selectedType); onSaved(); } window.Close(); };
            btnCancel.Click += (x, y) => window.Close();

            btnStack.Children.Add(btnCancel); btnStack.Children.Add(btnSave);
            Grid.SetRow(btnStack, 2); grid.Children.Add(btnStack);
            window.Content = grid;
            window.ShowDialog();
        }

        // ==================== 🔘 КНОПКИ КОДЕКСОВ/ЗАКОНОВ ====================
        public static void BuildCodeksButtons(Panel panel, List<Codek>? codeks, Action<string> onClick)
        {
            panel.Children.Clear();
            if (codeks == null) return;
            foreach (var item in codeks)
            {
                var btn = new Button
                {
                    Content = item.Название,
                    Margin = new Thickness(5),
                    Tag = item,
                    ToolTip = $"Номер: {item.Номер}",
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Background = Brushes.WhiteSmoke,
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Normal,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1)
                };
                btn.Click += (s, e) => onClick(item.Номер);
                panel.Children.Add(btn);
            }
        }

        public static void BuildLawsButtons(Panel panel, List<Law>? laws, Action<string> onClick)
        {
            panel.Children.Clear();
            if (laws == null) return;
            foreach (var item in laws)
            {
                var btn = new Button
                {
                    Content = item.Название,
                    Margin = new Thickness(5),
                    Tag = item,
                    ToolTip = $"Номер: {item.Номер}",
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Background = Brushes.WhiteSmoke,
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Normal,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1)
                };
                btn.Click += (s, e) => onClick(item.Номер);
                panel.Children.Add(btn);
            }
        }
    }
}