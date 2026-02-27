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
            .Where(t => t.ExternalTournamentId != null
                && t.Update == CustomTournament.TournamentUpdate.Auto
                && t.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var tournament in predefinedTournaments)
        {
            if (!tournament.IsActive)
                continue;

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
                        .Include(m => m.HomeTeam)
                        .Include(m => m.AwayTeam)
                        .FirstOrDefaultAsync(m => m.ExternalMatchId == updatedMatch.ExternalMatchId, cancellationToken);

                    if (existingMatch == null)
                        continue;

                    // --- NEW: detect team change (TBA -> real team) using incoming IDs ---
                    var incomingHomeTeamId = await dbContext.PredefinedTeams
                        .Where(t => t.PredefinedTournamentId == tournament.TournamentId && t.TeamName == updatedMatch.HomeTeam)
                        .Select(t => t.TeamId)
                        .FirstOrDefaultAsync(cancellationToken);

                    var incomingAwayTeamId = await dbContext.PredefinedTeams
                        .Where(t => t.PredefinedTournamentId == tournament.TournamentId && t.TeamName == updatedMatch.AwayTeam)
                        .Select(t => t.TeamId)
                        .FirstOrDefaultAsync(cancellationToken);

                    bool teamChanged =
                        incomingHomeTeamId != 0 && incomingAwayTeamId != 0 &&
                        (existingMatch.HomeTeamId != incomingHomeTeamId ||
                         existingMatch.AwayTeamId != incomingAwayTeamId);

                    bool hasChanges =
                        existingMatch.ExternalMatchId == updatedMatch.ExternalMatchId && (
                        existingMatch.Status.ToString() != updatedMatch.MatchStatus ||
                        existingMatch.MatchStart != updatedMatch.MatchStart ||
                        existingMatch.HomeScore != updatedMatch.ScoreHome ||
                        existingMatch.AwayScore != updatedMatch.ScoreAway ||
                        teamChanged);

                    if (!hasChanges)
                        continue;

                    _logger.LogInformation($"Updating predefined match {existingMatch.MatchId} from external match {updatedMatch.ExternalMatchId}");

                    var previousStatus = existingMatch.Status;

                    // Update match values
                    existingMatch.Status = Enum.TryParse(updatedMatch.MatchStatus, out CustomMatch.MatchStatus parsedStatus)
                        ? parsedStatus
                        : existingMatch.Status;

                    existingMatch.MatchStart = DateTime.SpecifyKind(updatedMatch.MatchStart, DateTimeKind.Utc);
                    existingMatch.HomeScore = updatedMatch.ScoreHome;
                    existingMatch.AwayScore = updatedMatch.ScoreAway;

                    // Update predefined match team IDs (use incoming IDs we already computed)
                    if (incomingHomeTeamId != 0 && incomingAwayTeamId != 0)
                    {
                        existingMatch.HomeTeamId = incomingHomeTeamId;
                        existingMatch.AwayTeamId = incomingAwayTeamId;
                    }

                    // Propagate changes to linked custom matches
                    var relatedCustomMatches = await dbContext.CustomMatches
                        .Where(m => m.PredefinedMatchId == existingMatch.MatchId &&
                                    m.Tournament.Update == CustomTournament.TournamentUpdate.Auto)
                        .Include(m => m.Tournament)
                        .ToListAsync(cancellationToken);

                    foreach (var customMatch in relatedCustomMatches)
                    {
                        _logger.LogInformation($"Propagating updates to custom match {customMatch.MatchId}");

                        // Always propagate these
                        customMatch.MatchStart = existingMatch.MatchStart;
                        customMatch.HomeScore = existingMatch.HomeScore;
                        customMatch.AwayScore = existingMatch.AwayScore;
                        customMatch.Status = existingMatch.Status;

                        // --- NEW: update CustomMatch team IDs by mapping PredefinedTeamId -> CustomTeam.TeamId ---
                        // This is the critical part for TBA -> real team updates.

                        var customTournamentId = customMatch.Tournament.TournamentId;

                        var newPredefinedHomeTeamId = existingMatch.HomeTeamId;
                        var newPredefinedAwayTeamId = existingMatch.AwayTeamId;

                        // HOME: find the custom team that references this predefined team
                        if (newPredefinedHomeTeamId > 0)
                        {
                            var mappedHomeTeam = await dbContext.CustomTeams
                                .FirstOrDefaultAsync(t =>
                                    t.TournamentId == customTournamentId &&
                                    t.PredefinedTeamId == newPredefinedHomeTeamId, cancellationToken);

                            if (mappedHomeTeam != null)
                            {
                                customMatch.HomeTeamId = mappedHomeTeam.TeamId;
                            }
                            else
                            {
                                // Fallback: reuse the placeholder team row currently used by the match
                                var placeholderHome = await dbContext.CustomTeams
                                    .FirstOrDefaultAsync(t =>
                                        t.TournamentId == customTournamentId &&
                                        t.TeamId == customMatch.HomeTeamId, cancellationToken);

                                if (placeholderHome != null)
                                {
                                    placeholderHome.PredefinedTeamId = newPredefinedHomeTeamId;
                                    placeholderHome.TeamName = updatedMatch.HomeTeam; // optional
                                    customMatch.HomeTeamId = placeholderHome.TeamId;
                                }
                            }
                        }

                        // AWAY
                        if (newPredefinedAwayTeamId > 0)
                        {
                            var mappedAwayTeam = await dbContext.CustomTeams
                                .FirstOrDefaultAsync(t =>
                                    t.TournamentId == customTournamentId &&
                                    t.PredefinedTeamId == newPredefinedAwayTeamId, cancellationToken);

                            if (mappedAwayTeam != null)
                            {
                                customMatch.AwayTeamId = mappedAwayTeam.TeamId;
                            }
                            else
                            {
                                var placeholderAway = await dbContext.CustomTeams
                                    .FirstOrDefaultAsync(t =>
                                        t.TournamentId == customTournamentId &&
                                        t.TeamId == customMatch.AwayTeamId, cancellationToken);

                                if (placeholderAway != null)
                                {
                                    placeholderAway.PredefinedTeamId = newPredefinedAwayTeamId;
                                    placeholderAway.TeamName = updatedMatch.AwayTeam; // optional
                                    customMatch.AwayTeamId = placeholderAway.TeamId;
                                }
                            }
                        }
                    }

                    // Save changes BEFORE any service calls
                    await dbContext.SaveChangesAsync(cancellationToken);

                    // Call services AFTER changes are saved
                    foreach (var customMatch in relatedCustomMatches)
                    {
                        if (customMatch.Status == CustomMatch.MatchStatus.Finished)
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
                        else if (customMatch.Status == CustomMatch.MatchStatus.In_Play)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing predefined tournament ID {tournament.TournamentId}");
            }
        }

        _logger.LogInformation("Finished checking tournament changes.");
    }
}
