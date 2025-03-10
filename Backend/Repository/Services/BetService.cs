using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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
                _logger.LogInformation($"Generating bets for tournament ID: {tournamentId}");

                // Step 1: Fetch all matches for the tournament
                var matches = await _context.CustomMatches
                    .Where(m => m.TournamentId == tournamentId)
                    .ToListAsync();

                if (!matches.Any())
                {
                    _logger.LogWarning($"No matches found for tournament ID: {tournamentId}. No bets will be created.");
                    return;
                }

                // Step 2: Fetch all users participating in this tournament
                var users = await _context.CustomTournamentUserAssignments
                    .Where(u => u.TournamentId == tournamentId)
                    .Select(u => u.UserId) // Select only user IDs
                    .ToListAsync();

                if (!users.Any())
                {
                    _logger.LogWarning($"No participants found for tournament ID: {tournamentId}. No bets will be created.");
                    return;
                }

                // Step 3: Create bet entries for each match and each user
                var bets = new List<Bet>();

                foreach (var match in matches)
                {
                    foreach (var userId in users)
                    {
                        bets.Add(new Bet
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

                // Step 4: Bulk insert all bets for efficiency
                await _context.Bets.AddRangeAsync(bets);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation($"Successfully created {bets.Count} bets for tournament ID: {tournamentId}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error creating bets for tournament ID {tournamentId}: {ex.Message}");
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
                    ActualHomeGoals = b.Match.HomeScore,
                    ActualAwayGoals = b.Match.AwayScore,

                    HomeOdds = b.Match.HomeWinOdds,
                    DrawOdds = b.Match.DrawOdds,
                    AwayOdds = b.Match.AwayWinOdds,

                    QualifyHomeOdds = b.Match.HomeQualifies,
                    QualifyAwayOdds = b.Match.AwayQualifies,

                    QualifiedTeam = b.QualifiedTeam?.ToString(),
                    Status = b.Status.ToString(),
                    Result = b.Result.ToString()
                }).ToList();

                return betDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching bets for user {userId} and tournament {tournamentId}");
                return new List<BetDto>();
            }
        }

        public async Task<bool> CalculateBetsAsync(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Calculating bets for tournament {tournamentId}");

                var matches = await _context.CustomMatches
                    .Include(m => m.Tournament)
                    .Where(m => m.TournamentId == tournamentId)
                    .ToListAsync();

                if (!matches.Any())
                {
                    _logger.LogWarning($"No matches found for tournament {tournamentId}");
                    return false;
                }

                foreach (var match in matches)
                {
                    var bets = await _context.Bets.Where(b => b.MatchId == match.MatchId).ToListAsync();

                    foreach (var bet in bets)
                    {
                        // Check if the bet is correct and update payout accordingly
                        if (bet.HomeGoals == match.HomeScore && bet.AwayGoals == match.AwayScore)
                        {
                            bet.Payout = bet.BaseAmount * 3; // Example: Triple the bet amount for correct score
                            bet.Result = Bet.BetResult.Won;
                        }
                        else
                        {
                            bet.Payout = 0;
                            bet.Result = Bet.BetResult.Lost;
                        }

                        bet.Status = Bet.BetStatus.Finalised; // Mark as finalised
                    }

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"Bet calculations completed for tournament {tournamentId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating bets for tournament {tournamentId}");
                return false;
            }
        }

        public async Task AutoUpdateBetStatusAsync()
        {
            try
            {
                _logger.LogInformation("Starting automatic update of bet statuses...");

                // Step 1: Get all matches that are in the past AND are not Onhoing
                var finalisedMatches = await _context.CustomMatches
                    .Where(m => m.MatchStart < DateTime.UtcNow && m.Status != CustomMatch.MatchStatus.Upcoming)
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

                // Step 2: Check if bets already exist for this match (to avoid duplicates)
                var existingBets = await _context.Bets
                    .Where(b => b.MatchId == matchId)
                    .Select(b => b.UserId)
                    .ToListAsync();

                var newBets = assignedUsers
                    .Where(userId => !existingBets.Contains(userId)) // Add only if the user has no bet for this match
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

                // Step 3: Save new bets
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
            }
        }

        public async Task<BetStatsDto?> GetBetStatisticsAsync(int matchId)
        {

            //TODO check userId

            try
            {
                _logger.LogInformation($"Fetching bet statistics for matchId: {matchId}");

                var match = await _context.CustomMatches
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Include(m => m.Bets)
                    .FirstOrDefaultAsync(m => m.MatchId == matchId);

                if (match == null)
                {
                    _logger.LogWarning($"Match with ID {matchId} not found.");
                    return null;
                }

                var bets = match.Bets.Where(b => b.HomeGoals.HasValue && b.AwayGoals.HasValue).ToList();
                var qualificationBets = match.Bets.Where(b => b.QualifiedTeam.HasValue).ToList();

                var totalBets = bets.Count;
                var totalQualificationBets = qualificationBets.Count;

                var betStats = new BetStatsDto
                {
                    HomeTeam = match.HomeTeam.Name,
                    AwayTeam = match.AwayTeam.Name,
                    HomeScoreActual = match.HomeScore,
                    AwayScoreActual = match.AwayScore,

                    // Calculate % of users betting on 1X2 outcomes based on their predicted goals
                    Percent1 = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals > b.AwayGoals) / totalBets * 100, 2) : 0,
                    PercentX = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals == b.AwayGoals) / totalBets * 100, 2) : 0,
                    Percent2 = totalBets > 0 ? Math.Round((decimal)bets.Count(b => b.HomeGoals < b.AwayGoals) / totalBets * 100, 2) : 0,

                    // Qualification Bets (Calculated based on user selections)
                    Percent1Q = totalQualificationBets > 0 ? Math.Round((decimal)qualificationBets.Count(b => b.QualifiedTeam == Bet.Team.Home) / totalQualificationBets * 100, 2) : null,
                    Percent2Q = totalQualificationBets > 0 ? Math.Round((decimal)qualificationBets.Count(b => b.QualifiedTeam == Bet.Team.Away) / totalQualificationBets * 100, 2) : null,

                    // Actual Result Based on Match Score (Handle missing match results)
                    Result = (match.HomeScore.HasValue && match.AwayScore.HasValue)
                        ? match.HomeScore > match.AwayScore ? "1"
                        : match.HomeScore < match.AwayScore ? "2"
                        : "X"
                        : null,

                    // Correctly Calculate the Result of Who Qualified
                    ResultQualified = (match.HomeScore.HasValue && match.AwayScore.HasValue && match.HomeQualifies.HasValue && match.AwayQualifies.HasValue)
                        ? match.HomeQualifies > match.AwayQualifies ? "home"
                        : match.AwayQualifies > match.HomeQualifies ? "away"
                        : null
                        : null
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

    }
}
