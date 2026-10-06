using Memish_Tutor.Domain.Entities;
using Memish_Tutor.Infrastructure.Data;
using Memish_Tutor.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Memish_Tutor.Services.Lessons.Delivery
{
    public interface IOnDemandLessonDeliveryService
    {
        Task DeliverLessonToUserAsync(long chatId);
    }

    public class OnDemandLessonDeliveryService : IOnDemandLessonDeliveryService
    {
        private readonly AppDBContext _db;
        private readonly ITelegramBotClient _bot;
        private readonly IMediaDownloadService _mediaDownloader;
        private readonly ITelegramMediaSender _mediaSender;
        private readonly ILessonMessageFormatter _messageFormatter;
        private readonly ILogger<OnDemandLessonDeliveryService> _logger;
        private readonly IQuizDeliveryService _quizDelivery;

        private const int MaxLessonsPerDay = 3;

        public OnDemandLessonDeliveryService(
            AppDBContext db,
            ITelegramBotClient bot,
            IMediaDownloadService mediaDownloader,
            ITelegramMediaSender mediaSender,
            ILessonMessageFormatter messageFormatter,
            ILogger<OnDemandLessonDeliveryService> logger,
            IQuizDeliveryService quizDelivery)
        {
            _db = db;
            _bot = bot;
            _mediaDownloader = mediaDownloader;
            _mediaSender = mediaSender;
            _messageFormatter = messageFormatter;
            _logger = logger;
            _quizDelivery = quizDelivery;
        }

        public async Task DeliverLessonToUserAsync(long chatId)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.TelegramChatId == chatId);

            if (user is null)
            {
                await _bot.SendMessage(chatId,
                    "Сначала зарегистрируйся с помощью /start 🙂");
                return;
            }

            // Check daily limit
            var todayCount = await _db.LessonDeliveries
                .CountAsync(d => d.UserId == user.Id && d.DeliveredAt.Date == DateTime.UtcNow.Date);

            if (todayCount >= MaxLessonsPerDay && !user.IsPremium)
            {
                await _bot.SendMessage(chatId,
                    $"📚 На сегодня лимит исчерпан ({MaxLessonsPerDay} из {MaxLessonsPerDay} уроков).\n" +
                    "Чтобы слова лучше запомнились, мы делаем паузу. Возвращайся завтра за новыми мемами! 🔥");

                var botInfo = await _bot.GetMe();
                string refLink = $"https://t.me/{botInfo.Username}?start=ref_{chatId}";

                await Task.Delay(500);
                await _bot.SendMessage(chatId,
                    "Но ты можешь снять дневной лимит на 2 недели! Просто пригласи 3 друзей по этой уникальной ссылке:\n" +
                    $"`{refLink}`",
                    parseMode: global::Telegram.Bot.Types.Enums.ParseMode.Markdown);
                return;
            }

            // Find a lesson the user hasn't seen yet (checking by MemeImageUrl to prevent duplicate memes)
            var sentMemeUrls = await _db.LessonDeliveries
                .Where(d => d.UserId == user.Id)
                .Select(d => d.Lesson.MemeImageUrl)
                .ToListAsync();

            var lesson = await _db.Lessons
                .Where(l => !sentMemeUrls.Contains(l.MemeImageUrl))
                .OrderBy(l => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (lesson is null)
            {
                await _bot.SendMessage(chatId,
                    "😔 Все уроки пройдены! Новые уроки скоро появятся.");
                return;
            }

            // Download and send
            try
            {
                var mediaUrl = lesson.MemeImageUrl;
                var mediaType = _mediaDownloader.DetectMediaType(mediaUrl);

                await using var stream = await _mediaDownloader.DownloadAsync(mediaUrl);
                var fileName = _mediaDownloader.ExtractFileName(mediaUrl);
                var (caption, followUp) = _messageFormatter.BuildLessonMessage(lesson);

                var ratingKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("👍 Понравилось", $"react_Like_{lesson.Id}") },
                    new[] { InlineKeyboardButton.WithCallbackData("👎 Не понравилось", $"react_Dislike_{lesson.Id}") },
                    new[] { InlineKeyboardButton.WithCallbackData("🤔 Есть сомнения", $"react_Doubt_{lesson.Id}") }
                });

                var sendResult = await _mediaSender.SendMediaAsync(chatId,
                    InputFile.FromStream(stream, fileName),
                    caption, followUp, mediaType, ratingKeyboard);

                if (!sendResult.Success)
                {
                    if (sendResult.ErrorType is Memish_Tutor.Services.Telegram.TelegramErrorType.UserBlockedBot or Memish_Tutor.Services.Telegram.TelegramErrorType.UserDeactivated)
                    {
                        user.IsActive = false;
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("Deactivated user {UserId} due to Telegram block/deactivation", user.Id);
                    }
                    else
                    {
                        await _bot.SendMessage(chatId, "😔 Не удалось отправить урок. Попробуй ещё раз позже.");
                    }
                    return;
                }

                _db.LessonDeliveries.Add(new LessonDelivery
                {
                    UserId = user.Id,
                    LessonId = lesson.Id
                });
                await _db.SaveChangesAsync();

                // Try to trigger a random quiz FIRST
                bool quizSent = await _quizDelivery.TrySendRandomQuizAsync(chatId, user.Id, lesson.Id);

                // Send separate message with "Get next lesson" button
                var followUpKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Получить ещё урок 📚", "get_lesson")
                });
                
                if (quizSent)
                {
                    await Task.Delay(500); // Small delay after the quiz
                }
                else
                {
                    await Task.Delay(Random.Shared.Next(1000, 1500)); // Normal delay after the lesson
                }
                
                await _bot.SendMessage(chatId, "Хочешь ещё один? 👇", replyMarkup: followUpKeyboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed on-demand lesson delivery to chat {ChatId}", chatId);
                await _bot.SendMessage(chatId,
                    "😔 Не удалось отправить урок. Попробуй ещё раз позже.");

                _db.ProcessingErrors.Add(new ProcessingError
                {
                    Stage = "OnDemandDelivery",
                    ErrorMessage = ex.Message,
                    RawContext = $"ChatId: {chatId}, LessonId: {lesson.Id}"
                });
                await _db.SaveChangesAsync();
            }
        }
    }
}
