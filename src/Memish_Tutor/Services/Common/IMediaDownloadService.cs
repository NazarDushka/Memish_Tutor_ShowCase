namespace Memish_Tutor.Services.Common
{
    /// <summary>
    /// Downloads media (images and GIFs) from external URLs,
    /// handling bot-protection bypassing with browser-like headers and retry logic.
    /// </summary>
    public interface IMediaDownloadService
    {
        /// <summary>
        /// Downloads media and returns it as a seekable MemoryStream.
        /// The caller is responsible for disposing the stream.
        /// </summary>
        Task<MemoryStream> DownloadAsync(string url);

        /// <summary>
        /// Extracts a file name from the media URL.
        /// Falls back to "meme.jpg" or "meme.gif" based on detected type.
        /// </summary>
        string ExtractFileName(string url);

        /// <summary>
        /// Detects whether the URL points to a static image or an animated GIF.
        /// </summary>
        MemeMediaType DetectMediaType(string url);
    }
}
