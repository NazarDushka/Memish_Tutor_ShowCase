using System.Text.Json.Serialization;

namespace Memish_Tutor.Services.Meme_Api
{
    //Response from Meme_Api
    public class memeJson
    {
        [JsonPropertyName("subreddit")]
        public string Subreddit { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

    }

    public class MemeBatchResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("memes")]
        public List<memeJson> Memes { get; set; } = new List<memeJson>();
    }
}
