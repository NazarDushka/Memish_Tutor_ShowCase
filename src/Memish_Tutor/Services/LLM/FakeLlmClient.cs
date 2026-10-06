using Memish_Tutor.Services.Common;

namespace Memish_Tutor.Services.LLM
{
    /// <summary>
    /// Showcase stub simulating Claude Vision response without requiring an Anthropic API key.
    /// Demonstrates Clean Architecture and Dependency Inversion Principle.
    /// </summary>
    public class FakeLlmClient : ILLMClient
    {
        private readonly ILogger<FakeLlmClient> _logger;

        public FakeLlmClient(ILogger<FakeLlmClient> logger)
        {
            _logger = logger;
        }

        public Task<Result<MemeLessonResult?>> AnalyzeMemeAsync(string imageUrl)
        {
            _logger.LogInformation("FakeLlmClient: Generating canned educational lesson for image: {Url}", imageUrl);

            var cannedResult = new MemeLessonResult
            {
                SourceLanguage = "en",
                OriginalText = "One does not simply deploy on Friday afternoon",
                HeroPhrase = "One does not simply",
                Pronunciation = "wʌn dʌz nɒt ˈsɪmpli",
                Translation = "Нельзя просто так взять и...",
                Explanation = "A legendary meme template based on Boromir from Lord of the Rings. Used sarcastically to highlight that a task is far more treacherous than it seems.",
                UsageExample = "— I'm just going to push directly to main without tests.\n— One does not simply push directly to main without code review.",
                RetrievalPrompt = "Boromir Lord of the Rings Friday deploy meme",
                IsSuitable = true,
                Confidence = 0.98,
                QuizOptions = new List<string>
                {
                    "Нельзя просто так взять и...",
                    "Очень легко и быстро...",
                    "В следующий вторник...",
                    "Никогда не говори никогда..."
                },
                QuizCorrectIndex = 0
            };

            return Task.FromResult(Result<MemeLessonResult?>.Success(cannedResult));
        }
    }
}

