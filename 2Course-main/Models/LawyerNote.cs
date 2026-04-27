namespace _2course.Models
{
    public class LawyerNote
    {
        public string Text { get; set; } = "";
        public NoteType Type { get; set; } = NoteType.Info;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public enum NoteType
    {
        Info,      // Синий
        Warning,   // Желтый
        Danger,    // Красный
        Success    // Зеленый
    }
}