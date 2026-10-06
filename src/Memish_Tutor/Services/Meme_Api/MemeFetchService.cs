using Memish_Tutor.Domain.Entities;
using Memish_Tutor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Memish_Tutor.Services.Meme_Api
{
    public class MemeFetchService
    {
        private readonly IMemeService _memeService;
        private readonly AppDBContext _db;

        public MemeFetchService(IMemeService memeService, AppDBContext db)
        {
            _memeService = memeService;
            _db = db;
        }

        public async Task<IResult> FetchAndStoreMeme()
        {
            var meme = await _memeService.GetMeme();
            if (meme != null && !string.IsNullOrEmpty(meme.Url))
            {
                bool existsInMemes = await _db.Memes.AnyAsync(m => m.Url == meme.Url);
                bool existsInLessons = await _db.Lessons.AnyAsync(l => l.MemeImageUrl == meme.Url);
                
                if (!existsInMemes && !existsInLessons)
                {
                    _db.Memes.Add(new RawMeme
                    {
                        Id = Guid.NewGuid(),
                        Subreddit = meme.Subreddit,
                        Title = meme.Title,
                        Url = meme.Url,
                        FetchedAt = DateTime.UtcNow     
                    });
                    await _db.SaveChangesAsync();
                }
            }
            return Results.Ok(new { message = "Meme fetched and stored successfully." });
        }

        public async Task FetchAndStoreBatchAsync(int batchSize)
        {
            var memes = await _memeService.GetMemesBatch(batchSize);
            
            var existingUrls = await _db.Memes.Select(m => m.Url).ToListAsync();
            var existingLessonUrls = await _db.Lessons.Select(l => l.MemeImageUrl).ToListAsync();
            var allUrls = existingUrls.Concat(existingLessonUrls).ToHashSet();
            
            foreach (var meme in memes)
            {
                if (meme != null && !string.IsNullOrEmpty(meme.Url) && !allUrls.Contains(meme.Url))
                {
                    _db.Memes.Add(new RawMeme
                    {
                        Id = Guid.NewGuid(),
                        Subreddit = meme.Subreddit,
                        Title = meme.Title,
                        Url = meme.Url,
                        FetchedAt = DateTime.UtcNow     
                    });
                    allUrls.Add(meme.Url);
                }
            }
            await _db.SaveChangesAsync();
        }
    }
}
