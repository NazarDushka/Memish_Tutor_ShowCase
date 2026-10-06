using Telegram.Bot.Types;

namespace Memish_Tutor.Services
{
    public interface IUpdateHandlerService
    {
        Task HandleUpdateAsync(Update update);
    }
}
