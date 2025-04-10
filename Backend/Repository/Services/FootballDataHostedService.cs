using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class FootballDataHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FootballDataHostedService> _logger;

    private readonly TimeSpan _inPlayInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _tournamentCheckInterval = TimeSpan.FromHours(1);

    private DateTime _lastTournamentCheck = DateTime.MinValue;

    public FootballDataHostedService(IServiceProvider serviceProvider, ILogger<FootballDataHostedService> logger)
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
                var footballDataService = scope.ServiceProvider.GetRequiredService<IFootballDataService>();
                var betService = scope.ServiceProvider.GetRequiredService<IBetService>();

                // Always run every minute
                await CheckTournamentChangesAsync(dbContext, footballDataService, betService, stoppingToken);

                // Run hourly
                if (DateTime.UtcNow - _lastTournamentCheck >= _tournamentCheckInterval)
                {
                    //TODO add if required
                    _lastTournamentCheck = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FootballDataHostedService.");
            }

            await Task.Delay(_inPlayInterval, stoppingToken);
        }
    }

    private async Task CheckTournamentChangesAsync(
        AppDbContext dbContext,
        IFootballDataService footballDataService,
        IBetService betService,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking predefined tournament changes...");

        var predefinedTournaments = await dbContext.PredefinedTournaments
            .Where(t => t.ExternalTournamentId != null && t.Update == CustomTournament.TournamentUpdate.Auto)
            .ToListAsync(cancellationToken);

        foreach (var tournament in predefinedTournaments)
        {
            try
            {
                var externalId = tournament.ExternalTournamentId!.Value;
                var season = tournament.Season ?? 2024;

                var rawJson = await footballDataService.GetCompetitionMatchesAsync(externalId, season);
                var updatedDto = await footballDataService.ConvertToPredefinedTournamentDtoAsync(rawJson);

                foreach (var updatedMatch in updatedDto.Matches)
                {
                    if (updatedMatch.ExternalMatchId == null)
                        continue;

                    var existingMatch = await dbContext.PredefinedMatches
                        .FirstOrDefaultAsync(m => m.ExternalMatchId == updatedMatch.ExternalMatchId, cancellationToken);

                    if (existingMatch == null)
                        continue;

                    bool hasChanges =
                        existingMatch.Status.ToString() != updatedMatch.MatchStatus ||
                        existingMatch.MatchStart != updatedMatch.MatchStart ||
                        existingMatch.HomeScore != updatedMatch.ScoreHome ||
                        existingMatch.AwayScore != updatedMatch.ScoreAway;

                    if (hasChanges)
                    {
                        _logger.LogInformation($"Updating predefined match {existingMatch.MatchId} from external match {updatedMatch.ExternalMatchId}");

                        // Capture original status before update
                        var previousStatus = existingMatch.Status;

                        // Apply updates
                        existingMatch.Status = Enum.TryParse(updatedMatch.MatchStatus, out CustomMatch.MatchStatus parsedStatus)
                            ? parsedStatus
                            : existingMatch.Status;

                        existingMatch.MatchStart = DateTime.SpecifyKind(updatedMatch.MatchStart, DateTimeKind.Utc);
                        existingMatch.HomeScore = updatedMatch.ScoreHome;
                        existingMatch.AwayScore = updatedMatch.ScoreAway;

                        // Find and update all custom matches linked to this predefined match
                        var relatedCustomMatches = await dbContext.CustomMatches
                            .Where(m => m.PredefinedMatchId == existingMatch.MatchId &&
                                        m.Tournament.Update == CustomTournament.TournamentUpdate.Auto)
                            .Include(m => m.Tournament)
                            .ToListAsync(cancellationToken);

                        foreach (var customMatch in relatedCustomMatches)
                        {
                            _logger.LogInformation($"Propagating updates to custom match {customMatch.MatchId}");

                            var previousCustomStatus = customMatch.Status;

                            customMatch.MatchStart = existingMatch.MatchStart;
                            customMatch.HomeScore = existingMatch.HomeScore;
                            customMatch.AwayScore = existingMatch.AwayScore;
                            customMatch.Status = existingMatch.Status;

                            if (existingMatch.Status == CustomMatch.MatchStatus.Finished)
                            {
                                _logger.LogInformation($"Triggering bet recalculation for custom match {customMatch.MatchId}");

                                try
                                {
                                    await betService.RecalculateBetsForMatchAsync(customMatch.MatchId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Failed to recalculate bets for match {customMatch.MatchId}");
                                }
                            }
                            else if (existingMatch.Status == CustomMatch.MatchStatus.In_Play && previousCustomStatus != CustomMatch.MatchStatus.In_Play)
                            {
                                _logger.LogInformation($"Marking bets as completed for in-play match {customMatch.MatchId}");

                                try
                                {
                                    await betService.MarkBetsAsCompletedForMatchAsync(customMatch.MatchId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Failed to mark bets as completed for match {customMatch.MatchId}");
                                }
                            }
                        }
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing predefined tournament ID {tournament.TournamentId}");
            }
        }

        _logger.LogInformation("Finished checking tournament changes.");
    }
}
