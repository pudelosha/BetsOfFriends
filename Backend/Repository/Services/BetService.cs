using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
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
    }
}
