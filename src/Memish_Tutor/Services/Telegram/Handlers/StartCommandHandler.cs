using Memish_Tutor.Domain.Entities;
using Memish_Tutor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace Memish_Tutor.Services.Telegram.Handlers
{
    public class StartCommandHandler : ICommandHandler
    {
        private readonly AppDBContext _dbContext;
        private readonly ITelegramBotClient _botClient;

        public StartCommandHandler(AppDBContext dbContext, ITelegramBotClient botClient)
        {
            _dbContext = dbContext;
            _botClient = botClient;
        }

        public bool CanHandle(string? text, string? callbackData)
        {
            return text?.StartsWith("/start") == true;
        }

        public async Task HandleAsync(long chatId, string? text, string? callbackData, string? username, int messageId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramChatId == chatId);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    TelegramChatId = chatId,
                    Name = username ?? "Unknown",
                    IsActive = true
                };
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }

            await _botClient.SendMessage(
                chatId: chatId,
                text: "Hello! I am a showcase bot.");
        }
    }
}
