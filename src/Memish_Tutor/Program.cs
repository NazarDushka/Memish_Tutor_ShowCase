using Memish_Tutor.Infrastructure.Data;
using Memish_Tutor.Services.Lessons;
using Memish_Tutor.Services.LLM;
using Memish_Tutor.Services.Meme_Api;
using Memish_Tutor.Services.Lessons.Delivery;
using Memish_Tutor.Services.Telegram.Handlers;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Memish_Tutor.Services.Background;
using Memish_Tutor.Services.Common;
using Memish_Tutor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
    new TelegramBotClient(builder.Configuration["Telegram:BotToken"]!));

builder.Services.AddControllers();
builder.Services.AddScoped<IUpdateHandlerService, UpdateHandlerService>();

// Showcase Fake Services
builder.Services.AddScoped<IMemeService, FakeMemeService>();
builder.Services.AddScoped<MemeFetchService>();
builder.Services.AddScoped<ILLMClient, FakeLlmClient>();

builder.Services.AddScoped<LessonGenerationService>();
builder.Services.AddHttpClient("ImageDownloader");
builder.Services.AddScoped<IMediaDownloadService, MediaDownloadService>();
builder.Services.AddScoped<IDeliveryPlanBuilder, DeliveryPlanBuilder>();

// Delivery Pipeline
builder.Services.AddSingleton<ILessonMessageFormatter, LessonMessageFormatter>();
builder.Services.AddScoped<ITelegramMediaSender, TelegramMediaSender>();
builder.Services.AddScoped<IQuizDeliveryService, QuizDeliveryService>();
builder.Services.AddScoped<IOnDemandLessonDeliveryService, OnDemandLessonDeliveryService>();
builder.Services.AddScoped<IDailyLessonDeliveryService, DailyLessonDeliveryService>();

// Core Command Handlers
builder.Services.AddScoped<ICommandHandler, StartCommandHandler>();
builder.Services.AddScoped<ICommandHandler, HelpCommandHandler>();
builder.Services.AddScoped<ICommandHandler, StopCommandHandler>();
builder.Services.AddScoped<ICommandHandler, LessonCommandHandler>();
builder.Services.AddScoped<QuizCallbackHandler>();
builder.Services.AddScoped<HelpCommandHandler>(); 

// Only run the core delivery background service in showcase
builder.Services.AddHostedService<DailyDeliveryBackgroundService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapControllers();
app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
    await botClient.SetMyCommands(new[]
    {
        new Telegram.Bot.Types.BotCommand { Command = "lesson", Description = "Получить новый мем-урок" },
        new Telegram.Bot.Types.BotCommand { Command = "help", Description = "Справка и настройки" },
        new Telegram.Bot.Types.BotCommand { Command = "stop", Description = "Остановить рассылку" }
    });
}

app.Run();
