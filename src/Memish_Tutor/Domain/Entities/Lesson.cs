using Memish_Tutor.Domain.Entities;
using System.Text.Json.Serialization;

namespace Memish_Tutor.Domain.Entities
{
    public class Lesson
    {
        public Guid Id { get; init; }

        public string MemeImageUrl { get; init; } = string.Empty;
        public string OriginalText { get; init; } = string.Empty;
        public string HeroPhrase { get; set; } = string.Empty;
        public string Pronunciation { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string RetrievalPrompt { get; set; } = string.Empty;

        public string UsageExample { get; set; } = string.Empty;
        public string SourceLanguage { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime? SentAt { get; init; }
        public bool WasSent { get; init; } = false;

        public LessonQuiz? Quiz { get; set; }
    }
}
