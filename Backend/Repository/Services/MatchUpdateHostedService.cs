using Backend.Model.Database;
using Microsoft.EntityFrameworkCore;
using static Backend.Model.Entities.CustomMatch;

public class MatchUpdateHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MatchUpdateHostedService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public MatchUpdateHostedService(IServiceProvider serviceProvider, ILogger<MatchUpdateHostedService> logger)
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

                await UpdateCustomMatchStatusesAsync(dbContext, stoppingToken);
                await UpdatePredefinedMatchStatusesAsync(dbContext, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MatchUpdateHostedService execution.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task UpdateCustomMatchStatusesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking for CUSTOM matches that need status updates...");

            var matches = await dbContext.CustomMatches
                .Where(m => m.MatchStart <= DateTime.UtcNow && m.Status == MatchStatus.Upcoming)
                .ToListAsync(cancellationToken);

            if (!matches.Any())
            {
                _logger.LogInformation("No CUSTOM matches require status updates.");
                return;
            }

            foreach (var match in matches)
            {
                match.Status = MatchStatus.InProgress;
                _logger.LogInformation($"Custom Match {match.MatchId} status updated to InProgress.");
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"{matches.Count} CUSTOM matches were updated to InProgress.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating CUSTOM match statuses.");
        }
    }

    private async Task UpdatePredefinedMatchStatusesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking for PREDEFINED matches that need status updates...");

            var matches = await dbContext.PredefinedMatches
                .Where(m => m.MatchStart <= DateTime.UtcNow && m.Status == MatchStatus.Upcoming)
                .ToListAsync(cancellationToken);

            if (!matches.Any())
            {
                _logger.LogInformation("No PREDEFINED matches require status updates.");
                return;
            }

            foreach (var match in matches)
            {
                match.Status = MatchStatus.InProgress;
                _logger.LogInformation($"Predefined Match {match.MatchId} status updated to InProgress.");
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"{matches.Count} PREDEFINED matches were updated to InProgress.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating PREDEFINED match statuses.");
        }
    }
}
