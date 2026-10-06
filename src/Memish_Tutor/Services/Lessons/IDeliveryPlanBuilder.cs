using Memish_Tutor.Services.Lessons.Models;

namespace Memish_Tutor.Services.Lessons
{
    /// <summary>
    /// Builds the daily delivery plan: determines which user
    /// should receive which lesson based on delivery history.
    /// </summary>
    public interface IDeliveryPlanBuilder
    {
        /// <summary>
        /// Returns a list of (User, Lesson) pairs for today's delivery.
        /// Users who already received a lesson today or have no unsent lessons are excluded.
        /// </summary>
        Task<List<PendingDelivery>> BuildAsync();
    }
}
