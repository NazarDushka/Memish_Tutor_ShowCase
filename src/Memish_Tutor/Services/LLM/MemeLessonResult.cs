using System.Text.Json.Serialization;

namespace Memish_Tutor.Services.LLM
{
    public class MemeLessonResult
    {
        [JsonPropertyName("source_language")]
        public string SourceLanguage { get; set; } = string.Empty; // "ru" или "en"

        [JsonPropertyName("original_text")]
        public string OriginalText { get; set; } = string.Empty; // текст, распознанный на картинке

        [JsonPropertyName("hero_phrase")]
        public string HeroPhrase { get; set; } = string.Empty;

        [JsonPropertyName("pronunciation")]
        public string Pronunciation { get; set; } = string.Empty;

        [JsonPropertyName("translation")]
        public string Translation { get; set; } = string.Empty;

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; } = string.Empty;

        [JsonPropertyName("usage_example")]
        public string UsageExample { get; set; } = string.Empty;

        [JsonPropertyName("retrieval_prompt")]
        public string RetrievalPrompt { get; set; } = string.Empty;

        [JsonPropertyName("is_suitable")]
        public bool IsSuitable { get; set; } // грубая само-модерация на этом же вызове

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("quiz_options")]
        public List<string> QuizOptions { get; set; } = new();

        [JsonPropertyName("quiz_correct_index")]
        public int QuizCorrectIndex { get; set; }
    }

}
