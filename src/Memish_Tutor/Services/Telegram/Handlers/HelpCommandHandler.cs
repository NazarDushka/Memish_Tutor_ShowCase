using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Memish_Tutor.Services.Telegram.Handlers
{
    public class HelpCommandHandler : ICommandHandler
    {
        private readonly ITelegramBotClient _bot;

        public HelpCommandHandler(ITelegramBotClient bot)
        {
            _bot = bot;
        }

        public bool CanHandle(string? text, string? callbackData)
        {
            return text?.StartsWith("/help") == true || callbackData == "cmd_help";
        }

        public async Task HandleAsync(long chatId, string? text, string? callbackData, string? username, int messageId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📚 Получить урок", "cmd_lesson"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("❓ Помощь", "cmd_help"),
                    InlineKeyboardButton.WithCallbackData("🛑 Остановить уроки", "cmd_stop"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("☕ Поддержать автора", "cmd_donate")
                }
            });

            await _bot.SendMessage(chatId,
                "📖 *Memish Tutor* — учи английский по мемам!\n\n" +
                "🔹 Каждый день я присылаю урок автоматически\n" +
                "🔹 Можешь запросить ещё один — жми кнопку ниже\n" +
                "🔹 Поддержать проект: /donate\n\n" +
                "Выбери действие:",
                parseMode: global::Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: keyboard);
        }
    }
}
