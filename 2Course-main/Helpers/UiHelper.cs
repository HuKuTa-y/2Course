using System.Windows.Media;
using _2course.Models;

namespace _2course.Helpers
{
    public static class UiHelper
    {
        public static Brush GetNoteColor(NoteType type) => type switch
        {
            NoteType.Danger => Brushes.Red,
            NoteType.Warning => Brushes.Orange,
            NoteType.Success => Brushes.Green,
            _ => Brushes.SteelBlue
        };

        // ?? Используем надёжные символы + явные коды
        public static string GetNoteIcon(NoteType type) => type switch
        {
            NoteType.Danger => "\u2757",  // ?
            NoteType.Warning => "\u26A0", // ?
            NoteType.Success => "\u2713", // ?
            _ => "\u2139"                 // ?
        };

        public static string ResolveSourceName(string sourceNumber, System.Collections.Generic.List<Codek> codeks, System.Collections.Generic.List<Law> laws)
        {
            if (string.IsNullOrEmpty(sourceNumber)) return "?? Прочее";
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
    }
}