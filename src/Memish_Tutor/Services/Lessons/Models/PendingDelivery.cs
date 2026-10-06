namespace Memish_Tutor.Services.Lessons.Models
{
    /// <summary>
    /// Represents a planned delivery: which user gets which lesson.
    /// Built during the planning phase, consumed by the delivery pipeline.
    /// </summary>
    public sealed record PendingDelivery(Domain.Entities.User User, Domain.Entities.Lesson Lesson);
}
