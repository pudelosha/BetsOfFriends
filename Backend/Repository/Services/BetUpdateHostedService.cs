using Backend.Model.Database;
using Backend.Model.Entities;
using Microsoft.EntityFrameworkCore;

public class BetUpdateHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BetUpdateHostedService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public BetUpdateHostedService(IServiceProvider serviceProvider, ILogger<BetUpdateHostedService> logger)
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

                await AutoUpdateBetStatusAsync(dbContext, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BetUpdateHostedService.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task AutoUpdateBetStatusAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting automatic update of bet statuses...");

            // Step 1: Get all matches that are in the past AND are not Timed
            //TODO review this logic !!!!!!!!!!
            var finalisedMatchIds = await dbContext.CustomMatches
                .Where(m => m.Status == CustomMatch.MatchStatus.Finished ||
                            (m.MatchStart < DateTime.UtcNow && m.Status != CustomMatch.MatchStatus.Timed))
                .Select(m => m.MatchId)
                .ToListAsync(cancellationToken);

            if (!finalisedMatchIds.Any())
            {
                _logger.LogInformation("No finalised matches found in the past. No bets updated.");
                return;
            }

            // Step 2: Get all bets related to those matches that are NOT already Finalised
            var betsToUpdate = await dbContext.Bets
                .Where(b => finalisedMatchIds.Contains(b.MatchId) && b.Status != Bet.BetStatus.Closed)
                .ToListAsync(cancellationToken);

            if (!betsToUpdate.Any())
            {
                _logger.LogInformation("No bets found that need status updates.");
                return;
            }

            // Step 3: Update the status of those bets to Finalised
            foreach (var bet in betsToUpdate)
            {
                bet.Status = Bet.BetStatus.Closed;
            }

            // Step 4: Save changes
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Successfully updated {betsToUpdate.Count} bets to Finalised.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating bet statuses.");
        }
    }
}
