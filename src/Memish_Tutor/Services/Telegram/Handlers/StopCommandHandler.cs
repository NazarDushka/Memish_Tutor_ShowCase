using Memish_Tutor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Memish_Tutor.Services.Telegram.Handlers
{
    public class StopCommandHandler : ICommandHandler
    {
        private readonly AppDBContext _db;
        private readonly ITelegramBotClient _bot;

        public StopCommandHandler(AppDBContext db, ITelegramBotClient bot)
        {
            _db = db;
            _bot = bot;
        }

        public bool CanHandle(string? text, string? callbackData)
        {
            if (text?.StartsWith("/stop") == true) return true;
            
            var validCallbacks = new[] { "cmd_stop", "confirm_stop", "cancel_stop" };
            return callbackData != null && validCallbacks.Contains(callbackData);
        }

        public async Task HandleAsync(long chatId, string? text, string? callbackData, string? username, int messageId)
        {
            if (text?.StartsWith("/stop") == true || callbackData == "cmd_stop")
            {
                await HandleStopAsync(chatId);
            }
            else if (callbackData == "confirm_stop")
            {
                await ConfirmStopAsync(chatId);
            }
            else if (callbackData == "cancel_stop")
            {
                await _bot.SendMessage(chatId, "Отлично, продолжаем! 🔥");
            }
        }

        private async Task HandleStopAsync(long chatId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Да, остановить", "confirm_stop"),
                InlineKeyboardButton.WithCallbackData("❌ Нет, продолжить", "cancel_stop"),
            });

            await _bot.SendMessage(chatId,
                "Ты уверен, что хочешь остановить ежедневные уроки?\n" +
                "Ты всегда сможешь вернуться через /start",
                replyMarkup: keyboard);
        }

        private async Task ConfirmStopAsync(long chatId)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.TelegramChatId == chatId);

            if (user is null) return;

            user.IsActive = false;
            await _db.SaveChangesAsync();

            await _bot.SendMessage(chatId,
                "Уроки остановлены 😢\n\n" +
                "Когда захочешь вернуться — просто напиши /start");
        }
    }
}
