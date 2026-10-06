namespace Memish_Tutor.Services.Lessons.Models
{
    /// <summary>
    /// Outcome of a single lesson delivery attempt.
    /// Collected during the send phase, persisted to DB in the final phase.
    /// </summary>
    public sealed record DeliveryResult
    {
        public Guid UserId { get; init; }
        public Guid LessonId { get; init; }
        public string? ImageUrl { get; init; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        public static DeliveryResult Ok(PendingDelivery d) => new()
        {
            UserId = d.User.Id,
            LessonId = d.Lesson.Id,
            ImageUrl = d.Lesson.MemeImageUrl,
            Success = true
        };

        public static DeliveryResult Fail(PendingDelivery d, string error) => new()
        {
            UserId = d.User.Id,
            LessonId = d.Lesson.Id,
            ImageUrl = d.Lesson.MemeImageUrl,
            Success = false,
            ErrorMessage = error
        };
    }
}
