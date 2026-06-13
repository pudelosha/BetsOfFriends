using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Services
{
    public class NotificationHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationHostedService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _dailyTournamentUpdateTime = TimeSpan.FromHours(12);
        private DateOnly? _lastDailyTournamentUpdateDate;

        public NotificationHostedService(
            IServiceProvider serviceProvider,
            ILogger<NotificationHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    await SendMatchStartRemindersAsync(notificationService, dbContext, TimeSpan.FromHours(1));
                    await notificationService.NotifyAdminsAboutMissingBetsAsync(DateTime.UtcNow);
                    await SendDailyTournamentUpdatesIfDueAsync(notificationService);
                    await DeleteOldNotificationsAsync(dbContext);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in MatchReminderHostedService.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task SendDailyTournamentUpdatesIfDueAsync(INotificationService notificationService)
        {
            var localNow = DateTime.Now;
            var today = DateOnly.FromDateTime(localNow);

            if (_lastDailyTournamentUpdateDate == today)
            {
                return;
            }

            if (localNow.TimeOfDay < _dailyTournamentUpdateTime)
            {
                return;
            }

            await notificationService.NotifyDailyTournamentUpdatesAsync(DateTime.UtcNow);
            _lastDailyTournamentUpdateDate = today;
        }

        public async Task SendMatchStartRemindersAsync(
            INotificationService notificationService,
            AppDbContext dbContext,
            TimeSpan threshold)
        {
            var now = DateTime.UtcNow;
            var lowerBound = now.AddMinutes(-1); // Add 1-minute tolerance
            var upperBound = now + threshold;

            // Decide which reminder type we're sending
            bool is1Hour = threshold.TotalHours == 1;
            bool is24Hours = threshold.TotalHours == 24;

            _logger.LogDebug($"Checking match reminders for {threshold.TotalHours}h window.");
            _logger.LogDebug($"Window: {lowerBound:u} to {upperBound:u}");

            // Fetch matches within the timing window that haven't been notified yet
            var matches = await dbContext.CustomMatches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m =>
                    m.MatchStart >= lowerBound &&
                    m.MatchStart <= upperBound &&
                    m.Status == CustomMatch.MatchStatus.Timed &&
                    ((is1Hour && !m.Notifications1Sent) || (is24Hours && !m.Notifications24Sent)))
                .ToListAsync();

            _logger.LogInformation($"Found {matches.Count} match(es) for {threshold.TotalHours}h reminder.");

            foreach (var match in matches)
            {
                await notificationService.NotifyMatchStartingSoonAsync(match, threshold);

                if (is1Hour)
                    match.Notifications1Sent = true;
                else if (is24Hours)
                    match.Notifications24Sent = true;
            }

            if (matches.Any())
            {
                await dbContext.SaveChangesAsync();
            }
        }

        private async Task DeleteOldNotificationsAsync(AppDbContext dbContext)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-14);

            _logger.LogInformation($"Deleting notifications older than {cutoffDate:u}");

            // Fetch notifications older than cutoff
            var oldNotifications = await dbContext.Notifications
                .Where(n => n.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldNotifications.Any())
            {
                _logger.LogInformation($"Found {oldNotifications.Count} old notifications. Deleting...");

                // Avoid SQL Server OPENJSON/compatibility-level issues from local collection Contains().
                var oldRecipients = await dbContext.NotificationRecipients
                    .Where(r => r.Notification.CreatedAt < cutoffDate)
                    .ToListAsync();

                dbContext.NotificationRecipients.RemoveRange(oldRecipients);
                dbContext.Notifications.RemoveRange(oldNotifications);

                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Old notifications and recipients deleted.");
            }
            else
            {
                _logger.LogInformation("No old notifications found.");
            }
        }
    }
}
