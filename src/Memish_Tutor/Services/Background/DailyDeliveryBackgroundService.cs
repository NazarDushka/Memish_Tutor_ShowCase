using Memish_Tutor.Services.Lessons.Delivery;

namespace Memish_Tutor.Services.Background
{
    public class DailyDeliveryBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval;

        public DailyDeliveryBackgroundService(IServiceProvider serviceProvider, IConfiguration config)
        {
            _serviceProvider = serviceProvider;
            var minutes = config.GetValue<int>("Delivery:DailyDeliveryCheckInterval", 2);
            _checkInterval = TimeSpan.FromMinutes(minutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var deliveryService = scope.ServiceProvider.GetRequiredService<IDailyLessonDeliveryService>();
                    try
                    {
                        var currentHour = DateTime.UtcNow.Hour;
                        
                        if (currentHour >= 7 && currentHour < 23)
                        {
                            await deliveryService.DeliverDailyLessonAsync();
                        }
                        else
                        {
                            Console.WriteLine($"Delivery skipped. Current hour {currentHour} is outside the allowed window (09:00 - 23:00).");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Delivery error: {ex.Message}");
                    }
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
