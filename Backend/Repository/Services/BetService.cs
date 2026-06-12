using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Backend.Model.Entities.CustomMatch;
using static Backend.Model.Entities.CustomTournament;

namespace Backend.Repository.Services
{
    public class BetService : IBetService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BetService> _logger;

        public BetService(AppDbContext context, ILogger<BetService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private static bool IsMatchOpenForBetting(CustomMatch match, DateTime nowUtc)
        {
            return match.MatchStart > nowUtc &&
                   (match.Status == CustomMatch.MatchStatus.Scheduled ||
                    match.Status == CustomMatch.MatchStatus.Timed);
        }

        private static IQueryable<Bet> WhereMatchOpenForBetting(IQueryable<Bet> query, DateTime nowUtc)
        {
            return query.Where(b =>
                b.Match.MatchStart > nowUtc &&
                (b.Match.Status == CustomMatch.MatchStatus.Scheduled ||
                 b.Match.Status == CustomMatch.MatchStatus.Timed));
        }

        public async Task CreateBetsForTournamentAsync(int tournamentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"Ensuring bets are created for tournament ID: {tournamentId}");

                // Step 1: Fetch all matches for the tournament
                var matches = await _context.CustomMatches
                    .Where(m => m.TournamentId == tournamentId)
                    .ToListAsync();

                if (!matches.Any())
                {
                    _logger.LogWarning($"No matches found for tournament ID: {tournamentId}. Skipping bet creation.");
                    return;
                }

                var matchIds = matches.Select(m => m.MatchId).ToList();

                // Step 2: Fetch all users participating in this tournament
                var users = await _context.CustomTournamentUserAssignments
                    .Where(u => u.TournamentId == tournamentId) // optionally add && u.Status == AssignmentStatus.Accepted
                    .Select(u => u.UserId)
                    .ToListAsync();

                if (!users.Any())
                {
                    _logger.LogWarning($"No participants found for tournament ID: {tournamentId}. Skipping bet creation.");
                    return;
                }

                // Step 3: Load all existing bets for this tournament into memory
                var allBets = await _context.Bets
                    .Where(b => b.Match.TournamentId == tournamentId)
                    .Select(b => new { b.MatchId, b.UserId })
                    .ToListAsync();

                var existingBetPairs = new HashSet<(int MatchId, string UserId)>(
                    allBets.Select(b => (b.MatchId, b.UserId))
                );

                // Step 4: Create only missing bets
                var betsToInsert = new List<Bet>();

                foreach (var match in matches)
                {
                    foreach (var userId in users)
                    {
                        if (!existingBetPairs.Contains((match.MatchId, userId)))
                        {
                            betsToInsert.Add(new Bet
                            {
                                MatchId = match.MatchId,
                                UserId = userId,
                                BaseAmount = 1,
                                BonusAmount = null,
                                HomeGoals = null,
                                AwayGoals = null,
                                Qualified = null,
                                Status = Bet.BetStatus.ToPlace,
                                Result = Bet.BetResult.Pending,
                                Calculated = false,
                                BasePayout = null,
                                QualificationPayout = null,
                                ExactScorePayout = null
                            });
                        }
                    }
                }

                // Step 5: Insert new bets
                if (betsToInsert.Any())
                {
                    await _context.Bets.AddRangeAsync(betsToInsert);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Inserted {betsToInsert.Count} missing bets for tournament ID: {tournamentId}");
                }
                else
                {
                    _logger.LogInformation($"No missing bets found. Tournament {tournamentId} is already fully populated.");
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error ensuring bets for tournament ID {tournamentId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateBetAsync(int betId, string userId, BetUpdateDto betUpdateDto)
        {
            try
            {
                var bet = await _context.Bets
                    .Include(b => b.Match) // Ensure we retrieve match details
                    .FirstOrDefaultAsync(b => b.BetId == betId && b.UserId == userId);

                if (bet == null)
                {
                    _logger.LogWarning($"Bet ID {betId} not found or does not belong to user {userId}.");
                    return false;
                }

                var nowUtc = DateTime.UtcNow;

                // Block update if match has already started or is no longer in a betting-open status.
                if (!IsMatchOpenForBetting(bet.Match, nowUtc))
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to update Bet ID {BetId}, but match {MatchId} is closed for betting. MatchStart: {MatchStart:o}, Status: {Status}, NowUtc: {NowUtc:o}",
                        userId,
                        betId,
                        bet.MatchId,
                        DateTime.SpecifyKind(bet.Match.MatchStart, DateTimeKind.Utc),
                        bet.Match.Status,
                        nowUtc);
                    return false;
                }

                // Update bet details
                bet.BaseAmount = betUpdateDto.BaseAmount;
                bet.BonusAmount = betUpdateDto.BonusAmount;
                bet.HomeGoals = betUpdateDto.HomeGoals;
                bet.AwayGoals = betUpdateDto.AwayGoals;
                bet.Calculated = false;
                bet.Status = Bet.BetStatus.Placed; // Status update after submission

                // Convert string to enum if provided
                if (!string.IsNullOrEmpty(betUpdateDto.QualifiedTeam) &&
                    Enum.TryParse<CustomMatch.TeamQualified>(betUpdateDto.QualifiedTeam, true, out var qualifiedTeamEnum))
                {
                    bet.Qualified = qualifiedTeamEnum;
                }
                else
                {
                    bet.Qualified = null;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bet ID {betId} updated successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating bet ID {betId} for user {userId}.");
                return false;
            }
        }

        public async Task<List<BetDto>> GetBetsByStatusAndStageAsync(int tournamentId, string userId, string status, string stage)
        {
            try
            {
                _logger.LogInformation($"Fetching bets for user {userId}, tournament {tournamentId}, status {status}, stage {stage}");

                // Step 1: Validate and parse BetStatus
                if (!Enum.TryParse<Bet.BetStatus>(status, true, out var betStatus))
                {
                    _logger.LogWarning($"Invalid bet status received: {status}");
                    return new List<BetDto>(); // Return empty list instead of throwing an error
                }

                // Get tournament
                var tournament = await _context.CustomTournaments
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament {tournamentId} not found.");
                    return new List<BetDto>();
                }

                // Step 2: Check if the user is assigned to the tournament
                var isAssigned = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (!isAssigned)
                {
                    _logger.LogWarning($"User {userId} is not assigned to tournament {tournamentId}");
                    return new List<BetDto>(); // No access
                }

                // Step 3: Validate if the stage exists in the tournament
                bool stageExists = await _context.CustomMatchStages
                    .AnyAsync(s => s.TournamentId == tournamentId && s.StageName == stage);

                if (!stageExists)
                {
                    _logger.LogWarning($"Stage {stage} does not exist for tournament {tournamentId}");
                    return new List<BetDto>(); // Return empty list if stage doesn't exist
                }

                var nowUtc = DateTime.UtcNow;

                // Step 4: Fetch bets with the given criteria (tournament, user, status, and stage)
                var query = _context.Bets
                    .Include(b => b.Match)
                        .ThenInclude(m => m.HomeTeam)
                    .Include(b => b.Match)
                        .ThenInclude(m => m.AwayTeam)
                    .Include(b => b.Match)
                        .ThenInclude(m => m.Stage)
                    .Where(b =>
                        b.Match.TournamentId == tournamentId &&
                        b.UserId == userId &&
                        b.Status == betStatus &&
                        b.Match.IsVisible == true && 
                        b.Match.Stage.StageName == stage);

                if (betStatus == Bet.BetStatus.ToPlace || betStatus == Bet.BetStatus.Placed)
                {
                    query = WhereMatchOpenForBetting(query, nowUtc);
                }

                var bets = await query
                    .OrderBy(b => b.Match.MatchStart)
                    .ToListAsync();

                if (!bets.Any())
                {
                    _logger.LogWarning($"No bets found for user {userId}, tournament {tournamentId}, status {status}, stage {stage}");
                    return new List<BetDto>();
                }

                // Step 5: Convert bets to DTO format
                var betDtos = bets.Select(b => new BetDto
                {
                    BetId = b.BetId,
                    MatchId = b.MatchId,
                    TeamHome = b.Match.HomeTeam.TeamName,
                    TeamAway = b.Match.AwayTeam.TeamName,
                    HomeTeamCrestUrl = b.Match.HomeTeam.CrestUrl,
                    AwayTeamCrestUrl = b.Match.AwayTeam.CrestUrl,

                    StartTime = DateTime.SpecifyKind(b.Match.MatchStart, DateTimeKind.Utc),

                    BaseAmount = b.BaseAmount,
                    BonusAmount = b.BonusAmount,

                    PlayerHomeGoals = b.HomeGoals,
                    PlayerAwayGoals = b.AwayGoals,
                    PlayerQualifiedTeam = b.Qualified?.ToString(),
                    ActualHomeGoals = b.Match.HomeScore,
                    ActualAwayGoals = b.Match.AwayScore,
                    ActualQualifiedTeam = b.Match.Qualified?.ToString(),

                    HomeOdds = b.Match.HomeWinOdds,
                    DrawOdds = b.Match.DrawOdds,
                    AwayOdds = b.Match.AwayWinOdds,

                    QualifyHomeOdds = b.Match.HomeQualifies,
                    QualifyAwayOdds = b.Match.AwayQualifies,

                    MatchStatus = b.Match.Status.ToString(),
                    Status = b.Status.ToString(),
                    Result = b.Result.ToString(),
                    Type = b.Match.Type.ToString(),

                    ShowWhoQualifies = tournament.AllowWhoQualifiesBets
                }).ToList();

                return betDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching bets for user {userId}, tournament {tournamentId}, status {status}, stage {stage}");
                return new List<BetDto>();
            }
        }

        public async Task RecalculateBetsForMatchAsync(int matchId)
        {
            var match = await _context.CustomMatches
                .Include(m => m.Tournament)
                .Include(m => m.Bets)
                .FirstOrDefaultAsync(m => m.MatchId == matchId);

            if (match == null)
            {
                _logger.LogWarning($"Match {matchId} not found.");
                return;
            }

            if (match.Status != CustomMatch.MatchStatus.Finished)
            {
                _logger.LogWarning($"Match {matchId} is not finished. Cannot recalculate bets.");
                return;
            }

            var tournamentSettings = await _context.CustomTournaments
                .Where(t => t.TournamentId == match.TournamentId)
                .Select(t => new
                {
                    t.AllowExactResultBonus,
                    t.ExactResultBonusCalculation,
                    t.ExactResultBonus,
                    t.AllowNonSubmittedBetsPenalty,
                    t.NonSubmittedBetPenalty
                })
                .FirstOrDefaultAsync();

            if (tournamentSettings == null)
            {
                _logger.LogWarning($"Tournament settings not found for match {matchId}.");
                return;
            }

            decimal homeWinOdds = match.HomeWinOdds;
            decimal drawOdds = match.DrawOdds;
            decimal awayWinOdds = match.AwayWinOdds;
            decimal? homeQualifiesOdds = match.HomeQualifies;
            decimal? awayQualifiesOdds = match.AwayQualifies;

            int homeScore = match.HomeScore ?? -1;
            int awayScore = match.AwayScore ?? -1;
            bool isDraw = homeScore == awayScore;
            bool homeWin = homeScore > awayScore;
            bool awayWin = homeScore < awayScore;
            bool hasQualification = match.Type == CustomMatch.MatchType.ExtendedWithQualification;
            bool homeQualified = hasQualification && match.Qualified == CustomMatch.TeamQualified.Home;
            bool awayQualified = hasQualification && match.Qualified == CustomMatch.TeamQualified.Away;

            foreach (var bet in match.Bets)
            {
                bet.BasePayout = 0;
                bet.QualificationPayout = 0;
                bet.ExactScorePayout = 0;

                bool won = false;
                bool isBetValid = bet.HomeGoals.HasValue && bet.AwayGoals.HasValue;

                // 1X2 Outcome
                if (isBetValid)
                {
                    if (homeWin && bet.HomeGoals > bet.AwayGoals)
                    {
                        bet.BasePayout = bet.BaseAmount * homeWinOdds;
                        won = true;
                    }
                    else if (isDraw && bet.HomeGoals == bet.AwayGoals)
                    {
                        bet.BasePayout = bet.BaseAmount * drawOdds;
                        won = true;
                    }
                    else if (awayWin && bet.AwayGoals > bet.HomeGoals)
                    {
                        bet.BasePayout = bet.BaseAmount * awayWinOdds;
                        won = true;
                    }
                }

                // Qualification Bet
                if (hasQualification && bet.Qualified.HasValue)
                {
                    if (bet.Qualified == CustomMatch.TeamQualified.Home && homeQualified && homeQualifiesOdds.HasValue)
                    {
                        bet.QualificationPayout = bet.BaseAmount * homeQualifiesOdds.Value;
                        won = true;
                    }
                    else if (bet.Qualified == CustomMatch.TeamQualified.Away && awayQualified && awayQualifiesOdds.HasValue)
                    {
                        bet.QualificationPayout = bet.BaseAmount * awayQualifiesOdds.Value;
                        won = true;
                    }
                }

                // Exact Score
                if (tournamentSettings.AllowExactResultBonus && isBetValid &&
                    bet.HomeGoals == homeScore && bet.AwayGoals == awayScore)
                {
                    decimal winningOdd = homeWin ? homeWinOdds : awayWin ? awayWinOdds : drawOdds;

                    if (tournamentSettings.ExactResultBonus.HasValue)
                    {
                        bet.ExactScorePayout = tournamentSettings.ExactResultBonusCalculation switch
                        {
                            ExactResultBonusCalculationType.Fixed => tournamentSettings.ExactResultBonus.Value,
                            ExactResultBonusCalculationType.Multiplied => winningOdd * tournamentSettings.ExactResultBonus.Value,
                            _ => 0
                        };
                        won = true;
                    }
                }

                // Penalty for not submitting
                if (tournamentSettings.AllowNonSubmittedBetsPenalty && tournamentSettings.NonSubmittedBetPenalty.HasValue)
                {
                    var penalty = tournamentSettings.NonSubmittedBetPenalty.Value;
                    bet.BasePayout -= penalty;
                }

                // Finalize Bet Status
                bet.Status = Bet.BetStatus.Closed;
                bet.Calculated = true;
                bet.Result = won ? Bet.BetResult.Won : Bet.BetResult.Lost;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Bets recalculated for match {matchId}.");
        }

        public async Task<bool> RecalculateBetsForTournamentAsync(int tournamentId)
        {
            var tournament = await _context.CustomTournaments
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Bets)
                .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

            if (tournament == null || !tournament.Matches.Any())
            {
                _logger.LogWarning($"No matches found for tournament ID {tournamentId}");
                return false;
            }

            foreach (var match in tournament.Matches.Where(m => m.Status == CustomMatch.MatchStatus.Finished))
            {
                await RecalculateBetsForMatchAsync(match.MatchId);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task GenerateBetsForNewMatchAsync(int matchId, int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Generating bets for match {matchId} in tournament {tournamentId}");

                // Step 1: Get all users assigned to this tournament
                var assignedUsers = await _context.CustomTournamentUserAssignments
                    .Where(a => a.TournamentId == tournamentId)
                    .Select(a => a.UserId)
                    .ToListAsync();

                if (!assignedUsers.Any())
                {
                    _logger.LogWarning($"No users assigned to tournament {tournamentId}. Skipping bet generation.");
                    return;
                }

                // Step 2: Load existing bets for this match into memory
                var existingBets = await _context.Bets
                    .Where(b => b.MatchId == matchId)
                    .Select(b => b.UserId)
                    .ToListAsync();

                var existingUserSet = new HashSet<string>(existingBets);

                // Step 3: Create new bets only for users without one
                var newBets = assignedUsers
                    .Where(userId => !existingUserSet.Contains(userId))
                    .Select(userId => new Bet
                    {
                        MatchId = matchId,
                        UserId = userId,
                        BaseAmount = 1,
                        BonusAmount = null,
                        HomeGoals = null,
                        AwayGoals = null,
                        Qualified = null,
                        Status = Bet.BetStatus.ToPlace,
                        Result = Bet.BetResult.Pending,
                        Calculated = false,
                        BasePayout = null,
                        QualificationPayout = null,
                        ExactScorePayout = null
                    })
                    .ToList();

                // Step 4: Save if needed
                if (newBets.Any())
                {
                    await _context.Bets.AddRangeAsync(newBets);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Inserted {newBets.Count} new bets for match {matchId}.");
                }
                else
                {
                    _logger.LogInformation($"All users already have bets for match {matchId}. Nothing to add.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating bets for match {matchId} in tournament {tournamentId}");
                throw;
            }
        }

        public async Task<BetStatsDto?> GetBetStatisticsAsync(int matchId, string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching bet statistics for matchId: {matchId} and userId: {userId}");

                // Fetch match with related data
                var match = await _context.CustomMatches
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Include(m => m.Bets)
                        .ThenInclude(b => b.User)
                    .FirstOrDefaultAsync(m => m.MatchId == matchId);

                if (match == null)
                {
                    _logger.LogWarning($"Match with ID {matchId} not found.");
                    return null;
                }

                // Check if user is assigned to the tournament
                var isUserAssigned = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == match.TournamentId && a.UserId == userId);

                if (!isUserAssigned)
                {
                    _logger.LogWarning($"User {userId} is not assigned to tournament ID {match.TournamentId}");
                    return null;
                }

                var tournament = await _context.CustomTournaments
                    .FirstOrDefaultAsync(t => t.TournamentId == match.TournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament with ID {match.TournamentId} not found.");
                    return null;
                }

                var userAssignments = await _context.CustomTournamentUserAssignments
                    .Where(a => a.TournamentId == match.TournamentId && a.Status == AssignmentStatus.Accepted)
                    .ToListAsync();

                var userIdToUsername = userAssignments.ToDictionary(a => a.UserId, a => a.UserName);
                var acceptedUserIds = userIdToUsername.Keys.ToHashSet();

                // Filter bets to only include accepted participants who placed bets.
                var bets = match.Bets
                    .Where(b => acceptedUserIds.Contains(b.UserId) && b.HomeGoals.HasValue && b.AwayGoals.HasValue)
                    .ToList();
                var qualificationBets = match.Bets
                    .Where(b => acceptedUserIds.Contains(b.UserId) && b.Qualified.HasValue)
                    .ToList();

                var totalBets = bets.Count;
                var totalQualificationBets = qualificationBets.Count;
                var totalParticipants = acceptedUserIds.Count;

                string? result = (match.HomeScore.HasValue && match.AwayScore.HasValue)
                    ? match.HomeScore > match.AwayScore ? "1"
                    : match.HomeScore < match.AwayScore ? "2"
                    : "X"
                    : null;

                string? resultQualified = match.Qualified.ToString();

                bool showQualified = tournament.AllowWhoQualifiesBets &&
                                     match.Type == CustomMatch.MatchType.ExtendedWithQualification;
                bool showExactResult = tournament.AllowExactResultBonus;

                // Create DTO
                var betStats = new BetStatsDto
                {
                    ShowQualified = showQualified,
                    ShowExactResult = showExactResult,
                    MatchStatus = match.Status.ToString(),

                    HomeTeam = match.HomeTeam.TeamName,
                    AwayTeam = match.AwayTeam.TeamName,
                    HomeTeamCrestUrl = match.HomeTeam.CrestUrl,
                    AwayTeamCrestUrl = match.AwayTeam.CrestUrl,
                    HomeScoreActual = match.HomeScore,
                    AwayScoreActual = match.AwayScore,
                    Result = result,
                    ResultQualified = resultQualified,

                    // 1X2 Outcome Percentages
                    Percent1 = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals > b.AwayGoals) / totalBets * 100, 2) : 0,
                    PercentX = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals == b.AwayGoals) / totalBets * 100, 2) : 0,
                    Percent2 = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals < b.AwayGoals) / totalBets * 100, 2) : 0,
                    PlacedBetsCount = totalBets,
                    ParticipantsCount = totalParticipants,
                    AverageHomeGoals = totalBets > 0 ? Math.Round(bets.Average(b => (decimal)b.HomeGoals!.Value), 1) : null,
                    AverageAwayGoals = totalBets > 0 ? Math.Round(bets.Average(b => (decimal)b.AwayGoals!.Value), 1) : null,

                    // Qualification Betting Percentages
                    Percent1Q = totalQualificationBets > 0 ? Math.Round((decimal)qualificationBets.Count(b => b.Qualified == CustomMatch.TeamQualified.Home) / totalQualificationBets * 100, 2) : null,
                    Percent2Q = totalQualificationBets > 0 ? Math.Round((decimal)qualificationBets.Count(b => b.Qualified == CustomMatch.TeamQualified.Away) / totalQualificationBets * 100, 2) : null,

                    // User Bets
                    UserBets = (match.Status == CustomMatch.MatchStatus.In_Play ||
                                match.Status == CustomMatch.MatchStatus.Finished)
                        ? match.Bets
                        .Where(b => (b.Status == Bet.BetStatus.Placed || b.Status == Bet.BetStatus.Closed) &&
                                    userIdToUsername.ContainsKey(b.UserId))
                        .Select(b => new UserBetDto
                        {
                            Username = userIdToUsername.TryGetValue(b.UserId, out var username) ? username : "Unknown",
                            BetScore = b.HomeGoals.HasValue && b.AwayGoals.HasValue ? $"{b.HomeGoals}-{b.AwayGoals}" : "-",

                            // Assign only if the user placed a bet
                            HomeWinSuccess = (b.HomeGoals.HasValue && b.AwayGoals.HasValue && b.HomeGoals > b.AwayGoals)
                                ? (result == "1" ? 1 : 0) : null,

                            DrawSuccess = (b.HomeGoals.HasValue && b.AwayGoals.HasValue && b.HomeGoals == b.AwayGoals)
                                ? (result == "X" ? 1 : 0) : null,

                            AwayWinSuccess = (b.HomeGoals.HasValue && b.AwayGoals.HasValue && b.HomeGoals < b.AwayGoals)
                                ? (result == "2" ? 1 : 0) : null,

                            // Qualification bets - only if the user placed a qualification bet
                            HomeQualifiesSuccess = b.Qualified == CustomMatch.TeamQualified.Home
                                ? (match.Qualified == CustomMatch.TeamQualified.Home ? 1 : 0)
                                : (b.Qualified == CustomMatch.TeamQualified.Away ? null : null),

                            AwayQualifiesSuccess = b.Qualified == CustomMatch.TeamQualified.Away
                                ? (match.Qualified == CustomMatch.TeamQualified.Away ? 1 : 0)
                                : (b.Qualified == CustomMatch.TeamQualified.Home ? null : null),

                            // Determine result success (only if the user placed a score prediction)
                            ResultSuccess = (b.HomeGoals.HasValue && b.AwayGoals.HasValue &&
                                 match.HomeScore.HasValue && match.AwayScore.HasValue &&
                                 b.HomeGoals == match.HomeScore &&
                                 b.AwayGoals == match.AwayScore)
                                 ? 1 : null

                        }).ToList() : null
                };

                _logger.LogInformation($"Bet statistics for match ID {matchId} successfully retrieved.");
                return betStats;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving bet statistics for match ID {matchId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<UpcomingBetDto>> GetUpcomingBetsAsync(int tournamentId, string userId, int? limit = null)
        {
            try
            {
                int maxResults = limit ?? int.MaxValue; // If limit is null, fetch all notifications

                _logger.LogInformation($"Fetching upcoming bets for user {userId} in tournament {tournamentId}");

                var nowUtc = DateTime.UtcNow;

                var upcomingBets = await WhereMatchOpenForBetting(_context.Bets, nowUtc)
                    .Where(b => b.Match.TournamentId == tournamentId &&
                                b.UserId == userId &&
                                b.Status == Bet.BetStatus.ToPlace &&
                                b.Match.IsVisible)
                    .OrderBy(b => b.Match.MatchStart)
                    .Take(maxResults)
                    .Select(b => new UpcomingBetDto
                    {
                        MatchId = b.Match.MatchId,
                        HomeTeam = b.Match.HomeTeam.TeamName,
                        AwayTeam = b.Match.AwayTeam.TeamName,
                        MatchTime = DateTime.SpecifyKind(b.Match.MatchStart, DateTimeKind.Utc),
                        Stage = b.Match.Stage.StageName
                    })
                    .ToListAsync();

                return upcomingBets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching upcoming bets for tournament {tournamentId} and user {userId}");
                throw;
            }
        }

        public async Task MarkBetsAsCompletedForMatchAsync(int matchId)
        {
            try
            {
                var betsToClose = await _context.Bets
                    .Where(b => b.MatchId == matchId &&
                                (b.Status == Bet.BetStatus.ToPlace || b.Status == Bet.BetStatus.Placed))
                    .ToListAsync();

                if (!betsToClose.Any())
                {
                    _logger.LogInformation($"No bets to mark as completed for match {matchId}.");
                    return;
                }

                foreach (var bet in betsToClose)
                {
                    bet.Status = Bet.BetStatus.Closed;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully marked {betsToClose.Count} bets as completed for match {matchId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking bets as completed for match {matchId}.");
                throw;
            }
        }
    }
}
