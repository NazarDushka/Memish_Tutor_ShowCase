namespace Memish_Tutor.Services.Common
{
    /// <summary>
    /// Type of media in a meme: static image or animated GIF.
    /// Determines which Telegram API method to use for delivery.
    /// </summary>
    public enum MemeMediaType
    {
        Image,
        Gif
    }
}
