using System.Collections.Concurrent;
using Memish_Tutor.Domain.Entities;
using Memish_Tutor.Infrastructure.Data;
using Memish_Tutor.Services.Common;
using Memish_Tutor.Services.Lessons.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Memish_Tutor.Services.Lessons.Delivery
{
    public interface IDailyLessonDeliveryService
    {
        Task DeliverDailyLessonAsync();
    }

    public class DailyLessonDeliveryService : IDailyLessonDeliveryService
    {
        private readonly AppDBContext _db;
        private readonly IDeliveryPlanBuilder _planBuilder;
        private readonly IMediaDownloadService _mediaDownloader;
        private readonly ITelegramMediaSender _mediaSender;
        private readonly ILessonMessageFormatter _messageFormatter;
        private readonly ILogger<DailyLessonDeliveryService> _logger;
        private readonly ITelegramBotClient _bot;
        private readonly IQuizDeliveryService _quizDelivery;

        private const int MaxConcurrentSends = 10;

        public DailyLessonDeliveryService(
            AppDBContext db,
            IDeliveryPlanBuilder planBuilder,
            IMediaDownloadService mediaDownloader,
            ITelegramMediaSender mediaSender,
            ILessonMessageFormatter messageFormatter,
            ILogger<DailyLessonDeliveryService> logger,
            ITelegramBotClient bot,
            IQuizDeliveryService quizDelivery)
        {
            _db = db;
            _planBuilder = planBuilder;
            _mediaDownloader = mediaDownloader;
            _mediaSender = mediaSender;
            _messageFormatter = messageFormatter;
            _logger = logger;
            _bot = bot;
            _quizDelivery = quizDelivery;
        }

        public async Task DeliverDailyLessonAsync()
        {
            var plan = await _planBuilder.BuildAsync();

            if (plan.Count == 0)
            {
                _logger.LogInformation("No lessons to deliver today");
                return;
            }

            var mediaGroups = plan
                .GroupBy(d => d.Lesson.MemeImageUrl)
                .ToList();

            _logger.LogInformation(
                "Delivering {Total} lessons ({UniqueMedia} unique media)",
                plan.Count, mediaGroups.Count);

            var results = new ConcurrentBag<DeliveryResult>();
            var semaphore = new SemaphoreSlim(MaxConcurrentSends);

            foreach (var group in mediaGroups)
            {
                await SendMediaGroupAsync(group.ToList(), semaphore, results);
            }

            PersistResults(results);
            await _db.SaveChangesAsync();

            var ok = results.Count(r => r.Success);
            var fail = results.Count(r => !r.Success);
            _logger.LogInformation("Delivery complete: {Ok} ok, {Fail} failed", ok, fail);
        }

        private async Task SendMediaGroupAsync(
            List<PendingDelivery> deliveries,
            SemaphoreSlim semaphore,
            ConcurrentBag<DeliveryResult> results)
        {
            var mediaUrl = deliveries[0].Lesson.MemeImageUrl;
            var mediaType = _mediaDownloader.DetectMediaType(mediaUrl);
            string? cachedFileId = null;

            // First user: download + upload to Telegram → get file_id
            var first = deliveries[0];
            try
            {
                await using var stream = await _mediaDownloader.DownloadAsync(mediaUrl);
                var fileName = _mediaDownloader.ExtractFileName(mediaUrl);
                var (caption, followUp) = _messageFormatter.BuildLessonMessage(first.Lesson);

                var ratingKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("👍 Понравилось", $"react_Like_{first.Lesson.Id}") },
                    new[] { InlineKeyboardButton.WithCallbackData("👎 Не понравилось", $"react_Dislike_{first.Lesson.Id}") },
                    new[] { InlineKeyboardButton.WithCallbackData("🤔 Есть сомнения", $"react_Doubt_{first.Lesson.Id}") }
                });

                var sendResult = await _mediaSender.SendMediaAsync(
                    first.User.TelegramChatId,
                    InputFile.FromStream(stream, fileName),
                    caption, followUp, mediaType, ratingKeyboard);

                if (!sendResult.Success)
                {
                    _logger.LogWarning("Failed initial send for {Url} ({Type}) to user {UserId}: {Error}", mediaUrl, mediaType, first.User.Id, sendResult.ErrorMessage);
                    
                    if (sendResult.ErrorType is Memish_Tutor.Services.Telegram.TelegramErrorType.UserBlockedBot or Memish_Tutor.Services.Telegram.TelegramErrorType.UserDeactivated)
                    {
                        first.User.IsActive = false;
                        _logger.LogInformation("Deactivated user {UserId} due to Telegram block/deactivation", first.User.Id);
                    }
                    
                    results.Add(DeliveryResult.Fail(first, sendResult.ErrorMessage ?? "Unknown error"));
                    return;
                }

                cachedFileId = sendResult.FileId;

                results.Add(DeliveryResult.Ok(first));
                _logger.LogInformation("Delivered daily lesson {LessonId} to user {UserId}", first.Lesson.Id, first.User.Id);

                // Send separate message with "Get next lesson" button
                var followUpKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Получить ещё урок 📚", "get_lesson")
                });
                await Task.Delay(Random.Shared.Next(1000, 1500));
                await _bot.SendMessage(first.User.TelegramChatId, "Хочешь ещё один? 👇", replyMarkup: followUpKeyboard);

                // Try to trigger a random quiz
                await _quizDelivery.TrySendRandomQuizAsync(first.User.TelegramChatId, first.User.Id, first.Lesson.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during initial send for {Url} ({Type})", mediaUrl, mediaType);
                if (ex is global::Telegram.Bot.Exceptions.ApiRequestException apiEx && (apiEx.ErrorCode == 403 || apiEx.Message.Contains("Forbidden")))
                {
                    first.User.IsActive = false;
                    _logger.LogInformation("Deactivated user {UserId} due to Telegram block/deactivation during follow-up", first.User.Id);
                }
                results.Add(DeliveryResult.Fail(first, ex.Message));
                return;
            }

            // Remaining users: reuse file_id in parallel
            var remainingDeliveries = deliveries.Skip(1).ToList();
            if (remainingDeliveries.Count == 0) return;

            var tasks = remainingDeliveries.Select(async delivery =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var (caption, followUp) = _messageFormatter.BuildLessonMessage(delivery.Lesson);

                    var ratingKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("👍 Понравилось", $"react_Like_{delivery.Lesson.Id}") },
                        new[] { InlineKeyboardButton.WithCallbackData("👎 Не понравилось", $"react_Dislike_{delivery.Lesson.Id}") },
                        new[] { InlineKeyboardButton.WithCallbackData("🤔 Есть сомнения", $"react_Doubt_{delivery.Lesson.Id}") }
                    });

                    Memish_Tutor.Services.Telegram.TelegramSendResult sendResult;
                    if (cachedFileId is not null)
                    {
                        sendResult = await _mediaSender.SendMediaAsync(
                            delivery.User.TelegramChatId,
                            InputFile.FromFileId(cachedFileId),
                            caption, followUp, mediaType, ratingKeyboard);
                    }
                    else
                    {
                        // Fallback: download again
                        await using var stream = await _mediaDownloader.DownloadAsync(mediaUrl);
                        var fn = _mediaDownloader.ExtractFileName(mediaUrl);
                        sendResult = await _mediaSender.SendMediaAsync(
                            delivery.User.TelegramChatId,
                            InputFile.FromStream(stream, fn),
                            caption, followUp, mediaType, ratingKeyboard);
                    }

                    if (!sendResult.Success)
                    {
                        if (sendResult.ErrorType is Memish_Tutor.Services.Telegram.TelegramErrorType.UserBlockedBot or Memish_Tutor.Services.Telegram.TelegramErrorType.UserDeactivated)
                        {
                            delivery.User.IsActive = false;
                            _logger.LogInformation("Deactivated user {UserId} due to Telegram block/deactivation", delivery.User.Id);
                        }
                        
                        results.Add(DeliveryResult.Fail(delivery, sendResult.ErrorMessage ?? "Unknown error"));
                        return;
                    }

                    results.Add(DeliveryResult.Ok(delivery));
                    _logger.LogInformation("Delivered daily lesson {LessonId} to user {UserId}", delivery.Lesson.Id, delivery.User.Id);

                    // Send separate message with "Get next lesson" button
                    var followUpKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Получить ещё урок 📚", "get_lesson")
                    });
                    await Task.Delay(Random.Shared.Next(1000, 1500));
                    await _bot.SendMessage(delivery.User.TelegramChatId, "Хочешь ещё один? 👇", replyMarkup: followUpKeyboard);

                    // Try to trigger a random quiz
                    await _quizDelivery.TrySendRandomQuizAsync(delivery.User.TelegramChatId, delivery.User.Id, delivery.Lesson.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed sending to user {UserId}", delivery.User.Id);
                    if (ex is global::Telegram.Bot.Exceptions.ApiRequestException apiEx && (apiEx.ErrorCode == 403 || apiEx.Message.Contains("Forbidden")))
                    {
                        delivery.User.IsActive = false;
                        _logger.LogInformation("Deactivated user {UserId} due to Telegram block/deactivation during follow-up", delivery.User.Id);
                    }
                    results.Add(DeliveryResult.Fail(delivery, ex.Message));
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private void PersistResults(ConcurrentBag<DeliveryResult> results)
        {
            foreach (var r in results)
            {
                if (r.Success)
                {
                    _db.LessonDeliveries.Add(new LessonDelivery
                    {
                        UserId = r.UserId,
                        LessonId = r.LessonId
                    });
                }
                else
                {
                    _db.ProcessingErrors.Add(new ProcessingError
                    {
                        Stage = "LessonDelivery",
                        ErrorMessage = r.ErrorMessage!,
                        RawContext = $"UserId: {r.UserId}, LessonId: {r.LessonId}, ImageUrl: {r.ImageUrl}"
                    });
                }
            }
        }
    }
}
