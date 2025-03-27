using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
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

                // Step 2: Fetch all users participating in this tournament
                var users = await _context.CustomTournamentUserAssignments
                    .Where(u => u.TournamentId == tournamentId) // && u.Status == AssignmentStatus.Accepted
                    .Select(u => u.UserId)
                    .ToListAsync();

                if (!users.Any())
                {
                    _logger.LogWarning($"No participants found for tournament ID: {tournamentId}. Skipping bet creation.");
                    return;
                }

                // Step 3: Fetch existing bets to prevent duplicates
                var existingBets = await _context.Bets
                    .Where(b => matches.Select(m => m.MatchId).Contains(b.MatchId) && users.Contains(b.UserId))
                    .Select(b => new { b.MatchId, b.UserId }) // Select only match-user pairs
                    .ToListAsync();

                var existingBetPairs = new HashSet<(int MatchId, string UserId)>(existingBets.Select(b => (b.MatchId, b.UserId)));

                // Step 4: Create missing bets
                var betsToInsert = new List<Bet>();

                foreach (var match in matches)
                {
                    foreach (var userId in users)
                    {
                        if (!existingBetPairs.Contains((match.MatchId, userId))) // Only add if the bet doesn't already exist
                        {
                            betsToInsert.Add(new Bet
                            {
                                MatchId = match.MatchId,
                                UserId = userId,
                                BaseAmount = 1, // Default bet amount
                                BonusAmount = null, // No bonus initially
                                HomeGoals = null, // No score prediction yet
                                AwayGoals = null,
                                Qualified = null, // No qualification prediction yet
                                Status = Bet.BetStatus.ToPlace, // New Status: Bet must be placed
                                Result = Bet.BetResult.Pending, // Bet starts as "Pending"
                                Submitted = false, // Bet is unsubmitted initially
                                Payout = null // No payout calculated yet
                            });
                        }
                    }
                }

                // Step 5: Bulk insert all missing bets for efficiency
                if (betsToInsert.Count > 0)
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

                // Block update if match has already started
                if (bet.Match.MatchStart <= DateTime.UtcNow)
                {
                    _logger.LogWarning($"User {userId} attempted to update Bet ID {betId}, but the match has already started.");
                    return false; // Prevent bet modification after match start
                }

                // Update bet details
                bet.BaseAmount = betUpdateDto.BaseAmount;
                bet.BonusAmount = betUpdateDto.BonusAmount;
                bet.HomeGoals = betUpdateDto.HomeGoals;
                bet.AwayGoals = betUpdateDto.AwayGoals;
                bet.Submitted = true; // TODO: Confirm if necessary
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

                // Step 4: Fetch bets with the given criteria (tournament, user, status, and stage)
                var bets = await _context.Bets
                    .Include(b => b.Match)
                        .ThenInclude(m => m.HomeTeam)
                    .Include(b => b.Match)
                        .ThenInclude(m => m.AwayTeam)
                    .Where(b =>
                        b.Match.TournamentId == tournamentId &&
                        b.UserId == userId &&
                        b.Status == betStatus &&
                        b.Match.Stage.StageName == stage)
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
                    StartTime = b.Match.MatchStart,

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

                    Status = b.Status.ToString(),
                    Result = b.Result.ToString(),
                    Type = b.Match.Type.ToString()
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

            if (match.Status != CustomMatch.MatchStatus.Finalised)
            {
                _logger.LogWarning($"Match {matchId} is not finalised. Cannot recalculate bets.");
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
                decimal payout = 0;
                bool won = false;

                // Ensure the bet has valid numbers before checking conditions
                bool isBetValid = bet.HomeGoals.HasValue && bet.AwayGoals.HasValue;

                // Process 1X2 Bet Outcome only if valid goals are provided
                if (isBetValid)
                {
                    if (homeWin && bet.HomeGoals.Value > bet.AwayGoals.Value)
                    {
                        payout += bet.BaseAmount * homeWinOdds;
                        won = true;
                    }
                    else if (isDraw && bet.HomeGoals.Value == bet.AwayGoals.Value)
                    {
                        payout += bet.BaseAmount * drawOdds;
                        won = true;
                    }
                    else if (awayWin && bet.AwayGoals.Value > bet.HomeGoals.Value)
                    {
                        payout += bet.BaseAmount * awayWinOdds;
                        won = true;
                    }
                }

                // Process Qualification Bet
                if (hasQualification && bet.Qualified.HasValue)
                {
                    if (bet.Qualified == CustomMatch.TeamQualified.Home && homeQualified && homeQualifiesOdds.HasValue)
                    {
                        payout += bet.BaseAmount * homeQualifiesOdds.Value;
                        won = true;
                    }
                    else if (bet.Qualified == CustomMatch.TeamQualified.Away && awayQualified && awayQualifiesOdds.HasValue)
                    {
                        payout += bet.BaseAmount * awayQualifiesOdds.Value;
                        won = true;
                    }
                }

                // Process Exact Result Bonus only if the bet is valid
                if (tournamentSettings.AllowExactResultBonus && isBetValid &&
                    bet.HomeGoals.Value == homeScore && bet.AwayGoals.Value == awayScore)
                {
                    decimal winningOdd = 0;

                    // Determine the correct winning odd
                    if (homeScore > awayScore)
                        winningOdd = homeWinOdds;
                    else if (homeScore < awayScore)
                        winningOdd = awayWinOdds;
                    else
                        winningOdd = drawOdds;

                    if (tournamentSettings.ExactResultBonus.HasValue)
                    {
                        if (tournamentSettings.ExactResultBonusCalculation == ExactResultBonusCalculationType.Fixed)
                        {
                            payout += tournamentSettings.ExactResultBonus.Value;
                        }
                        else if (tournamentSettings.ExactResultBonusCalculation == ExactResultBonusCalculationType.Multiplied)
                        {
                            payout += winningOdd * tournamentSettings.ExactResultBonus.Value;
                        }
                    }
                }

                // Apply Non-Submitted Bet Penalty
                if (tournamentSettings.AllowNonSubmittedBetsPenalty && !bet.Submitted &&
                    tournamentSettings.NonSubmittedBetPenalty.HasValue)
                {
                    payout -= tournamentSettings.NonSubmittedBetPenalty.Value;
                }

                // Finalize Bet Status
                bet.Status = Bet.BetStatus.Finalised;
                bet.Result = won ? Bet.BetResult.Won : Bet.BetResult.Lost;
                bet.Payout = payout;
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

            foreach (var match in tournament.Matches.Where(m => m.Status == CustomMatch.MatchStatus.Finalised))
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

                // Step 1: Get all active users assigned to this tournament
                var assignedUsers = await _context.CustomTournamentUserAssignments
                    .Where(a => a.TournamentId == tournamentId)
                    .Select(a => a.UserId)
                    .ToListAsync();

                if (!assignedUsers.Any())
                {
                    _logger.LogWarning($"No users assigned to tournament {tournamentId}. Skipping bet generation.");
                    return;
                }

                // Step 2: Check existing bets to avoid duplicates
                var existingBets = await _context.Bets
                    .Where(b => b.MatchId == matchId)
                    .Select(b => b.UserId)
                    .ToHashSetAsync(); // Uses HashSet for fast lookup

                // Step 3: Generate missing bets only
                var newBets = assignedUsers
                    .Where(userId => !existingBets.Contains(userId)) // Avoids inserting duplicates
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
                        Submitted = false
                    }).ToList();

                // Step 4: Save new bets if any
                if (newBets.Any())
                {
                    await _context.Bets.AddRangeAsync(newBets);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Successfully generated {newBets.Count} bets for match {matchId}.");
                }
                else
                {
                    _logger.LogInformation($"No new bets were added for match {matchId}, all users already have bets.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while generating bets for match {matchId}");
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

                // Filter bets to only include placed bets
                var bets = match.Bets.Where(b => b.HomeGoals.HasValue && b.AwayGoals.HasValue).ToList();
                var qualificationBets = match.Bets.Where(b => b.Qualified.HasValue).ToList();

                var totalBets = bets.Count;
                var totalQualificationBets = qualificationBets.Count;

                string? result = (match.HomeScore.HasValue && match.AwayScore.HasValue)
                    ? match.HomeScore > match.AwayScore ? "1"
                    : match.HomeScore < match.AwayScore ? "2"
                    : "X"
                    : null;

                string? resultQualified = match.Qualified.ToString();

                // Create DTO
                var betStats = new BetStatsDto
                {
                    HomeTeam = match.HomeTeam.TeamName,
                    AwayTeam = match.AwayTeam.TeamName,
                    HomeScoreActual = match.HomeScore,
                    AwayScoreActual = match.AwayScore,
                    Result = result,
                    ResultQualified = resultQualified,

                    // 1X2 Outcome Percentages
                    Percent1 = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals > b.AwayGoals) / totalBets * 100, 2) : 0,
                    PercentX = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals == b.AwayGoals) / totalBets * 100, 2) : 0,
                    Percent2 = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals < b.AwayGoals) / totalBets * 100, 2) : 0,

                    // Qualification Betting Percentages
                    Percent1Q = totalQualificationBets > 0 ? Math.Round((decimal)qualificationBets.Count(b => b.Qualified == CustomMatch.TeamQualified.Home) / totalQualificationBets * 100, 2) : null,
                    Percent2Q = totalQualificationBets > 0 ? Math.Round((decimal)qualificationBets.Count(b => b.Qualified == CustomMatch.TeamQualified.Away) / totalQualificationBets * 100, 2) : null,

                    // User Bets
                    UserBets = match.Status == CustomMatch.MatchStatus.Finalised ? match.Bets
                        .Where(b => b.Status == Bet.BetStatus.Finalised)
                        .Select(b => new UserBetDto
                        {
                            Username = b.User.UserName,
                            BetScore = b.HomeGoals.HasValue && b.AwayGoals.HasValue ? $"{b.HomeGoals}-{b.AwayGoals}" : "-",

                            // Assign only if the user placed a bet
                            HomeWinSuccess = (b.HomeGoals.HasValue && b.AwayGoals.HasValue && b.HomeGoals > b.AwayGoals)
                                ? (result == "1" ? 1 : 0) : null,

                            DrawSuccess = (b.HomeGoals.HasValue && b.AwayGoals.HasValue && b.HomeGoals == b.AwayGoals)
                                ? (result == "X" ? 1 : 0) : null,

                            AwayWinSuccess = (b.HomeGoals.HasValue && b.AwayGoals.HasValue && b.HomeGoals < b.AwayGoals)
                                ? (result == "2" ? 1 : 0) : null,

                            // Qualification bets - only if the user placed a qualification bet
                            HomeQualifiesSuccess = (b.Qualified.HasValue)
                                ? (resultQualified == "home" ? (b.Qualified == CustomMatch.TeamQualified.Home ? 1 : 0) : null)
                                : null,

                            AwayQualifiesSuccess = (b.Qualified.HasValue)
                                ? (resultQualified == "away" ? (b.Qualified == CustomMatch.TeamQualified.Away ? 1 : 0) : null)
                                : null,

                            // Determine result success (only if the user placed a score prediction)
                            ResultSuccess = (b.HomeGoals.HasValue && b.AwayGoals.HasValue) &&
                                            ((result == "1" && b.HomeGoals > b.AwayGoals) ||
                                             (result == "X" && b.HomeGoals == b.AwayGoals) ||
                                             (result == "2" && b.HomeGoals < b.AwayGoals))
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

                var upcomingBets = await _context.Bets
                    .Where(b => b.Match.TournamentId == tournamentId &&
                                b.UserId == userId &&
                                b.Status == Bet.BetStatus.ToPlace)
                    .OrderBy(b => b.Match.MatchStart)
                    .Take(maxResults)
                    .Select(b => new UpcomingBetDto
                    {
                        MatchId = b.Match.MatchId,
                        HomeTeam = b.Match.HomeTeam.TeamName,
                        AwayTeam = b.Match.AwayTeam.TeamName,
                        MatchTime = b.Match.MatchStart
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
    }
}
