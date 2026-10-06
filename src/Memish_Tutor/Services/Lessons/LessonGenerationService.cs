using Memish_Tutor.Domain.Entities;
using System.Text.Json;
using Memish_Tutor.Infrastructure.Data;
using Memish_Tutor.Services.LLM;
using Microsoft.EntityFrameworkCore;

namespace Memish_Tutor.Services.Lessons
{
    public class LessonGenerationService
    {
        private readonly ILLMClient _llmClient;
        private readonly AppDBContext appDBContext;

        public LessonGenerationService(ILLMClient llmClient, AppDBContext appDBContext)
        {
            _llmClient = llmClient;
            this.appDBContext = appDBContext;
        }

        public async Task<int> ProcessUnprocessedMemesAsync(int batchSize = 5)
        {
            var unprocessed = await appDBContext.Memes
                .Where(m => !m.IsProcessed)
                .OrderBy(m => m.FetchedAt)
                .Take(batchSize)
                .ToListAsync();

            var processedCount = 0;

            foreach (var meme in unprocessed)
            {
                var result = await _llmClient.AnalyzeMemeAsync(meme.Url);
                meme.IsProcessed = true;

                if (!result.IsSuccess)
                {
                    appDBContext.ProcessingErrors.Add(new ProcessingError
                    {
                        Stage = "llm_call",
                        ErrorMessage = result.Error!,
                        RawContext = result.RawResponse,
                        
                    });
                    continue;
                }

                var lesson = result.Value!;

                var lessonEntity = new Lesson
                {
                    MemeImageUrl = meme.Url,
                    SourceLanguage = result.Value.SourceLanguage,
                    OriginalText = result.Value.OriginalText,
                    HeroPhrase = result.Value.HeroPhrase,
                    Pronunciation = result.Value.Pronunciation,
                    Translation = result.Value.Translation,
                    Explanation = result.Value.Explanation,
                    RetrievalPrompt = result.Value.RetrievalPrompt,
                    UsageExample = result.Value.UsageExample
                };

                if (result.Value.QuizOptions?.Count == 4)
                {
                    lessonEntity.Quiz = new LessonQuiz
                    {
                        OptionsJson = JsonSerializer.Serialize(result.Value.QuizOptions),
                        CorrectOptionIndex = result.Value.QuizCorrectIndex
                    };
                }

                appDBContext.Lessons.Add(lessonEntity);
                processedCount++;
            }

            await appDBContext.SaveChangesAsync();
            return processedCount;
        }
    }
}
