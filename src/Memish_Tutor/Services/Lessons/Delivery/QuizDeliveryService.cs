using Memish_Tutor.Domain.Entities;
using Memish_Tutor.Infrastructure.Data;
using Memish_Tutor.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Memish_Tutor.Services.Lessons.Delivery
{
    public interface IQuizDeliveryService
    {
        Task<bool> TrySendRandomQuizAsync(long chatId, Guid userId, Guid excludeLessonId, bool force = false);
    }

    public class QuizDeliveryService : IQuizDeliveryService
    {
        private readonly AppDBContext _db;
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<QuizDeliveryService> _logger;
        private readonly ITelegramMediaSender _mediaSender;
        private readonly IMediaDownloadService _mediaDownloader;
        private const double QuizChance = 0.20; // 20% chance

        public QuizDeliveryService(
            AppDBContext db,
            ITelegramBotClient botClient,
            ILogger<QuizDeliveryService> logger,
            ITelegramMediaSender mediaSender,
            IMediaDownloadService mediaDownloader)
        {
            _db = db;
            _botClient = botClient;
            _logger = logger;
            _mediaSender = mediaSender;
            _mediaDownloader = mediaDownloader;
        }

        public async Task<bool> TrySendRandomQuizAsync(long chatId, Guid userId, Guid excludeLessonId, bool force = false)
        {
            if (!force && Random.Shared.NextDouble() > QuizChance)
            {
                return false;
            }

            // Find past delivered lessons for this user that have an associated quiz, excluding the current lesson
            var pastQuizLessons = await _db.LessonDeliveries
                .Where(ld => ld.UserId == userId && ld.LessonId != excludeLessonId && ld.Lesson.Quiz != null)
                .Select(ld => new { ld.Lesson, ld.Lesson.Quiz })
                .ToListAsync();

            if (pastQuizLessons.Count == 0)
            {
                _logger.LogInformation("Quiz triggered, but user {UserId} has no past lessons with quizzes.", userId);
                return false;
            }

            var randomItem = pastQuizLessons[Random.Shared.Next(pastQuizLessons.Count)];
            var lesson = randomItem.Lesson;
            var quiz = randomItem.Quiz!;

            var options = JsonSerializer.Deserialize<List<string>>(quiz.OptionsJson);
            if (options == null || options.Count != 4)
            {
                _logger.LogError("Quiz {QuizId} has invalid options.", quiz.Id);
                return false;
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] 
                { 
                    InlineKeyboardButton.WithCallbackData("1️⃣", $"quiz_{quiz.Id}_0"),
                    InlineKeyboardButton.WithCallbackData("2️⃣", $"quiz_{quiz.Id}_1")
                },
                new[] 
                { 
                    InlineKeyboardButton.WithCallbackData("3️⃣", $"quiz_{quiz.Id}_2"),
                    InlineKeyboardButton.WithCallbackData("4️⃣", $"quiz_{quiz.Id}_3")
                }
            });

            var caption = $"🎲 <b>Случайная проверка!</b>\n\nПомнишь, как переводится эта фраза из прошлого урока?\n\n«<b>{lesson.HeroPhrase}</b>»\n\n" +
                          $"1️⃣ {options[0]}\n" +
                          $"2️⃣ {options[1]}\n" +
                          $"3️⃣ {options[2]}\n" +
                          $"4️⃣ {options[3]}";

            try
            {
                await Task.Delay(Random.Shared.Next(1500, 2500));
                
                var mediaType = _mediaDownloader.DetectMediaType(lesson.MemeImageUrl);
                await _mediaSender.SendMediaAsync(
                    chatId: chatId,
                    inputFile: global::Telegram.Bot.Types.InputFile.FromUri(lesson.MemeImageUrl),
                    caption: caption,
                    followUp: null,
                    mediaType: mediaType,
                    replyMarkup: keyboard
                );

                _logger.LogInformation("Sent random quiz {QuizId} to user {UserId}", quiz.Id, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send quiz {QuizId} to chat {ChatId}", quiz.Id, chatId);
                return false;
            }
        }
    }
}
