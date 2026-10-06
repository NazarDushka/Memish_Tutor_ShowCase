namespace Memish_Tutor.Domain.Entities
{
    public class ProcessingError
    {
        public Guid Id { get; set; }

        public string Stage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string? RawContext { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; } = false;
    }
}
