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
                    await SendMatchStartRemindersAsync(notificationService, dbContext, TimeSpan.FromHours(24));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in MatchReminderHostedService.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        public async Task SendMatchStartRemindersAsync(INotificationService notificationService,
                                                        AppDbContext dbContext,
                                                        TimeSpan threshold)
        {
            var now = DateTime.UtcNow;
            var upperBound = now + threshold;

            // Decide which reminder type we're sending
            bool is1Hour = threshold.TotalHours == 1;
            bool is24Hours = threshold.TotalHours == 24;

            // Filter matches based on threshold and reminder flag
            var matches = await dbContext.CustomMatches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m =>
                    m.MatchStart > now &&
                    m.MatchStart <= upperBound &&
                    m.Status == CustomMatch.MatchStatus.Upcoming &&
                    ((is1Hour && !m.Notifications1Sent) || (is24Hours && !m.Notifications24Sent)))
                .ToListAsync();

            foreach (var match in matches)
            {
                // Notify users who haven't placed a bet
                await notificationService.NotifyMatchStartingSoonAsync(match, threshold);

                // Update the correct flag
                if (is1Hour)
                {
                    match.Notifications1Sent = true;
                }
                else if (is24Hours)
                {
                    match.Notifications24Sent = true;
                }
            }

            // Save changes (only if any matches were processed)
            if (matches.Any())
            {
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
