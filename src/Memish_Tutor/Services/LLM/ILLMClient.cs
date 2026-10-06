using Memish_Tutor.Services.Common;

namespace Memish_Tutor.Services.LLM
{
    public interface ILLMClient
    {
        Task<Result<MemeLessonResult?>> AnalyzeMemeAsync(string ImageUrl);
    }
}
