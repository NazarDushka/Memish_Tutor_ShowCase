namespace Memish_Tutor.Domain.Entities
{
    public class LessonDelivery
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        public Guid LessonId { get; set; }
        public Lesson Lesson { get; set; }

        public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
    }
}
