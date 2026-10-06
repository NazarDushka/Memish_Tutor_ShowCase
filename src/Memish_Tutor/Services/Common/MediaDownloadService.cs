namespace Memish_Tutor.Services.Common
{
    /// <summary>
    /// Downloads media (images and GIFs) from external URLs with browser-like headers
    /// to bypass bot protection (Reddit, Imgur, etc.) and retry on transient failures.
    /// </summary>
    public class MediaDownloadService : IMediaDownloadService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MediaDownloadService> _logger;

        private const int MaxRetries = 3;

        private static readonly HashSet<string> GifExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".gif"
        };

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tiff"
        };

        public MediaDownloadService(
            IHttpClientFactory httpClientFactory,
            ILogger<MediaDownloadService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<MemoryStream> DownloadAsync(string url)
        {
            var client = _httpClientFactory.CreateClient("ImageDownloader");

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    // Browser-like headers to bypass Reddit/Imgur bot protection
                    request.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
                    request.Headers.Add("Accept",
                        "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
                    request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                    request.Headers.Add("Referer", "https://www.reddit.com/");

                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var memoryStream = new MemoryStream();
                    await response.Content.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    _logger.LogDebug("Downloaded media from {Url} on attempt {Attempt}", url, attempt);
                    return memoryStream;
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    _logger.LogWarning(ex,
                        "Download attempt {Attempt}/{Max} failed for {Url}. Retrying...",
                        attempt, MaxRetries, url);

                    // Exponential backoff: 1s, 2s, 4s
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));
                }
            }

            throw new HttpRequestException(
                $"Failed to download media from {url} after {MaxRetries} attempts.");
        }

        public MemeMediaType DetectMediaType(string url)
        {
            try
            {
                var uri = new Uri(url);
                var ext = Path.GetExtension(uri.AbsolutePath);

                if (GifExtensions.Contains(ext))
                    return MemeMediaType.Gif;

                return MemeMediaType.Image;
            }
            catch
            {
                return MemeMediaType.Image;
            }
        }

        public string ExtractFileName(string url)
        {
            try
            {
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.AbsolutePath);
                if (!string.IsNullOrWhiteSpace(fileName))
                    return fileName;
            }
            catch { }

            // Fallback based on detected type
            return DetectMediaType(url) == MemeMediaType.Gif ? "meme.gif" : "meme.jpg";
        }
    }
}
