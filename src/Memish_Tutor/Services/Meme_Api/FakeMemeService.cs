using Memish_Tutor.Services.Meme_Api;

namespace Memish_Tutor.Services.Meme_Api
{
    public class FakeMemeService : IMemeService
    {
        public Task<memeJson> GetMeme()
        {
            return Task.FromResult(new memeJson
            {
                Url = "https://example.com/fake-meme.jpg",
                Title = "Fake Meme",
                Subreddit = "ProgrammerHumor"
            });
        }

        public Task<List<memeJson>> GetMemesBatch(int count)
        {
            var list = new List<memeJson>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new memeJson
                {
                    Url = $"https://example.com/fake-meme-{i}.jpg",
                    Title = $"Fake Meme {i}",
                    Subreddit = "ProgrammerHumor"
                });
            }
            return Task.FromResult(list);
        }
    }
}

