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

            var now = DateTime.UtcNow;

            // Step 1: Get all match IDs that are finished or in the past and not Timed
            var finalisedMatches = await dbContext.CustomMatches
                .Where(m => m.Status == CustomMatch.MatchStatus.Finished ||
                            (m.MatchStart < now && m.Status != CustomMatch.MatchStatus.Timed))
                .Select(m => m.MatchId)
                .ToListAsync(cancellationToken);

            if (!finalisedMatches.Any())
            {
                _logger.LogInformation("No finalised matches found. Skipping bet updates.");
                return;
            }

            int totalUpdated = 0;

            // Step 2: Process match-by-match (avoids .Contains() in SQL)
            foreach (var matchId in finalisedMatches)
            {
                var bets = await dbContext.Bets
                    .Where(b => b.MatchId == matchId && b.Status != Bet.BetStatus.Closed)
                    .ToListAsync(cancellationToken);

                if (!bets.Any())
                    continue;

                foreach (var bet in bets)
                    bet.Status = Bet.BetStatus.Closed;

                await dbContext.SaveChangesAsync(cancellationToken);
                totalUpdated += bets.Count;

                // Optional: clear change tracking to avoid memory bloat
                foreach (var bet in bets)
                    dbContext.Entry(bet).State = EntityState.Detached;
            }

            _logger.LogInformation($"Successfully updated {totalUpdated} bets to Finalised.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating bet statuses.");
        }
    }
}
