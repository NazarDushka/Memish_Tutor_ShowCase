using Memish_Tutor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Memish_Tutor.Services.Telegram.Handlers
{
    public class QuizCallbackHandler
    {
        private readonly AppDBContext _db;
        private readonly ITelegramBotClient _bot;
        private readonly ILogger<QuizCallbackHandler> _logger;

        public QuizCallbackHandler(AppDBContext db, ITelegramBotClient bot, ILogger<QuizCallbackHandler> logger)
        {
            _db = db;
            _bot = bot;
            _logger = logger;
        }

        public async Task HandleAsync(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message == null || string.IsNullOrEmpty(callbackQuery.Data)) return;

            var parts = callbackQuery.Data.Split('_');
            if (parts.Length != 3) return;

            if (!Guid.TryParse(parts[1], out var quizId)) return;
            if (!int.TryParse(parts[2], out var selectedIndex)) return;

            var quiz = await _db.LessonQuizzes.FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null)
            {
                await _bot.AnswerCallbackQuery(callbackQuery.Id, "Квиз не найден 😔", showAlert: true);
                return;
            }

            var options = JsonSerializer.Deserialize<List<string>>(quiz.OptionsJson) ?? new List<string>();
            var correctOption = options.ElementAtOrDefault(quiz.CorrectOptionIndex) ?? "Unknown";

            string newCaption;
            string alertText;
            if (selectedIndex == quiz.CorrectOptionIndex)
            {
                newCaption = $"🎉 <b>Правильно!</b>\n\nФраза переводится как:\n«{correctOption}»";
                alertText = "🎉 Правильно!";
            }
            else
            {
                newCaption = $"❌ <b>Не совсем...</b>\n\nПравильный ответ был:\n«{correctOption}»";
                alertText = "❌ Не совсем! Правильный ответ:\n" + correctOption;
            }

            try
            {
                // Remove the keyboard and update the text to show the result
                await _bot.EditMessageCaption(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    caption: newCaption,
                    parseMode: global::Telegram.Bot.Types.Enums.ParseMode.Html,
                    replyMarkup: null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit quiz message {MessageId}", callbackQuery.Message.MessageId);
            }

            // Always acknowledge the callback query and show the result as a popup!
            await _bot.AnswerCallbackQuery(callbackQuery.Id, alertText, showAlert: true);
        }
    }
}
