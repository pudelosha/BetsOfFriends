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
    private readonly TimeSpan _finishedToInPlayCorrectionWindow = TimeSpan.FromHours(6);
    private const int StatusConfirmationThreshold = 2;

    private readonly Dictionary<int, (CustomMatch.MatchStatus Status, int Count)> _statusObservations = new();

    private DateTime _lastTournamentCheck = DateTime.MinValue;

    public FootballDataHostedService(IServiceProvider serviceProvider, ILogger<FootballDataHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private static CustomMatch.TeamQualified? ParseQualifiedTeam(string? qualifiedTeam)
    {
        return Enum.TryParse<CustomMatch.TeamQualified>(qualifiedTeam, true, out var parsed)
            ? parsed
            : null;
    }

    private static string? NormalizeCrestUrl(string? crestUrl)
    {
        return string.IsNullOrWhiteSpace(crestUrl) ? null : crestUrl.Trim();
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
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // Always run every minute
                await CheckTournamentChangesAsync(dbContext, footballDataService, betService, notificationService, stoppingToken);

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
        INotificationService notificationService,
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

                var storedPredefinedTeams = await dbContext.PredefinedTeams
                    .Where(t => t.PredefinedTournamentId == tournament.TournamentId)
                    .ToListAsync(cancellationToken);

                var predefinedTeamsByExternalId = storedPredefinedTeams
                    .Where(t => t.ExternalTeamId.HasValue)
                    .ToDictionary(t => t.ExternalTeamId!.Value);

                var predefinedTeamsByName = storedPredefinedTeams
                    .GroupBy(t => t.TeamName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var changedPredefinedTeamIds = new List<int>();

                foreach (var apiTeam in updatedDto.Teams)
                {
                    var normalizedCrestUrl = NormalizeCrestUrl(apiTeam.CrestUrl);
                    if (normalizedCrestUrl == null)
                    {
                        continue;
                    }

                    PredefinedTeam? storedTeam = null;
                    if (apiTeam.ExternalTeamId.HasValue)
                    {
                        predefinedTeamsByExternalId.TryGetValue(apiTeam.ExternalTeamId.Value, out storedTeam);
                    }

                    if (storedTeam == null && !string.IsNullOrWhiteSpace(apiTeam.TeamName))
                    {
                        predefinedTeamsByName.TryGetValue(apiTeam.TeamName, out storedTeam);
                    }

                    if (storedTeam != null && storedTeam.CrestUrl != normalizedCrestUrl)
                    {
                        storedTeam.CrestUrl = normalizedCrestUrl;
                        changedPredefinedTeamIds.Add(storedTeam.TeamId);
                    }
                }

                if (changedPredefinedTeamIds.Count > 0)
                {
                    var linkedCustomTeams = await dbContext.CustomTeams
                        .Where(t => t.PredefinedTeamId.HasValue
                            && changedPredefinedTeamIds.Contains(t.PredefinedTeamId.Value)
                            && t.Tournament.Update == CustomTournament.TournamentUpdate.Auto)
                        .ToListAsync(cancellationToken);

                    foreach (var customTeam in linkedCustomTeams)
                    {
                        var predefinedTeam = storedPredefinedTeams
                            .FirstOrDefault(t => t.TeamId == customTeam.PredefinedTeamId);

                        customTeam.CrestUrl = predefinedTeam?.CrestUrl;
                    }

                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                // ============================================================
                // Odds helpers (same shape as frontend; deterministic jitter to avoid churn)
                // ============================================================
                const double HOME_ADVANTAGE = 50.0;

                static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

                static double Clamp(double v, double min, double max)
                    => Math.Max(min, Math.Min(max, v));

                static double JitterDeterministic(int matchId, int salt = 0)
                {
                    unchecked
                    {
                        int h = (matchId * 397) ^ (salt * 104729);
                        h ^= (h >> 16);
                        var frac = ((uint)h % 10000) / 10000.0; // [0..0.9999]
                        return 0.95 + (frac / 100.0);           // [0.95..0.959999]
                    }
                }

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
                    var incomingStatus = Enum.TryParse(updatedMatch.MatchStatus, out CustomMatch.MatchStatus parsedStatus)
                        ? parsedStatus
                        : existingMatch.Status;
                    var incomingHomeScore = updatedMatch.ScoreHome;
                    var incomingAwayScore = updatedMatch.ScoreAway;
                    var incomingQualified = ParseQualifiedTeam(updatedMatch.QualifiedTeam);
                    var statusConfirmationCount = RegisterStatusObservation(
                        updatedMatch.ExternalMatchId.Value,
                        incomingStatus);

                    var existingFinished = existingMatch.Status == CustomMatch.MatchStatus.Finished;
                    var existingFinishedWithScore =
                        existingFinished &&
                        existingMatch.HomeScore.HasValue &&
                        existingMatch.AwayScore.HasValue;

                    if (!existingFinished && incomingStatus == CustomMatch.MatchStatus.Finished &&
                        (!incomingHomeScore.HasValue || !incomingAwayScore.HasValue ||
                         statusConfirmationCount < StatusConfirmationThreshold))
                    {
                        _logger.LogWarning(
                            "Waiting for confirmation of external finished status for predefined match {MatchId} / external match {ExternalMatchId}. Confirmation {ConfirmationCount}/{ConfirmationThreshold}, incoming score: {IncomingHomeScore}:{IncomingAwayScore}.",
                            existingMatch.MatchId,
                            updatedMatch.ExternalMatchId,
                            statusConfirmationCount,
                            StatusConfirmationThreshold,
                            incomingHomeScore,
                            incomingAwayScore);

                        incomingStatus = existingMatch.Status;
                        incomingQualified = existingMatch.Qualified;
                    }
                    else if (existingFinished && incomingStatus != CustomMatch.MatchStatus.Finished)
                    {
                        var confirmedLiveCorrection =
                            incomingStatus == CustomMatch.MatchStatus.In_Play &&
                            statusConfirmationCount >= StatusConfirmationThreshold &&
                            IsWithinInPlayCorrectionWindow(existingMatch.MatchStart);

                        if (confirmedLiveCorrection)
                        {
                            _logger.LogWarning(
                                "Accepting confirmed external live correction for predefined match {MatchId} / external match {ExternalMatchId}. Local: {LocalStatus} {LocalHomeScore}:{LocalAwayScore}, incoming: {IncomingStatus} {IncomingHomeScore}:{IncomingAwayScore}.",
                                existingMatch.MatchId,
                                updatedMatch.ExternalMatchId,
                                existingMatch.Status,
                                existingMatch.HomeScore,
                                existingMatch.AwayScore,
                                incomingStatus,
                                incomingHomeScore,
                                incomingAwayScore);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Ignoring unconfirmed external status downgrade for predefined match {MatchId} / external match {ExternalMatchId}. Local: {LocalStatus} {LocalHomeScore}:{LocalAwayScore}, incoming: {IncomingStatus} {IncomingHomeScore}:{IncomingAwayScore}, confirmation {ConfirmationCount}/{ConfirmationThreshold}.",
                                existingMatch.MatchId,
                                updatedMatch.ExternalMatchId,
                                existingMatch.Status,
                                existingMatch.HomeScore,
                                existingMatch.AwayScore,
                                incomingStatus,
                                incomingHomeScore,
                                incomingAwayScore,
                                statusConfirmationCount,
                                StatusConfirmationThreshold);

                            incomingStatus = existingMatch.Status;
                            incomingMatchStart = existingMatch.MatchStart;
                            incomingHomeScore = existingMatch.HomeScore;
                            incomingAwayScore = existingMatch.AwayScore;
                            incomingQualified = existingMatch.Qualified;
                        }
                    }
                    else if (existingFinishedWithScore && (!incomingHomeScore.HasValue || !incomingAwayScore.HasValue))
                    {
                        _logger.LogWarning(
                            "Preserving final score for predefined match {MatchId} / external match {ExternalMatchId}. Local score: {LocalHomeScore}:{LocalAwayScore}, incoming score: {IncomingHomeScore}:{IncomingAwayScore}.",
                            existingMatch.MatchId,
                            updatedMatch.ExternalMatchId,
                            existingMatch.HomeScore,
                            existingMatch.AwayScore,
                            incomingHomeScore,
                            incomingAwayScore);

                        incomingHomeScore = existingMatch.HomeScore;
                        incomingAwayScore = existingMatch.AwayScore;
                        incomingQualified ??= existingMatch.Qualified;
                    }

                    if (incomingStatus != CustomMatch.MatchStatus.Finished)
                    {
                        incomingQualified = null;
                    }

                    // ============================================================
                    // 1) Resolve incoming EXTERNAL team IDs from API DTO (source of truth)
                    // ============================================================
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

                    bool becameRealMatch =
                        teamChanged &&
                        (!existingExternalHomeTeamId.HasValue || !existingExternalAwayTeamId.HasValue) &&
                        incomingExternalHomeTeamId.HasValue &&
                        incomingExternalAwayTeamId.HasValue;

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
                        existingMatch.Status != incomingStatus ||
                        existingMatch.MatchStart != incomingMatchStart ||
                        existingMatch.HomeScore != incomingHomeScore ||
                        existingMatch.AwayScore != incomingAwayScore ||
                        existingMatch.Qualified != incomingQualified ||
                        teamChanged;

                    if (!hasChanges)
                        continue;

                    _logger.LogInformation(
                        $"Updating predefined match {existingMatch.MatchId} from external match {updatedMatch.ExternalMatchId}");

                    // ============================================================
                    // 4) Update predefined match
                    // ============================================================
                    existingMatch.Status = incomingStatus;
                    existingMatch.MatchStart = incomingMatchStart;
                    existingMatch.HomeScore = incomingHomeScore;
                    existingMatch.AwayScore = incomingAwayScore;
                    existingMatch.Qualified = incomingQualified;

                    // Update predefined team ids ONLY if we successfully resolved them
                    // (If still TBD/null from API, keep existing)
                    if (incomingPredefinedHomeTeamId != 0)
                        existingMatch.HomeTeamId = incomingPredefinedHomeTeamId;

                    if (incomingPredefinedAwayTeamId != 0)
                        existingMatch.AwayTeamId = incomingPredefinedAwayTeamId;

                    // If team ids changed we should ensure navigation props are consistent for odds calc
                    // (reload only when needed to avoid extra queries)
                    if (incomingPredefinedHomeTeamId != 0 || incomingPredefinedAwayTeamId != 0)
                    {
                        existingMatch.HomeTeam = await dbContext.PredefinedTeams
                            .FirstOrDefaultAsync(t => t.TeamId == existingMatch.HomeTeamId, cancellationToken);

                        existingMatch.AwayTeam = await dbContext.PredefinedTeams
                            .FirstOrDefaultAsync(t => t.TeamId == existingMatch.AwayTeamId, cancellationToken);
                    }

                    // ============================================================
                    // 4b) Update odds in PREDEFINED match as well (future only)
                    // ============================================================
                    var nowUtcForPredefined = DateTime.UtcNow;

                    bool predefinedNotStartedYet =
                        existingMatch.MatchStart > nowUtcForPredefined &&
                        existingMatch.Status != CustomMatch.MatchStatus.In_Play &&
                        existingMatch.Status != CustomMatch.MatchStatus.Finished;

                    if (predefinedNotStartedYet && existingMatch.HomeTeam != null && existingMatch.AwayTeam != null)
                    {
                        // Assumes PredefinedTeam has EloRating (same concept as CustomTeam)
                        var hElo = (double)existingMatch.HomeTeam.EloRating;
                        var aElo = (double)existingMatch.AwayTeam.EloRating;

                        double powerRatio = 1.0 / (1.0 + Math.Pow(10.0, (aElo - hElo - HOME_ADVANTAGE) / 600.0));
                        double probDraw = Clamp(0.29 - Math.Abs(0.5 - powerRatio) * 0.3, 0.0, 0.33);
                        double probHome = (1.0 - probDraw) * powerRatio;
                        double probAway = 1.0 - probHome - probDraw;

                        if (probHome > 0 && probDraw > 0 && probAway > 0)
                        {
                            double jitter = JitterDeterministic(existingMatch.MatchId);

                            existingMatch.HomeWinOdds = Round2((decimal)((1.0 / probHome) * jitter));
                            existingMatch.DrawOdds = Round2((decimal)((1.0 / probDraw) * jitter));
                            existingMatch.AwayWinOdds = Round2((decimal)((1.0 / probAway) * jitter));

                            if (existingMatch.Type == CustomMatch.MatchType.ExtendedWithQualification)
                            {
                                var probHomeQualifies = probHome + probDraw * 0.5;
                                var probAwayQualifies = 1.0 - probHomeQualifies;

                                if (probHomeQualifies > 0 && probAwayQualifies > 0)
                                {
                                    var homeQualifiesJitter = JitterDeterministic(existingMatch.MatchId, 1);
                                    var awayQualifiesJitter = JitterDeterministic(existingMatch.MatchId, 2);

                                    existingMatch.HomeQualifies = Round2((decimal)((1.0 / probHomeQualifies) * homeQualifiesJitter));
                                    existingMatch.AwayQualifies = Round2((decimal)((1.0 / probAwayQualifies) * awayQualifiesJitter));
                                }
                            }
                        }
                    }

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
                        customMatch.Qualified = existingMatch.Qualified;
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
                                    placeholderHome.CrestUrl = existingMatch.HomeTeam?.CrestUrl;
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
                                    placeholderAway.CrestUrl = existingMatch.AwayTeam?.CrestUrl;
                                    customMatch.AwayTeamId = placeholderAway.TeamId;
                                }
                            }
                        }

                        // ============================================================
                        // 5b) Recalculate odds for CUSTOM future (not started) matches only
                        // ============================================================
                        var nowUtc = DateTime.UtcNow;

                        bool matchNotStartedYet =
                            customMatch.MatchStart > nowUtc &&
                            customMatch.Status != CustomMatch.MatchStatus.In_Play &&
                            customMatch.Status != CustomMatch.MatchStatus.Finished;

                        if (matchNotStartedYet)
                        {
                            // Resolve ELO for the CURRENT (possibly remapped) custom teams
                            var homeElo = await dbContext.CustomTeams
                                .Where(t => t.TeamId == customMatch.HomeTeamId)
                                .Select(t => (int?)t.EloRating)
                                .FirstOrDefaultAsync(cancellationToken);

                            var awayElo = await dbContext.CustomTeams
                                .Where(t => t.TeamId == customMatch.AwayTeamId)
                                .Select(t => (int?)t.EloRating)
                                .FirstOrDefaultAsync(cancellationToken);

                            // if not resolvable (placeholders), skip odds update
                            if (homeElo.HasValue && awayElo.HasValue)
                            {
                                double hElo = homeElo.Value;
                                double aElo = awayElo.Value;

                                double powerRatio = 1.0 / (1.0 + Math.Pow(10.0, (aElo - hElo - HOME_ADVANTAGE) / 600.0));
                                double probDraw = Clamp(0.29 - Math.Abs(0.5 - powerRatio) * 0.3, 0.0, 0.33);
                                double probHome = (1.0 - probDraw) * powerRatio;
                                double probAway = 1.0 - probHome - probDraw;

                                if (probHome > 0 && probDraw > 0 && probAway > 0)
                                {
                                    double jitter = JitterDeterministic(customMatch.MatchId);

                                    var odds1 = Round2((decimal)((1.0 / probHome) * jitter));
                                    var oddsX = Round2((decimal)((1.0 / probDraw) * jitter));
                                    var odds2 = Round2((decimal)((1.0 / probAway) * jitter));

                                    customMatch.HomeWinOdds = odds1;
                                    customMatch.DrawOdds = oddsX;
                                    customMatch.AwayWinOdds = odds2;

                                    if (customMatch.Type == CustomMatch.MatchType.ExtendedWithQualification)
                                    {
                                        var probHomeQualifies = probHome + probDraw * 0.5;
                                        var probAwayQualifies = 1.0 - probHomeQualifies;

                                        if (probHomeQualifies > 0 && probAwayQualifies > 0)
                                        {
                                            var homeQualifiesJitter = JitterDeterministic(customMatch.MatchId, 1);
                                            var awayQualifiesJitter = JitterDeterministic(customMatch.MatchId, 2);

                                            customMatch.HomeQualifies = Round2((decimal)((1.0 / probHomeQualifies) * homeQualifiesJitter));
                                            customMatch.AwayQualifies = Round2((decimal)((1.0 / probAwayQualifies) * awayQualifiesJitter));
                                        }
                                    }
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
                        if (becameRealMatch &&
                            customMatch.MatchStart > DateTime.UtcNow &&
                            customMatch.Status == CustomMatch.MatchStatus.Timed)
                        {
                            try
                            {
                                await betService.GenerateBetsForNewMatchAsync(customMatch.MatchId, customMatch.TournamentId);
                                await notificationService.NotifyNewGamesToBetAsync(customMatch);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to send new game notification for match {customMatch.MatchId}");
                            }
                        }

                        if (customMatch.Status == CustomMatch.MatchStatus.Finished)
                        {
                            _logger.LogInformation($"Triggering bet recalculation for custom match {customMatch.MatchId}");
                            try
                            {
                                await betService.RecalculateBetsForMatchAsync(customMatch.MatchId);
                                await notificationService.NotifyMatchClosureAsync(customMatch);
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

    private int RegisterStatusObservation(int externalMatchId, CustomMatch.MatchStatus status)
    {
        if (_statusObservations.TryGetValue(externalMatchId, out var observation) &&
            observation.Status == status)
        {
            var updatedCount = Math.Min(observation.Count + 1, StatusConfirmationThreshold);
            _statusObservations[externalMatchId] = (status, updatedCount);
            return updatedCount;
        }

        _statusObservations[externalMatchId] = (status, 1);
        return 1;
    }

    private bool IsWithinInPlayCorrectionWindow(DateTime matchStart)
    {
        var matchStartUtc = DateTime.SpecifyKind(matchStart, DateTimeKind.Utc);
        var nowUtc = DateTime.UtcNow;

        return nowUtc >= matchStartUtc.AddMinutes(-30) &&
               nowUtc <= matchStartUtc.Add(_finishedToInPlayCorrectionWindow);
    }
}
