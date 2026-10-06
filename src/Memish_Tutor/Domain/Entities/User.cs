using System.ComponentModel.DataAnnotations.Schema;

namespace Memish_Tutor.Domain.Entities
{
    public class User
    {
        public Guid Id { get; init; }
        public long TelegramChatId { get; init; }
        public string? Name { get; init; }
        public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        [NotMapped]
        public bool IsPremium => PremiumUntil.HasValue && PremiumUntil.Value > DateTime.UtcNow;
        public DateTime? PremiumUntil { get; set; }
        public ICollection<Lesson> Lessons { get; init; } = new List<Lesson>();
    }
}
