using Backend.Model.Database;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Services
{
    public class TournamentSelectionService : ITournamentSelectionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TournamentSelectionService> _logger;

        public TournamentSelectionService(AppDbContext context, ILogger<TournamentSelectionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> SetSelectedTournamentAsync(string userId, int tournamentId)
        {
            var userAssignments = await _context.CustomTournamentUserAssignments
                .Where(a => a.UserId == userId)
                .ToListAsync();

            if (!userAssignments.Any(a => a.TournamentId == tournamentId))
            {
                _logger.LogWarning($"User {userId} tried to select an unassigned tournament {tournamentId}.");
                return false; // User doesn't have this tournament
            }

            foreach (var assignment in userAssignments)
            {
                assignment.IsSelected = assignment.TournamentId == tournamentId;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"User {userId} set tournament {tournamentId} as selected.");
            return true;
        }

        public async Task<int?> GetSelectedTournamentAsync(string userId)
        {
            var selectedTournament = await _context.CustomTournamentUserAssignments
                .Where(a => a.UserId == userId && a.IsSelected)
                .Select(a => a.TournamentId)
                .FirstOrDefaultAsync();

            return selectedTournament != 0 ? selectedTournament : null;
        }
    }
}
