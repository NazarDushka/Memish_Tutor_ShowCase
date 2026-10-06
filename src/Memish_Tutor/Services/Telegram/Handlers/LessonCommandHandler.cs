using Memish_Tutor.Services.Lessons.Delivery;

namespace Memish_Tutor.Services.Telegram.Handlers
{
    public class LessonCommandHandler : ICommandHandler
    {
        private readonly IOnDemandLessonDeliveryService _lessonDelivery;

        public LessonCommandHandler(IOnDemandLessonDeliveryService lessonDelivery)
        {
            _lessonDelivery = lessonDelivery;
        }

        public bool CanHandle(string? text, string? callbackData)
        {
            if (text?.StartsWith("/lesson") == true || text == "📚 Ещё урок") return true;

            var validCallbacks = new[] { "cmd_lesson", "get_lesson" };
            return callbackData != null && validCallbacks.Contains(callbackData);
        }

        public async Task HandleAsync(long chatId, string? text, string? callbackData, string? username, int messageId)
        {
            await _lessonDelivery.DeliverLessonToUserAsync(chatId);
        }
    }
}
