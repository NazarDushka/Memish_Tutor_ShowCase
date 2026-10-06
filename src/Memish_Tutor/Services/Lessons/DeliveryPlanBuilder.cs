using Memish_Tutor.Infrastructure.Data;
using Memish_Tutor.Services.Lessons.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace Memish_Tutor.Services.Lessons
{
    /// <summary>
    /// Queries the database to build today's delivery plan.
    /// For each active user who hasn't received a lesson today,
    /// picks a random unsent lesson.
    /// </summary>
    public class DeliveryPlanBuilder : IDeliveryPlanBuilder
    {
        private readonly AppDBContext _db;
        private readonly ITelegramBotClient _bot;

        public DeliveryPlanBuilder(AppDBContext db, ITelegramBotClient bot)
        {
            _db = db;
            _bot = bot;
        }

        public async Task<List<PendingDelivery>> BuildAsync()
        {
            var plan = new List<PendingDelivery>();

            var activeUsers = await _db.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            foreach (var user in activeUsers)
            {
                var alreadySentToday = await _db.LessonDeliveries
                    .AnyAsync(d => d.UserId == user.Id && d.DeliveredAt.Date == DateTime.UtcNow.Date);

                if (alreadySentToday) continue;

                var sentMemeUrls = await _db.LessonDeliveries
                    .Where(d => d.UserId == user.Id)
                    .Select(d => d.Lesson.MemeImageUrl)
                    .ToListAsync();

                var candidate = await _db.Lessons
                    .Where(l => !sentMemeUrls.Contains(l.MemeImageUrl))
                    .OrderBy(l => Guid.NewGuid())
                    .FirstOrDefaultAsync();

                if (candidate is null)
                {
                    try
                    {
                        await _bot.SendMessage(user.TelegramChatId, "Сегодня новых уроков нет. Пожалуйста, попробуйте завтра.");
                    }
                    catch (global::Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 403 || ex.Message.Contains("Forbidden"))
                    {
                        user.IsActive = false;
                        await _db.SaveChangesAsync();
                    }
                    catch
                    {
                        // Ignore other errors for now to keep building the plan
                    }
                    continue;
                }

                plan.Add(new PendingDelivery(user, candidate));
            }

            return plan;
        }
    }
}
