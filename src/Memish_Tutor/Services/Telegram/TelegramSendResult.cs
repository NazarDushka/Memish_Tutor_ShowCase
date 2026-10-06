namespace Memish_Tutor.Services.Telegram
{
    public enum TelegramErrorType
    {
        None,
        UserBlockedBot,
        UserDeactivated,
        ChatNotFound,
        Unknown
    }

    public sealed record TelegramSendResult
    {
        public bool Success { get; init; }
        public TelegramErrorType ErrorType { get; init; }
        public string? ErrorMessage { get; init; }
        public string? FileId { get; init; }

        public static TelegramSendResult Ok(string? fileId = null) => new()
        {
            Success = true,
            ErrorType = TelegramErrorType.None,
            FileId = fileId
        };

        public static TelegramSendResult Fail(TelegramErrorType errorType, string message) => new()
        {
            Success = false,
            ErrorType = errorType,
            ErrorMessage = message
        };
    }
}
