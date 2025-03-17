using Backend.DTOs.Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Backend.Model.Entities.CustomMatch;

namespace Backend.Repository.Services
{
    public class MatchService : IMatchService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MatchService> _logger;
        private readonly IBetService _betService;
        private readonly INotificationService _notificationService;

        public MatchService(AppDbContext context, IBetService betService, ILogger<MatchService> logger, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _betService = betService;
            _notificationService = notificationService;
        }

        public async Task<List<MatchDto>> GetMatchesByStatusAsync(int tournamentId, string userId, MatchStatus status)
        {
            try
            {
                _logger.LogInformation($"Fetching matches with status {status} for tournament {tournamentId} and user {userId}");

                // Step 1: Check if the user is assigned to the tournament
                var assignment = await _context.CustomTournamentUserAssignments
                    .Where(a => a.TournamentId == tournamentId && a.UserId == userId)
                    .Select(a => new { a.Role })
                    .FirstOrDefaultAsync();

                if (assignment == null)
                {
                    _logger.LogWarning($"User {userId} is not assigned to tournament {tournamentId}");
                    return new List<MatchDto>(); // No access
                }

                // Step 2: Restrict matches if user is not an admin and IsVisible is false
                bool isAdmin = assignment.Role == UserTournamentRole.Admin;
                if (!isAdmin)
                {
                    _logger.LogWarning($"User {userId} is not allowed to see matches for tournament {tournamentId}");
                    return new List<MatchDto>(); // No access
                }

                // Step 3: Fetch matches with the given status
                var matches = await _context.CustomMatches
                    .Include(m => m.Tournament)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Where(m => m.TournamentId == tournamentId && m.Status == status)
                    .OrderBy(m => m.MatchStart)
                    .ToListAsync();

                if (!matches.Any())
                {
                    _logger.LogWarning($"No matches found with status {status} for tournament {tournamentId}");
                    return new List<MatchDto>();
                }

                // Step 4: Convert to DTO
                return matches.Select(m => new MatchDto
                {
                    MatchId = m.MatchId,
                    Stage = m.Stage.StageName,
                    HomeTeam = m.HomeTeam.TeamName,
                    AwayTeam = m.AwayTeam.TeamName,
                    MatchStart = m.MatchStart,
                    HomeScore = m.HomeScore,
                    AwayScore = m.AwayScore,
                    QualifiedTeam = m.Qualified.HasValue ? m.Qualified.ToString() : null,
                    Status = m.Status.ToString(),
                    MatchType = m.Type.ToString(),
                    IsFinished = m.Status == MatchStatus.Finalised
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching matches with status {status} for tournament {tournamentId}");
                throw;
            }
        }

        public async Task<bool> UpdateMatchResultAsync(MatchResultUpdateDto matchUpdateDto, string userId)
        {
            try
            {
                var match = await _context.CustomMatches
                    .Include(m => m.Tournament)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .FirstOrDefaultAsync(m => m.MatchId == matchUpdateDto.MatchId);

                if (match == null)
                {
                    _logger.LogWarning($"Match ID {matchUpdateDto.MatchId} not found.");
                    return false;
                }

                // Check if user is an Admin for this tournament
                var assignment = await _context.CustomTournamentUserAssignments
                    .Where(a => a.TournamentId == match.TournamentId && a.UserId == userId)
                    .Select(a => new { a.Role })
                    .FirstOrDefaultAsync();

                if (assignment == null || assignment.Role != UserTournamentRole.Admin)
                {
                    _logger.LogWarning($"User {userId} is not an Admin for tournament {match.TournamentId}");
                    return false; // Forbidden
                }

                // Update match start time if different
                if (match.MatchStart != matchUpdateDto.MatchStart)
                {
                    _logger.LogInformation($"Updating match start time for Match ID {match.MatchId}");
                    match.MatchStart = matchUpdateDto.MatchStart;
                }

                bool matchWasFinalised = false;

                // If the match is finished
                if (matchUpdateDto.IsFinished)
                {
                    _logger.LogInformation($"Finalizing match ID {match.MatchId}");

                    // Update scores
                    match.HomeScore = matchUpdateDto.HomeScore;
                    match.AwayScore = matchUpdateDto.AwayScore;

                    // If match type is ExtendedWithQualification, update qualified team
                    if (match.Type == CustomMatch.MatchType.ExtendedWithQualification)
                    {
                        if (!string.IsNullOrEmpty(matchUpdateDto.QualifiedTeam))
                        {
                            if (Enum.TryParse(matchUpdateDto.QualifiedTeam, out CustomMatch.TeamQualified qualifiedTeam))
                            {
                                match.Qualified = qualifiedTeam;
                            }
                            else
                            {
                                _logger.LogWarning($"Invalid QualifiedTeam value: {matchUpdateDto.QualifiedTeam}");
                                return false;
                            }
                        }
                        else
                        {
                            match.Qualified = null; // Reset if no selection
                        }
                    }

                    // Set match as Finalised
                    match.Status = MatchStatus.Finalised;
                    matchWasFinalised = true;
                }
                else
                {
                    // Reset match details if it's not finished
                    _logger.LogInformation($"Resetting match ID {match.MatchId} to Upcoming");

                    match.HomeScore = null;
                    match.AwayScore = null;
                    match.Qualified = null;
                    match.Status = MatchStatus.Upcoming;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Match ID {matchUpdateDto.MatchId} updated successfully by User {userId}.");

                if (matchWasFinalised)
                {
                    // Trigger Bet Recalculation if Match is Finalised
                    _logger.LogInformation($"Triggering bet recalculation for Match ID {match.MatchId}");
                    await _betService.RecalculateBetsForMatchAsync(match.MatchId);

                    // Send notifications via NotificationService
                    await _notificationService.NotifyMatchClosureAsync(match);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating match ID {matchUpdateDto.MatchId}");
                return false;
            }
        }

        public async Task AutoUpdateMatchStatusAsync()
        {
            try
            {
                _logger.LogInformation("Checking for matches that need status updates...");

                // Find matches that have started but are still marked as Upcoming
                var matchesToUpdate = await _context.CustomMatches
                    .Where(m => m.MatchStart <= DateTime.UtcNow && m.Status == MatchStatus.Upcoming)
                    .ToListAsync();

                if (!matchesToUpdate.Any())
                {
                    _logger.LogInformation("No matches require status updates.");
                    return;
                }

                // Update the status of the matches
                foreach (var match in matchesToUpdate)
                {
                    match.Status = MatchStatus.InProgress;
                    _logger.LogInformation($"Match {match.MatchId} status updated to InProgress.");
                }

                // Save changes
                await _context.SaveChangesAsync();
                _logger.LogInformation($"{matchesToUpdate.Count} matches were updated to InProgress.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating match statuses.");
                throw;
            }
        }
    }
}
