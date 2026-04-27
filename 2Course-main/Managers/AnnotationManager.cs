using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using _2course.Models; // ? Важно: ссылка на модели

namespace _2course.Managers
{
    public static class AnnotationManager
    {
        private static readonly string NotesFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LawApp", "annotations.json");

        private static Dictionary<string, LawyerNote> _notes;
        private static readonly object _lock = new object();

        static AnnotationManager() => LoadNotes();

        private static void LoadNotes()
        {
            try
            {
                if (File.Exists(NotesFile))
                {
                    var json = File.ReadAllText(NotesFile);
                    _notes = JsonSerializer.Deserialize<Dictionary<string, LawyerNote>>(json)
                             ?? new Dictionary<string, LawyerNote>();
                }
                else
                {
                    _notes = new Dictionary<string, LawyerNote>();
                }
            }
            catch { _notes = new Dictionary<string, LawyerNote>(); }
        }

        private static void SaveNotes()
        {
            try
            {
                var dir = Path.GetDirectoryName(NotesFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(_notes, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(NotesFile, json);
            }
            catch { }
        }

        public static LawyerNote GetNote(string articleName)
        {
            if (string.IsNullOrEmpty(articleName)) return null;
            lock (_lock)
            {
                _notes.TryGetValue(articleName, out var note);
                return note;
            }
        }

        public static void SaveNote(string articleName, string text, NoteType type)
        {
            if (string.IsNullOrEmpty(articleName)) return;
            lock (_lock)
            {
                if (_notes.ContainsKey(articleName))
                {
                    _notes[articleName].Text = text;
                    _notes[articleName].Type = type;
                    _notes[articleName].UpdatedAt = DateTime.Now;
                }
                else
                {
                    _notes[articleName] = new LawyerNote
                    {
                        Text = text,
                        Type = type,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                }
                SaveNotes();
            }
        }

        public static void DeleteNote(string articleName)
        {
            if (string.IsNullOrEmpty(articleName)) return;
            lock (_lock)
            {
                if (_notes.Remove(articleName))
                    SaveNotes();
            }
        }

        public static bool HasNote(string articleName)
        {
            if (string.IsNullOrEmpty(articleName)) return false;
            lock (_lock) return _notes.ContainsKey(articleName);
        }
    }
}