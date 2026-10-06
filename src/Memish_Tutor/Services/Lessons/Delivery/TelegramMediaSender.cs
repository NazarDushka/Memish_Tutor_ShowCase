using Memish_Tutor.Services.Common;
using Memish_Tutor.Services.Telegram;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

using Telegram.Bot.Types.ReplyMarkups;

namespace Memish_Tutor.Services.Lessons.Delivery
{
    public interface ITelegramMediaSender
    {
        Task<TelegramSendResult> SendMediaAsync(
            long chatId,
            InputFile inputFile,
            string caption,
            string? followUp,
            MemeMediaType mediaType,
            InlineKeyboardMarkup? replyMarkup = null);
    }

    public class TelegramMediaSender : ITelegramMediaSender
    {
        private readonly ITelegramBotClient _bot;

        public TelegramMediaSender(ITelegramBotClient bot)
        {
            _bot = bot;
        }

        public async Task<TelegramSendResult> SendMediaAsync(
            long chatId,
            InputFile inputFile,
            string caption,
            string? followUp,
            MemeMediaType mediaType,
            InlineKeyboardMarkup? replyMarkup = null)
        {
            try
            {
                var fileId = mediaType switch
                {
                    MemeMediaType.Gif => await SendGifAsync(chatId, inputFile, caption, string.IsNullOrEmpty(followUp) ? replyMarkup : null),
                    _ => await SendPhotoAsync(chatId, inputFile, caption, string.IsNullOrEmpty(followUp) ? replyMarkup : null)
                };

                // Send the full lesson text as a follow-up if it didn't fit in the caption
                if (!string.IsNullOrEmpty(followUp))
                {
                    await Task.Delay(Random.Shared.Next(1000, 2000));
                    await _bot.SendMessage(chatId, followUp, replyMarkup: replyMarkup);
                }

                return TelegramSendResult.Ok(fileId);
            }
            catch (ApiRequestException ex)
            {
                var errorType = DetermineErrorType(ex);
                return TelegramSendResult.Fail(errorType, ex.Message);
            }
            catch (Exception ex)
            {
                return TelegramSendResult.Fail(TelegramErrorType.Unknown, ex.Message);
            }
        }

        private async Task<string?> SendPhotoAsync(long chatId, InputFile inputFile, string caption, InlineKeyboardMarkup? replyMarkup)
        {
            var msg = await _bot.SendPhoto(chatId, inputFile, caption: caption, replyMarkup: replyMarkup);
            return msg.Photo?.LastOrDefault()?.FileId;
        }

        private async Task<string?> SendGifAsync(long chatId, InputFile inputFile, string caption, InlineKeyboardMarkup? replyMarkup)
        {
            var msg = await _bot.SendAnimation(chatId, inputFile, caption: caption, replyMarkup: replyMarkup);
            return msg.Animation?.FileId;
        }

        private static TelegramErrorType DetermineErrorType(ApiRequestException ex)
        {
            if (ex.ErrorCode == 403)
            {
                if (ex.Message.Contains("deactivated", StringComparison.OrdinalIgnoreCase))
                    return TelegramErrorType.UserDeactivated;
                
                return TelegramErrorType.UserBlockedBot;
            }
            
            if (ex.ErrorCode == 400 && ex.Message.Contains("chat not found", StringComparison.OrdinalIgnoreCase))
            {
                return TelegramErrorType.ChatNotFound;
            }

            return TelegramErrorType.Unknown;
        }
    }
}
