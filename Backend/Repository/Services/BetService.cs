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
                                QualifiedTeam = null, // No qualification prediction yet
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
                    Enum.TryParse<Bet.Team>(betUpdateDto.QualifiedTeam, true, out var qualifiedTeamEnum))
                {
                    bet.QualifiedTeam = qualifiedTeamEnum;
                }
                else
                {
                    bet.QualifiedTeam = null;
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

        public async Task<List<BetDto>> GetBetsByStatusAsync(int tournamentId, string userId, Bet.BetStatus status)
        {
            try
            {
                _logger.LogInformation($"Fetching bets for user {userId}, tournament {tournamentId}, status {status}");

                var bets = await _context.Bets
                    .Include(b => b.Match)
                        .ThenInclude(m => m.HomeTeam)
                    .Include(b => b.Match)
                        .ThenInclude(m => m.AwayTeam)
                    .Where(b => b.Match.TournamentId == tournamentId && b.UserId == userId && b.Status == status)
                    .OrderBy(b => b.Match.MatchStart)
                    .ToListAsync();

                if (!bets.Any())
                {
                    _logger.LogWarning($"No bets found for user {userId}, tournament {tournamentId}, status {status}");
                    return new List<BetDto>();
                }

                var betDtos = bets.Select(b => new BetDto
                {
                    BetId = b.BetId,
                    MatchId = b.MatchId,
                    TeamHome = b.Match.HomeTeam.Name,
                    TeamAway = b.Match.AwayTeam.Name,
                    StartTime = b.Match.MatchStart,

                    BaseAmount = b.BaseAmount,
                    BonusAmount = b.BonusAmount,

                    PlayerHomeGoals = b.HomeGoals,
                    PlayerAwayGoals = b.AwayGoals,
                    PlayerQualifiedTeam = b.QualifiedTeam?.ToString(),
                    ActualHomeGoals = b.Match.HomeScore,
                    ActualAwayGoals = b.Match.AwayScore,
                    ActualQualifiedTeam = b.Match.Qualified.ToString(),

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
                _logger.LogError(ex, $"Error fetching bets for user {userId} and tournament {tournamentId}");
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

                // Process 1X2 Bet Outcome
                if (homeWin && bet.HomeGoals > bet.AwayGoals)
                {
                    payout += bet.BaseAmount * homeWinOdds;
                    won = true;
                }
                else if (isDraw && bet.HomeGoals == bet.AwayGoals)
                {
                    payout += bet.BaseAmount * drawOdds;
                    won = true;
                }
                else if (awayWin && bet.AwayGoals > bet.HomeGoals)
                {
                    payout += bet.BaseAmount * awayWinOdds;
                    won = true;
                }

                // Process Qualification Bet
                if (hasQualification && bet.QualifiedTeam.HasValue)
                {
                    if (bet.QualifiedTeam == Bet.Team.Home && homeQualified && homeQualifiesOdds.HasValue)
                    {
                        payout += bet.BaseAmount * homeQualifiesOdds.Value;
                        won = true;
                    }
                    else if (bet.QualifiedTeam == Bet.Team.Away && awayQualified && awayQualifiesOdds.HasValue)
                    {
                        payout += bet.BaseAmount * awayQualifiesOdds.Value;
                        won = true;
                    }
                }

                // Process Exact Result Bonus
                if (tournamentSettings.AllowExactResultBonus &&
                    bet.HomeGoals == homeScore && bet.AwayGoals == awayScore)
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

        public async Task AutoUpdateBetStatusAsync()
        {
            try
            {
                _logger.LogInformation("Starting automatic update of bet statuses...");

                // Step 1: Get all matches that are in the past AND are not Onhoing
                var finalisedMatches = await _context.CustomMatches
                    .Where(m => m.Status == CustomMatch.MatchStatus.Finalised ||
                                (m.MatchStart < DateTime.UtcNow && m.Status != CustomMatch.MatchStatus.Upcoming))
                    .Select(m => m.MatchId)
                    .ToListAsync();

                if (!finalisedMatches.Any())
                {
                    _logger.LogInformation("No finalised matches found in the past. No bets updated.");
                    return;
                }

                // Step 2: Get all bets related to those matches that are NOT already Finalised
                var betsToUpdate = await _context.Bets
                    .Where(b => finalisedMatches.Contains(b.MatchId) && b.Status != Bet.BetStatus.Finalised)
                    .ToListAsync();

                if (!betsToUpdate.Any())
                {
                    _logger.LogInformation("No bets found that need status updates.");
                    return;
                }

                // Step 3: Update the status of those bets to Finalised
                foreach (var bet in betsToUpdate)
                {
                    bet.Status = Bet.BetStatus.Finalised;
                }

                // Step 4: Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully updated {betsToUpdate.Count} bets to Finalised.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating bet statuses.");
            }
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
                        QualifiedTeam = null,
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
                var qualificationBets = match.Bets.Where(b => b.QualifiedTeam.HasValue).ToList();

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
                    HomeTeam = match.HomeTeam.Name,
                    AwayTeam = match.AwayTeam.Name,
                    HomeScoreActual = match.HomeScore,
                    AwayScoreActual = match.AwayScore,
                    Result = result,
                    ResultQualified = resultQualified,

                    // 1X2 Outcome Percentages
                    Percent1 = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals > b.AwayGoals) / totalBets * 100, 2) : 0,
                    PercentX = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals == b.AwayGoals) / totalBets * 100, 2) : 0,
                    Percent2 = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals < b.AwayGoals) / totalBets * 100, 2) : 0,

                    // Qualification Betting Percentages
                    Percent1Q = totalQualificationBets > 0 ? Math.Round((decimal)qualificationBets.Count(b => b.QualifiedTeam == Bet.Team.Home) / totalQualificationBets * 100, 2) : null,
                    Percent2Q = totalQualificationBets > 0 ? Math.Round((decimal)qualificationBets.Count(b => b.QualifiedTeam == Bet.Team.Away) / totalQualificationBets * 100, 2) : null,

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
                            HomeQualifiesSuccess = (b.QualifiedTeam.HasValue)
                                ? (resultQualified == "home" ? (b.QualifiedTeam == Bet.Team.Home ? 1 : 0) : null)
                                : null,

                            AwayQualifiesSuccess = (b.QualifiedTeam.HasValue)
                                ? (resultQualified == "away" ? (b.QualifiedTeam == Bet.Team.Away ? 1 : 0) : null)
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

        public async Task<List<UpcomingBetDto>> GetUpcomingBetsAsync(int tournamentId, string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching upcoming bets for user {userId} in tournament {tournamentId}");

                var upcomingBets = await _context.Bets
                    .Where(b => b.Match.TournamentId == tournamentId &&
                                b.UserId == userId &&
                                b.Status == Bet.BetStatus.ToPlace)
                    .OrderBy(b => b.Match.MatchStart)
                    .Take(5)
                    .Select(b => new UpcomingBetDto
                    {
                        MatchId = b.Match.MatchId,
                        HomeTeam = b.Match.HomeTeam.Name,
                        AwayTeam = b.Match.AwayTeam.Name,
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
