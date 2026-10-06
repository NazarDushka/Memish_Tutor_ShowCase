namespace Memish_Tutor.Services.Meme_Api
{
    public interface IMemeService
    {
        Task<memeJson> GetMeme();
        Task<List<memeJson>> GetMemesBatch(int count);

    }
}
