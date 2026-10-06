using Telegram.Bot;
using Telegram.Bot.Types;
using Memish_Tutor.Services.Telegram.Handlers;
using System.Collections.Concurrent;

namespace Memish_Tutor.Services
{
    public class UpdateHandlerService : IUpdateHandlerService
    {
        private readonly ITelegramBotClient _bot;
        private readonly IEnumerable<ICommandHandler> _handlers;
        private readonly HelpCommandHandler _helpHandler; // Fallback
        private readonly QuizCallbackHandler _quizCallbackHandler;
        private static readonly ConcurrentDictionary<long, string> _userStates = new();
        private static readonly ConcurrentDictionary<long, DateTime> _lastRequestTimes = new();

        public UpdateHandlerService(
            ITelegramBotClient bot,
            IEnumerable<ICommandHandler> handlers,
            HelpCommandHandler helpHandler,
            QuizCallbackHandler quizCallbackHandler)
        {
            _bot = bot;
            _handlers = handlers;
            _helpHandler = helpHandler;
            _quizCallbackHandler = quizCallbackHandler;
        }

        public async Task HandleUpdateAsync(Update update)
        {
            string? text = update.Message?.Text;
            string? callbackData = update.CallbackQuery?.Data;
            long chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id ?? 0;
            string? username = update.Message?.From?.Username ?? update.CallbackQuery?.From.Username;

            int messageId = update.Message?.MessageId ?? update.CallbackQuery?.Message?.MessageId ?? 0;

            if (chatId == 0) return;

            // --- Anti-Spam (Rate Limiting) ---
            // Игнорируем запросы, если с момента последнего прошло меньше 1.5 секунд
            var now = DateTime.UtcNow;
            if (_lastRequestTimes.TryGetValue(chatId, out var lastTime))
            {
                if ((now - lastTime).TotalSeconds < 1.5) 
                {
                    // Если это callback query, отвечаем на него, чтобы убрать часики загрузки у пользователя, но ничего не делаем
                    if (update.CallbackQuery != null)
                    {
                        try { await _bot.AnswerCallbackQuery(update.CallbackQuery.Id); } catch { }
                    }
                    return; 
                }
            }
            _lastRequestTimes[chatId] = now;
            // ---------------------------------

            // Handle user states (like waiting for feedback)
            if (_userStates.ContainsKey(chatId))
            {
                if (text != null && text.StartsWith("/"))
                {
                    // Cancel state if user sends a new command
                    _userStates.TryRemove(chatId, out _);
                }
                else if (text != null)
                {
                    // User sent the feedback text
                    _userStates.TryRemove(chatId, out _);
                    text = "/feedback " + text; // Prepend command so FeedbackCommandHandler catches it
                }
            }

            // Intercept bare /feedback command from menu
            if (text == "/feedback")
            {
                _userStates[chatId] = "WAITING_FEEDBACK";
                await _bot.SendMessage(chatId, "✏️ Напиши свой отзыв, идею или сообщение о баге следующим сообщением. Я передам его разработчику!");
                return;
            }

            // Acknowledge callback queries
            if (update.CallbackQuery != null)
            {
                // Route quiz callbacks before acknowledging because QuizCallbackHandler handles its own answering with alerts if needed
                if (update.CallbackQuery.Data != null && update.CallbackQuery.Data.StartsWith("quiz_"))
                {
                    await _quizCallbackHandler.HandleAsync(update.CallbackQuery);
                    return;
                }

                try
                {
                    await _bot.AnswerCallbackQuery(update.CallbackQuery.Id);
                }
                catch (Exception ex)
                {
                    // Ignore if callback query is too old or invalid
                    Console.WriteLine($"Could not answer callback query: {ex.Message}");
                }
            }

            // Обработка Telegram Stars (Invoices)
            if (update.PreCheckoutQuery != null)
            {
                try
                {
                    await _bot.AnswerPreCheckoutQuery(update.PreCheckoutQuery.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not answer pre-checkout query: {ex.Message}");
                }
                return;
            }

            if (update.Message?.SuccessfulPayment != null)
            {
                var stars = update.Message.SuccessfulPayment.TotalAmount;
                await _bot.SendMessage(chatId, $"💖 Огромное спасибо за поддержку в размере {stars} ⭐️! Это невероятно мотивирует делать бота еще лучше!");
                return;
            }

            // Route to appropriate handler
            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(text, callbackData))
                {
                    await handler.HandleAsync(chatId, text, callbackData, username, messageId);
                    return;
                }
            }

            // Fallback for unknown commands
            if (text != null)
            {
                await _helpHandler.HandleAsync(chatId, null, null, null, messageId);
            }
        }
    }
}
