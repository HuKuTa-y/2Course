using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Input;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using _2course.Services;

namespace _2course.Controls
{
    public class AiChatControl : UserControl
    {
        private readonly IYandexGptService? _gptService;
        private readonly ScrollViewer _scroll;
        private readonly StackPanel _messagesPanel;
        private readonly TextBox _inputBox;
        private readonly Button _sendBtn;
        private readonly Button _uploadBtn;
        private readonly bool _useMockMode;
        private bool _isAnalyzing;

        // 🔥 ПОЛНАЯ ИСТОРИЯ ДИАЛОГА
        private readonly List<ChatMessage> _chatHistory = new();

        public AiChatControl(IYandexGptService? gptService = null, bool useMockMode = false)
        {
            _gptService = gptService;
            _useMockMode = useMockMode || gptService == null;

            _scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 0, 0, 10) };
            _messagesPanel = new StackPanel();
            _scroll.Content = _messagesPanel;

            _inputBox = new TextBox { Text = "Вставь текст или задай вопрос...", Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(5, 0, 5, 0) };
            _inputBox.GotFocus += (s, e) => { if (_inputBox.Tag == null) { _inputBox.Tag = _inputBox.Text; _inputBox.Text = ""; _inputBox.Foreground = Brushes.Black; } };
            _inputBox.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_inputBox.Text)) { _inputBox.Text = _inputBox.Tag?.ToString() ?? ""; _inputBox.Foreground = Brushes.Gray; } };
            _inputBox.KeyDown += (s, e) => { if (e.Key == Key.Enter && !_isAnalyzing) { e.Handled = true; _ = SendMessageAsync(); } };

            _sendBtn = new Button { Content = "➤", Width = 40, Background = Brushes.SteelBlue, Foreground = Brushes.White, Margin = new Thickness(5, 0, 0, 0) };
            _sendBtn.Click += async (s, e) => await SendMessageAsync();

            _uploadBtn = new Button { Content = "📎", Width = 40, ToolTip = "Загрузить файл", Margin = new Thickness(0, 0, 5, 0) };
            _uploadBtn.Click += async (s, e) => await UploadDocumentAsync();

            CreateUi();
            AddMessage("🤖 Привет! Я помню весь наш диалог. Скидывай законы, задавай вопросы — буду отвечать с учётом контекста!", false);
        }

        private void CreateUi()
        {
            var root = new Grid { Margin = new Thickness(10) };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(_scroll, 0); root.Children.Add(_scroll);
            var inputGrid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
            Grid.SetColumn(_uploadBtn, 0); Grid.SetColumn(_inputBox, 1); Grid.SetColumn(_sendBtn, 2);
            inputGrid.Children.Add(_uploadBtn); inputGrid.Children.Add(_inputBox); inputGrid.Children.Add(_sendBtn);
            Grid.SetRow(inputGrid, 1); root.Children.Add(inputGrid);
            Content = root;
        }

        private void AddMessage(string text, bool isUser)
        {
            var tb = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 13, Foreground = isUser ? Brushes.White : Brushes.Black };
            var bubble = new Border { Background = isUser ? Brushes.SteelBlue : Brushes.WhiteSmoke, CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(isUser ? 40 : 0, 4, isUser ? 0 : 40, 4), HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left, MaxWidth = 400, Child = tb };
            _messagesPanel.Children.Add(bubble);
            _scroll.ScrollToEnd();
        }

        private Border CreateHighlightedText(string text)
        {
            var card = new Border { Background = Brushes.White, BorderBrush = Brushes.SteelBlue, BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(10), Padding = new Thickness(15), Margin = new Thickness(0, 4, 40, 10), HorizontalAlignment = HorizontalAlignment.Left, MaxWidth = 450, Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.2 } };
            var tb = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 13, Foreground = Brushes.Black, LineHeight = 20 };
            var pattern = @"(?i)(ст\.\s*\d+[а-яё\.]*|статья\s*\d+[а-яё\.]*|\bфз\b|\bук\s*рф\b|\bгк\s*рф\b|\bнк\s*рф\b|\bтк\s*рф\b|\bкодекс\b|\bконституция\b)";
            var matches = Regex.Matches(text ?? "", pattern);
            int lastIndex = 0;
            foreach (Match m in matches) { if (m.Index > lastIndex) tb.Inlines.Add(new Run(text.Substring(lastIndex, m.Index - lastIndex))); tb.Inlines.Add(new Run(m.Value) { Background = Brushes.Yellow, FontWeight = FontWeights.SemiBold }); lastIndex = m.Index + m.Length; }
            if (lastIndex < (text?.Length ?? 0)) tb.Inlines.Add(new Run(text.Substring(lastIndex)));
            card.Child = tb; return card;
        }

        private async Task UploadDocumentAsync() { if (_isAnalyzing) return; var dialog = new OpenFileDialog { Filter = "Текстовые файлы|*.txt;*.doc;*.docx|Все файлы|*.*" }; if (dialog.ShowDialog() != true) return; string fileName = Path.GetFileName(dialog.FileName); AddMessage($"📎 Загружен: {fileName}", true); _chatHistory.Add(new ChatMessage { Role = "user", Text = $"Пользователь загрузил файл: {fileName}" }); await AnalyzeFileAsync(dialog.FileName); }

        private async Task SendMessageAsync()
        {
            if (_isAnalyzing || string.IsNullOrWhiteSpace(_inputBox.Text) || _inputBox.Text == _inputBox.Tag?.ToString()) return;

            string userText = _inputBox.Text.Trim();
            _inputBox.Text = "";
            AddMessage(userText, true);

            // 🔥 Сохраняем сообщение пользователя в историю
            _chatHistory.Add(new ChatMessage { Role = "user", Text = userText });

            AddMessage("⏳ Думаю...", false);
            _isAnalyzing = true;

            YandexGptAnalysisResult result;

            if (_useMockMode || _gptService == null)
            {
                await Task.Delay(1000);
                result = new YandexGptAnalysisResult { Success = true, Text = "Тестовый режим. Я бы ответил на основе всей истории диалога. Подключи токены Яндекс GPT для реальной работы." };
            }
            else
            {
                // 🔥 ОТПРАВЛЯЕМ ВСЮ ИСТОРИЮ В АПИ
                result = await _gptService.ChatWithHistoryAsync(_chatHistory, userText);
            }

            // Удаляем "Думаю..."
            if (_messagesPanel.Children.Count > 0 && _messagesPanel.Children[_messagesPanel.Children.Count - 1] is Border last && last.Child is TextBlock lt && lt.Text == "⏳ Думаю...")
                _messagesPanel.Children.RemoveAt(_messagesPanel.Children.Count - 1);

            if (!result.Success)
                AddMessage($"❌ Ошибка: {result.Error}", false);
            else
            {
                _messagesPanel.Children.Add(CreateHighlightedText(result.Text));
                // 🔥 Сохраняем ответ бота в историю
                _chatHistory.Add(new ChatMessage { Role = "assistant", Text = result.Text });
            }

            _isAnalyzing = false;
            _scroll.ScrollToEnd();
        }

        private async Task AnalyzeFileAsync(string filePath)
        {
            AddMessage("⏳ Читаю файл...", false);
            _isAnalyzing = true;
            try
            {
                string text = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

                YandexGptAnalysisResult result;
                if (_useMockMode || _gptService == null)
                {
                    await Task.Delay(1200);
                    result = new YandexGptAnalysisResult { Success = true, Text = "Файл прочитан (" + text.Length + " симв.). В реальном режиме я бы проанализировал этот текст и запомнил его в истории диалога." };
                }
                else
                {
                    result = await _gptService.AnalyzeLegalDocumentAsync(text);
                }

                if (_messagesPanel.Children.Count > 0 && _messagesPanel.Children[_messagesPanel.Children.Count - 1] is Border last && last.Child is TextBlock lt && lt.Text == "⏳ Читаю файл...")
                    _messagesPanel.Children.RemoveAt(_messagesPanel.Children.Count - 1);

                if (!result.Success)
                    AddMessage($"❌ Ошибка: {result.Error}", false);
                else
                {
                    _messagesPanel.Children.Add(CreateHighlightedText(result.Text));
                    // 🔥 Сохраняем анализ в историю
                    _chatHistory.Add(new ChatMessage { Role = "assistant", Text = "Анализ документа: " + result.Text });
                }
            }
            catch (Exception ex)
            {
                AddMessage($"❌ Ошибка: {ex.Message}", false);
            }
            _isAnalyzing = false;
            _scroll.ScrollToEnd();
        }
    }
}