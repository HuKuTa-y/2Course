using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using _2course.Models;
using _2course.Managers;

namespace _2course.Controls
{
    public class ArticleCard : Button
    {
        public ArticleFull? Article { get; set; }
        public event RoutedEventHandler? ArticleClicked;
        public string? SourceName { get; set; }
        public event RoutedEventHandler? FavoriteToggled;
        public event RoutedEventHandler? NoteClicked;

        public ArticleCard(ArticleFull article)
        {
            Article = article;
            Tag = article;
            CreateVisuals();
            AttachEvents();
        }

        private void CreateVisuals()
        {
            // Текст статьи
            var textBlock = new TextBlock
            {
                Text = Article?.Название ?? "",
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis, // 🔥 Обрезание с троеточием
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 55, 0),
                Foreground = Brushes.Black,
                FontSize = 13
            };

            var starBtn = CreateStarButton();
            var noteBtn = CreateNoteButton();

            // 🔥 Кнопки в правой колонке
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 5, 0),
                Children = { noteBtn, starBtn }
            };

            // 🔥 Grid с колонками
            var grid = new Grid
            {
                Margin = new Thickness(2),
                ClipToBounds = true
            };

            // 🔥 Исправлено: new GridLength(1, GridUnitType.Star) вместо GridLength.Star
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Текст
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Кнопки

            Grid.SetColumn(textBlock, 0);
            Grid.SetColumn(buttonsPanel, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(buttonsPanel);

            Content = grid;
            Width = 390;
            Height = 50;
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            BorderBrush = Brushes.LightGray;
            BorderThickness = new Thickness(1);
            Background = Brushes.White;
            Cursor = Cursors.Hand;

            MouseEnter += (s, e) => { BorderBrush = Brushes.SteelBlue; starBtn.Opacity = 1; noteBtn.Opacity = 1; };
            MouseLeave += (s, e) => { BorderBrush = Brushes.LightGray; starBtn.Opacity = 0.6; noteBtn.Opacity = 0.6; };
        }

        private Button CreateStarButton()
        {
            var isFav = FavoritesManager.IsFavorite(Article?.Название ?? "");
            var starChar = isFav ? "\u2605" : "\u2606";

            return new Button
            {
                Content = starChar,
                FontFamily = new FontFamily("Segoe UI"),
                Width = 24,
                Height = 24,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = isFav ? Brushes.Gold : Brushes.Gray,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 5, 0),
                Cursor = Cursors.Hand,
                Tag = "STAR",
                ToolTip = "Избранное",
                Opacity = 0.6
            };
        }

        private Button CreateNoteButton()
        {
            var hasNote = AnnotationManager.HasNote(Article?.Название ?? "");
            return new Button
            {
                Content = hasNote ? "📝" : "·",
                Width = 24,
                Height = 24,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = hasNote ? Brushes.SteelBlue : Brushes.LightGray,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 33, 0),
                Cursor = Cursors.Hand,
                Tag = "NOTE",
                ToolTip = hasNote ? "Есть заметка" : "Добавить заметку",
                Opacity = 0.6
            };
        }

        private void AttachEvents()
        {
            if (Content is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    // Проверяем кнопки внутри StackPanel (buttonsPanel)
                    if (child is StackPanel sp)
                    {
                        foreach (var spChild in sp.Children)
                        {
                            if (spChild is Button btn)
                            {
                                if (btn.Tag?.ToString() == "STAR")
                                {
                                    btn.Click += (s, e) =>
                                    {
                                        e.Handled = true;
                                        FavoritesManager.ToggleFavorite(Article?.Название ?? "");
                                        btn.Content = FavoritesManager.IsFavorite(Article?.Название ?? "") ? "\u2605" : "\u2606";
                                        btn.Foreground = FavoritesManager.IsFavorite(Article?.Название ?? "") ? Brushes.Gold : Brushes.Gray;
                                        FavoriteToggled?.Invoke(this, e);
                                    };
                                }
                                else if (btn.Tag?.ToString() == "NOTE")
                                {
                                    btn.Click += (s, e) =>
                                    {
                                        e.Handled = true;
                                        NoteClicked?.Invoke(this, e);
                                    };
                                }
                            }
                        }
                    }
                    else if (child is Button btn)
                    {
                        if (btn.Tag?.ToString() == "STAR" || btn.Tag?.ToString() == "NOTE")
                        {
                            btn.Click += (s, e) => e.Handled = true;
                        }
                    }
                }
            }

            Click += (s, e) =>
            {
                if (e.OriginalSource is Button b && (b.Tag?.ToString() is "STAR" or "NOTE")) return;
                ArticleClicked?.Invoke(this, e);
            };
        }
    }
}