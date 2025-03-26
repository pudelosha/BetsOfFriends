using Backend.DTOs.Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Backend.Model.Entities.CustomMatch;

namespace Backend.Repository.Services
{
    public class PredefinedMatchService : IPredefinedMatchService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PredefinedMatchService> _logger;
        private readonly INotificationService _notificationService;

        public PredefinedMatchService(AppDbContext context, ILogger<PredefinedMatchService> logger, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<List<MatchDto>> GetMatchesByStatusAndStageAsync(int tournamentId, string status, string stage)
        {
            try
            {
                // Step 1: Validate and parse match status
                if (!Enum.TryParse<MatchStatus>(status, true, out var matchStatus))
                {
                    _logger.LogWarning($"Invalid match status received: {status}");
                    return new List<MatchDto>(); // Return empty list instead of BadRequest
                }

                // Step 2: Validate if the stage exists in the tournament
                bool stageExists = await _context.PredefinedMatchStages
                    .AnyAsync(s => s.TournamentId == tournamentId && s.StageName == stage);

                if (!stageExists)
                {
                    _logger.LogWarning($"Stage {stage} does not exist for tournament {tournamentId}");
                    return new List<MatchDto>();
                }

                // Step 3: Fetch matches with the given status and stage
                var matches = await _context.PredefinedMatches
                    .Include(m => m.PredefinedTournament)
                    .Include(m => m.PredefinedStage)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Where(m => m.TournamentId == tournamentId && m.Status == matchStatus && m.PredefinedStage.StageName == stage)
                    .OrderBy(m => m.MatchStart)
                    .ToListAsync();

                if (!matches.Any())
                {
                    _logger.LogWarning($"No matches found with status {status} and stage {stage} for tournament {tournamentId}");
                    return new List<MatchDto>();
                }

                // Step 6: Convert matches to DTO format
                return matches.Select(m => new MatchDto
                {
                    MatchId = m.MatchId,
                    Stage = m.PredefinedStage.StageName,
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
                _logger.LogError(ex, $"Error fetching matches for tournament {tournamentId}, status {status}, stage {stage}");
                throw;
            }
        }

        public async Task<bool> UpdateMatchResultAsync(MatchResultUpdateDto matchUpdateDto)
        {
            try
            {
                var match = await _context.PredefinedMatches
                    .Include(m => m.PredefinedTournament)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .FirstOrDefaultAsync(m => m.MatchId == matchUpdateDto.MatchId);

                if (match == null)
                {
                    _logger.LogWarning($"Match ID {matchUpdateDto.MatchId} not found.");
                    return false;
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
                _logger.LogInformation($"Match ID {matchUpdateDto.MatchId} updated successfully.");

                if (matchWasFinalised)
                {
                    //TODO
                    // Trigger Custom Match updates if the tournament settings allow that
                    // Send notifications via NotificationService
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating match ID {matchUpdateDto.MatchId}");
                return false;
            }
        }

        public async Task<List<MatchDto>> GetStartedMatchesAsync(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Fetching matches for tournament {tournamentId}.");

                // Step 1: Fetch matches with Status = InProgress
                var matches = await _context.PredefinedMatches
                    .Include(m => m.PredefinedStage)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Where(m => m.TournamentId == tournamentId &&
                                m.Status == MatchStatus.InProgress)
                    .OrderBy(m => m.MatchStart)
                    .ToListAsync();

                if (!matches.Any())
                {
                    _logger.LogWarning($"No started predefined matches found for tournament {tournamentId}");
                    return new List<MatchDto>();
                }

                // Step 3: Map to DTO
                return matches.Select(m => new MatchDto
                {
                    MatchId = m.MatchId,
                    Stage = m.PredefinedStage.StageName,
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
                _logger.LogError(ex, $"Error fetching started predefined matches for tournament {tournamentId}");
                throw;
            }
        }
    }
}
