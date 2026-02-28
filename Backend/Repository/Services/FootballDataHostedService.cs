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

                // Build an API lookup: ExternalTeamId -> TeamName (and optionally reverse)
                // NOTE: ExternalTeamId for TBD is null (as you said), so we handle nulls.
                var apiTeamsByName = updatedDto.Teams
                    .GroupBy(t => t.TeamName)
                    .ToDictionary(g => g.Key, g => g.First());

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

                    // Normalize incoming MatchStart to UTC before compare/update
                    var incomingMatchStart = DateTime.SpecifyKind(updatedMatch.MatchStart, DateTimeKind.Utc);

                    // ============================================================
                    // 1) Resolve incoming EXTERNAL team IDs from API DTO (source of truth)
                    // ============================================================
                    // If your updatedMatch already contains ExternalHomeTeamId/ExternalAwayTeamId,
                    // use those instead of name lookup.
                    int? incomingExternalHomeTeamId = null;
                    int? incomingExternalAwayTeamId = null;

                    if (!string.IsNullOrWhiteSpace(updatedMatch.HomeTeam) &&
                        apiTeamsByName.TryGetValue(updatedMatch.HomeTeam, out var apiHomeTeam))
                    {
                        incomingExternalHomeTeamId = apiHomeTeam.ExternalTeamId;
                    }

                    if (!string.IsNullOrWhiteSpace(updatedMatch.AwayTeam) &&
                        apiTeamsByName.TryGetValue(updatedMatch.AwayTeam, out var apiAwayTeam))
                    {
                        incomingExternalAwayTeamId = apiAwayTeam.ExternalTeamId;
                    }

                    // Existing external team ids (from DB)
                    var existingExternalHomeTeamId = existingMatch.HomeTeam?.ExternalTeamId;
                    var existingExternalAwayTeamId = existingMatch.AwayTeam?.ExternalTeamId;

                    // Detect team change based on EXTERNAL IDs
                    // This catches "TBD (null) -> real team (non-null)" and also changes between real teams.
                    bool teamChanged =
                        incomingExternalHomeTeamId != existingExternalHomeTeamId ||
                        incomingExternalAwayTeamId != existingExternalAwayTeamId;

                    // ============================================================
                    // 2) Resolve incoming PREDEFINED TeamIds by ExternalTeamId (not by name)
                    // ============================================================
                    int incomingPredefinedHomeTeamId = 0;
                    int incomingPredefinedAwayTeamId = 0;

                    if (incomingExternalHomeTeamId.HasValue)
                    {
                        incomingPredefinedHomeTeamId = await dbContext.PredefinedTeams
                            .Where(t => t.PredefinedTournamentId == tournament.TournamentId
                                     && t.ExternalTeamId == incomingExternalHomeTeamId.Value)
                            .Select(t => t.TeamId)
                            .FirstOrDefaultAsync(cancellationToken);
                    }

                    if (incomingExternalAwayTeamId.HasValue)
                    {
                        incomingPredefinedAwayTeamId = await dbContext.PredefinedTeams
                            .Where(t => t.PredefinedTournamentId == tournament.TournamentId
                                     && t.ExternalTeamId == incomingExternalAwayTeamId.Value)
                            .Select(t => t.TeamId)
                            .FirstOrDefaultAsync(cancellationToken);
                    }

                    // ============================================================
                    // 3) Detect changes (time/status/score OR team change)
                    // ============================================================
                    bool hasChanges =
                        existingMatch.Status.ToString() != updatedMatch.MatchStatus ||
                        existingMatch.MatchStart != incomingMatchStart ||
                        existingMatch.HomeScore != updatedMatch.ScoreHome ||
                        existingMatch.AwayScore != updatedMatch.ScoreAway ||
                        teamChanged;

                    if (!hasChanges)
                        continue;

                    _logger.LogInformation(
                        $"Updating predefined match {existingMatch.MatchId} from external match {updatedMatch.ExternalMatchId}");

                    // ============================================================
                    // 4) Update predefined match
                    // ============================================================
                    existingMatch.Status = Enum.TryParse(updatedMatch.MatchStatus, out CustomMatch.MatchStatus parsedStatus)
                        ? parsedStatus
                        : existingMatch.Status;

                    existingMatch.MatchStart = incomingMatchStart;
                    existingMatch.HomeScore = updatedMatch.ScoreHome;
                    existingMatch.AwayScore = updatedMatch.ScoreAway;

                    // Update predefined team ids ONLY if we successfully resolved them
                    // (If still TBD/null from API, keep existing)
                    if (incomingPredefinedHomeTeamId != 0)
                        existingMatch.HomeTeamId = incomingPredefinedHomeTeamId;

                    if (incomingPredefinedAwayTeamId != 0)
                        existingMatch.AwayTeamId = incomingPredefinedAwayTeamId;

                    // ============================================================
                    // 5) Propagate to linked custom matches
                    // ============================================================
                    var relatedCustomMatches = await dbContext.CustomMatches
                        .Where(m => m.PredefinedMatchId == existingMatch.MatchId
                                 && m.Tournament.Update == CustomTournament.TournamentUpdate.Auto)
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

                        var customTournamentId = customMatch.Tournament.TournamentId;

                        var newPredefinedHomeId = existingMatch.HomeTeamId; // internal predefined TeamId
                        var newPredefinedAwayId = existingMatch.AwayTeamId;

                        // HOME: map predefined team -> custom team
                        if (newPredefinedHomeId > 0)
                        {
                            var mappedHomeTeam = await dbContext.CustomTeams
                                .FirstOrDefaultAsync(t =>
                                    t.TournamentId == customTournamentId &&
                                    t.PredefinedTeamId == newPredefinedHomeId,
                                    cancellationToken);

                            if (mappedHomeTeam != null)
                            {
                                customMatch.HomeTeamId = mappedHomeTeam.TeamId;
                            }
                            else
                            {
                                // optional fallback: reuse placeholder row currently linked to match
                                var placeholderHome = await dbContext.CustomTeams
                                    .FirstOrDefaultAsync(t =>
                                        t.TournamentId == customTournamentId &&
                                        t.TeamId == customMatch.HomeTeamId,
                                        cancellationToken);

                                if (placeholderHome != null)
                                {
                                    placeholderHome.PredefinedTeamId = newPredefinedHomeId;
                                    // do NOT rely on names; but keeping is fine as best-effort display
                                    placeholderHome.TeamName = updatedMatch.HomeTeam;
                                    customMatch.HomeTeamId = placeholderHome.TeamId;
                                }
                            }
                        }

                        // AWAY
                        if (newPredefinedAwayId > 0)
                        {
                            var mappedAwayTeam = await dbContext.CustomTeams
                                .FirstOrDefaultAsync(t =>
                                    t.TournamentId == customTournamentId &&
                                    t.PredefinedTeamId == newPredefinedAwayId,
                                    cancellationToken);

                            if (mappedAwayTeam != null)
                            {
                                customMatch.AwayTeamId = mappedAwayTeam.TeamId;
                            }
                            else
                            {
                                var placeholderAway = await dbContext.CustomTeams
                                    .FirstOrDefaultAsync(t =>
                                        t.TournamentId == customTournamentId &&
                                        t.TeamId == customMatch.AwayTeamId,
                                        cancellationToken);

                                if (placeholderAway != null)
                                {
                                    placeholderAway.PredefinedTeamId = newPredefinedAwayId;
                                    placeholderAway.TeamName = updatedMatch.AwayTeam;
                                    customMatch.AwayTeamId = placeholderAway.TeamId;
                                }
                            }
                        }
                    }

                    // Save changes BEFORE any service calls
                    await dbContext.SaveChangesAsync(cancellationToken);

                    // ============================================================
                    // 6) Call services AFTER changes are saved
                    // ============================================================
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
