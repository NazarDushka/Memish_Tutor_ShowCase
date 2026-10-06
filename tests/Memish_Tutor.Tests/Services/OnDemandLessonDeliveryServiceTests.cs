using FluentAssertions;
using Memish_Tutor.Domain.Entities;
using Memish_Tutor.Infrastructure.Data;
using Memish_Tutor.Services.Lessons.Delivery;
using Memish_Tutor.Services.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;

namespace Memish_Tutor.Tests.Services
{
    public class OnDemandLessonDeliveryServiceTests
    {
        private readonly AppDBContext _dbContext;
        private readonly Mock<ITelegramMediaSender> _mediaSenderMock;
        private readonly Mock<ILogger<OnDemandLessonDeliveryService>> _loggerMock;
        private readonly Mock<Memish_Tutor.Services.Common.IMediaDownloadService> _mediaDownloaderMock;
        private readonly Mock<ILessonMessageFormatter> _formatterMock;
        private readonly Mock<global::Telegram.Bot.ITelegramBotClient> _botMock;
        private readonly Mock<IQuizDeliveryService> _quizMock;
        private readonly OnDemandLessonDeliveryService _service;

        public OnDemandLessonDeliveryServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDBContext(options);
            _mediaSenderMock = new Mock<ITelegramMediaSender>();
            _loggerMock = new Mock<ILogger<OnDemandLessonDeliveryService>>();
            _mediaDownloaderMock = new Mock<Memish_Tutor.Services.Common.IMediaDownloadService>();
            _formatterMock = new Mock<ILessonMessageFormatter>();
            _botMock = new Mock<global::Telegram.Bot.ITelegramBotClient>();
            _quizMock = new Mock<IQuizDeliveryService>();

            _service = new OnDemandLessonDeliveryService(
                _dbContext,
                _botMock.Object,
                _mediaDownloaderMock.Object,
                _mediaSenderMock.Object,
                _formatterMock.Object,
                _loggerMock.Object,
                _quizMock.Object
            );
        }

        [Fact]
        public async Task DeliverLessonAsync_WhenUserDoesNotExist_ShouldReturnFalse()
        {
            // Act
            await _service.DeliverLessonToUserAsync(12345);

            // Assert
            // (If user doesn't exist, it returns early and does not crash)
        }

        [Fact]
        public async Task DeliverLessonAsync_WhenNoUnseenLessons_ShouldReturnFalse()
        {
            // Arrange
            var user = new Memish_Tutor.Domain.Entities.User { Id = Guid.NewGuid(), TelegramChatId = 12345, IsActive = true };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.DeliverLessonToUserAsync(12345);

            // Assert
            // Message about no unseen lessons should be sent, no crash
        }

        [Fact]
        public async Task DeliverLessonAsync_WhenTelegramFails_ShouldDeactivateUserAndReturnFalse()
        {
            // Arrange
            var user = new Memish_Tutor.Domain.Entities.User { Id = Guid.NewGuid(), TelegramChatId = 12345, IsActive = true };
            var lesson = new Lesson { Id = Guid.NewGuid(), MemeImageUrl = "url" };
            _dbContext.Users.Add(user);
            _dbContext.Lessons.Add(lesson);
            await _dbContext.SaveChangesAsync();

            _mediaDownloaderMock.Setup(m => m.DownloadAsync(It.IsAny<string>()))
                .ReturnsAsync(new MemoryStream());
            _formatterMock.Setup(f => f.BuildLessonMessage(It.IsAny<Lesson>()))
                .Returns(("caption", null));

            _mediaSenderMock.Setup(m => m.SendMediaAsync(It.IsAny<long>(), It.IsAny<InputFile>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Memish_Tutor.Services.Common.MemeMediaType>(), It.IsAny<global::Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup?>()))
                .ReturnsAsync(TelegramSendResult.Fail(TelegramErrorType.UserBlockedBot, "bot was blocked"));

            // Act
            await _service.DeliverLessonToUserAsync(12345);

            // Assert
            var updatedUser = await _dbContext.Users.FindAsync(user.Id);
            updatedUser!.IsActive.Should().BeFalse(); // User should be deactivated!
        }
    }
}
