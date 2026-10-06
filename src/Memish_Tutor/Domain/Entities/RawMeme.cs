namespace Memish_Tutor.Domain.Entities
{
    public class RawMeme
    {
        public Guid Id { get; set; }

        public string Title { get; set; }
        public string Subreddit { get; set; }
        public string Url { get; set; }

        public DateTime FetchedAt { get; set; }
        public bool IsProcessed { get; set; } = false;
    }
}
