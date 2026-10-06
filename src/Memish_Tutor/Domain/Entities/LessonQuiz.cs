namespace Memish_Tutor.Domain.Entities
{
    public class LessonQuiz
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid LessonId { get; init; }
        
        // JSON array of strings
        public string OptionsJson { get; set; } = string.Empty; 
        
        public int CorrectOptionIndex { get; set; }

        public Lesson Lesson { get; init; } = null!;
    }
}
