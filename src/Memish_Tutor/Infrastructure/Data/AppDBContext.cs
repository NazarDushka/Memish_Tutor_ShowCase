using Memish_Tutor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Memish_Tutor.Infrastructure.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

        public DbSet<User> Users { get { return Set<User>(); } }
        public DbSet<Lesson> Lessons { get { return Set<Lesson>(); } }
        public DbSet<RawMeme> Memes { get { return Set<RawMeme>(); } }
        public DbSet<LessonDelivery> LessonDeliveries { get { return Set<LessonDelivery>(); } }
        public DbSet<ProcessingError> ProcessingErrors { get { return Set<ProcessingError>(); } }
        public DbSet<LessonQuiz> LessonQuizzes { get { return Set<LessonQuiz>(); } }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.TelegramChatId).IsRequired();
                entity.Property(u => u.Name).HasMaxLength(100);
                entity.Property(u => u.RegisteredAt).IsRequired();
                entity.Property(u => u.IsActive).IsRequired();
            });
            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.HasKey(l => l.Id);
            });
            modelBuilder.Entity<RawMeme>(entity =>
            {
                entity.HasKey(m => m.Id);
            });
            modelBuilder.Entity<LessonDelivery>(entity =>
            {
                entity.HasKey(ld => ld.Id);
            });
            modelBuilder.Entity<ProcessingError>(entity =>
            {
                entity.HasKey(pe => pe.Id);
            });
            modelBuilder.Entity<LessonQuiz>(entity =>
            {
                entity.HasKey(q => q.Id);
            });
        }
    }
}
