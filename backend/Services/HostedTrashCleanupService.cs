
using CloudCore.Services.Interfaces;

namespace CloudCore.Services
{
    public class HostedTrashCleanupService : BackgroundService
    {
        private readonly ILogger<HostedTrashCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        // Every 24 hours
        private readonly TimeSpan _period = TimeSpan.FromHours(24);

        public HostedTrashCleanupService(ILogger<HostedTrashCleanupService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hosted Trash Cleanup Service is starting.");

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Hosted Trash Cleanup Service is running.");

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var trashCleanupService = scope.ServiceProvider.GetRequiredService<ITrashCleanupService>();
                        int cleanedItems = await trashCleanupService.CleanupExpiredItemsAsync();
                        _logger.LogInformation($"Trash cleanup task finished. Cleaned up {cleanedItems} items.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning up trash.");
                }

                await Task.Delay(_period, stoppingToken);
            }

            _logger.LogInformation("Hosted Trash Cleanup Service is stopping.");
        }
    }

}

