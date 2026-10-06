using Memish_Tutor.Domain.Entities;

namespace Memish_Tutor.Services.Lessons.Delivery
{
    public interface ILessonMessageFormatter
    {
        (string Caption, string? FollowUp) BuildLessonMessage(Lesson lesson);
    }

    public class LessonMessageFormatter : ILessonMessageFormatter
    {
        private const int CaptionLimit = 1024;
        private const int MessageLimit = 4096;

        public (string Caption, string? FollowUp) BuildLessonMessage(Lesson lesson)
        {
            var title = $"📸 {lesson.OriginalText}";
            
            var prompt = string.IsNullOrWhiteSpace(lesson.RetrievalPrompt) 
                ? "❓ Как бы ты перевел или использовал это?" 
                : $"❓ {lesson.RetrievalPrompt}";

            var parts = new List<string> { title, prompt };

            if (!string.IsNullOrWhiteSpace(lesson.HeroPhrase))
            {
                var pronunciation = !string.IsNullOrWhiteSpace(lesson.Pronunciation)
                    ? $" ({lesson.Pronunciation})"
                    : "";
                parts.Add($"🎯 {lesson.HeroPhrase}{pronunciation} — {lesson.Translation}");
            }

            if (!string.IsNullOrWhiteSpace(lesson.Explanation))
                parts.Add($"{lesson.Explanation}");

            if (!string.IsNullOrWhiteSpace(lesson.UsageExample))
                parts.Add($"💬 Примеры:\n{lesson.UsageExample}");

            var fullText = string.Join("\n\n", parts);

            // Everything fits in caption — no follow-up needed
            if (fullText.Length <= CaptionLimit)
                return (fullText, null);

            // Caption: just the title and prompt (truncated if extremely long)
            var captionTitle = $"{title}\n\n{prompt}";
            var caption = captionTitle.Length > CaptionLimit
                ? captionTitle[..(CaptionLimit - 4)] + "..."
                : captionTitle;

            // Follow-up: full lesson text without the title and prompt, to avoid duplication
            var explanationParts = parts.Skip(2); // Skip title and prompt
            var followUp = string.Join("\n\n", explanationParts);
            
            if (followUp.Length > MessageLimit)
                followUp = followUp[..(MessageLimit - 4)] + "...";

            return (caption, followUp);
        }
    }
}
