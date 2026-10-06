using FluentAssertions;
using Memish_Tutor.Domain.Entities;
using Memish_Tutor.Infrastructure.Data;
using Memish_Tutor.Services.Common;
using Memish_Tutor.Services.Lessons.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;

namespace Memish_Tutor.Tests.Services
{
    public class QuizDeliveryServiceTests
    {
        private readonly AppDBContext _dbContext;
        private readonly Mock<ITelegramBotClient> _botMock;
        private readonly Mock<ILogger<QuizDeliveryService>> _loggerMock;
        private readonly Mock<ITelegramMediaSender> _mediaSenderMock;
        private readonly Mock<IMediaDownloadService> _mediaDownloaderMock;
        private readonly QuizDeliveryService _service;

        public QuizDeliveryServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDBContext(options);
            _botMock = new Mock<ITelegramBotClient>();
            _loggerMock = new Mock<ILogger<QuizDeliveryService>>();
            _mediaSenderMock = new Mock<ITelegramMediaSender>();
            _mediaDownloaderMock = new Mock<IMediaDownloadService>();

            _service = new QuizDeliveryService(
                _dbContext,
                _botMock.Object,
                _loggerMock.Object,
                _mediaSenderMock.Object,
                _mediaDownloaderMock.Object
            );
        }

        [Fact]
        public async Task TrySendRandomQuizAsync_WhenNoPastQuizzesExist_ShouldReturnFalse()
        {
            // Act
            var result = await _service.TrySendRandomQuizAsync(12345, Guid.NewGuid(), Guid.NewGuid(), force: true);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TrySendRandomQuizAsync_WhenPastQuizExists_ShouldReturnTrueAndSend()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var excludeLessonId = Guid.NewGuid();
            
            var lesson = new Lesson { Id = Guid.NewGuid(), MemeImageUrl = "http://example.com/image.jpg", HeroPhrase = "Test" };
            var quiz = new LessonQuiz { Id = Guid.NewGuid(), LessonId = lesson.Id, OptionsJson = "[\"A\", \"B\", \"C\", \"D\"]", CorrectOptionIndex = 0 };
            lesson.Quiz = quiz;

            _dbContext.Lessons.Add(lesson);
            _dbContext.LessonQuizzes.Add(quiz);
            _dbContext.LessonDeliveries.Add(new LessonDelivery { Id = Guid.NewGuid(), UserId = userId, LessonId = lesson.Id, DeliveredAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.TrySendRandomQuizAsync(12345, userId, excludeLessonId, force: true);

            // Assert
            result.Should().BeTrue();
            _mediaSenderMock.Verify(m => m.SendMediaAsync(
                It.IsAny<long>(),
                It.IsAny<Telegram.Bot.Types.InputFile>(),
                It.IsAny<string>(),
                null,
                It.IsAny<MemeMediaType>(),
                It.IsAny<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup>()), 
                Times.Once);
        }

        [Fact]
        public async Task TrySendRandomQuizAsync_WhenOptionsAreInvalid_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lesson = new Lesson { Id = Guid.NewGuid(), MemeImageUrl = "http://example.com/image.jpg", HeroPhrase = "Test" };
            // Invalid options: not 4 options
            var quiz = new LessonQuiz { Id = Guid.NewGuid(), LessonId = lesson.Id, OptionsJson = "[\"A\", \"B\"]", CorrectOptionIndex = 0 };
            lesson.Quiz = quiz;

            _dbContext.Lessons.Add(lesson);
            _dbContext.LessonQuizzes.Add(quiz);
            _dbContext.LessonDeliveries.Add(new LessonDelivery { Id = Guid.NewGuid(), UserId = userId, LessonId = lesson.Id, DeliveredAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.TrySendRandomQuizAsync(12345, userId, Guid.NewGuid(), force: true);

            // Assert
            result.Should().BeFalse();
            _mediaSenderMock.Verify(m => m.SendMediaAsync(It.IsAny<long>(), It.IsAny<Telegram.Bot.Types.InputFile>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MemeMediaType>(), It.IsAny<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup?>()), Times.Never);
        }
    }
}
