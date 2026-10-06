namespace Memish_Tutor.Services.Telegram.Handlers
{
    public interface ICommandHandler
    {
        bool CanHandle(string? text, string? callbackData);
        Task HandleAsync(long chatId, string? text, string? callbackData, string? username, int messageId);
    }
}
