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
        private readonly IBetService _betService;

        public PredefinedMatchService(AppDbContext context, ILogger<PredefinedMatchService> logger, INotificationService notificationService, IBetService betService)
        {
            _context = context;
            _logger = logger;
            _betService = betService;
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
                    MatchStart = DateTime.SpecifyKind(m.MatchStart, DateTimeKind.Utc),
                    HomeScore = m.HomeScore,
                    AwayScore = m.AwayScore,
                    QualifiedTeam = m.Qualified.HasValue ? m.Qualified.ToString() : null,
                    Status = m.Status.ToString(),
                    MatchType = m.Type.ToString(),
                    IsFinished = m.Status == MatchStatus.Finished
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

                var isFinalised = matchUpdateDto.IsFinished;

                // Update start time
                if (match.MatchStart != matchUpdateDto.MatchStart)
                {
                    _logger.LogInformation($"Updating match start time for Match ID {match.MatchId}");
                    match.MatchStart = matchUpdateDto.MatchStart;
                }

                if (isFinalised)
                {
                    _logger.LogInformation($"Finalizing predefined match ID {match.MatchId}");

                    match.HomeScore = matchUpdateDto.HomeScore;
                    match.AwayScore = matchUpdateDto.AwayScore;

                    if (match.Type == CustomMatch.MatchType.ExtendedWithQualification)
                    {
                        if (!string.IsNullOrEmpty(matchUpdateDto.QualifiedTeam) &&
                            Enum.TryParse(matchUpdateDto.QualifiedTeam, out CustomMatch.TeamQualified qualifiedTeam))
                        {
                            match.Qualified = qualifiedTeam;
                        }
                        else
                        {
                            match.Qualified = null;
                        }
                    }

                    match.Status = MatchStatus.Finished;
                }
                else
                {
                    _logger.LogInformation($"Resetting predefined match ID {match.MatchId} to Upcoming");

                    match.HomeScore = null;
                    match.AwayScore = null;
                    match.Qualified = null;
                    match.Status = MatchStatus.Timed;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Predefined Match ID {matchUpdateDto.MatchId} updated successfully.");

                var predefinedTournamentId = match.PredefinedTournament?.TournamentId;
                if (predefinedTournamentId == null) return true;

                var relatedCustomTournaments = await _context.CustomTournaments
                    .Where(t => t.PredefinedTournamentId == predefinedTournamentId)
                    .ToListAsync();

                foreach (var customTournament in relatedCustomTournaments)
                {
                    if (customTournament.Update == CustomTournament.TournamentUpdate.Manual)
                    {
                        _logger.LogInformation($"Tournament {customTournament.TournamentId} is Manual. Skipping update.");
                        continue;
                    }

                    if (customTournament.Update == CustomTournament.TournamentUpdate.Semi)
                    {
                        _logger.LogInformation($"Tournament {customTournament.TournamentId} is Semi. Notify admin.");
                        // await _notificationService.NotifyAdminsOfSemiUpdate(customTournament); // To be implemented
                        continue;
                    }

                    var customMatch = await _context.CustomMatches
                        .Include(cm => cm.HomeTeam)
                        .Include(cm => cm.AwayTeam)
                        .FirstOrDefaultAsync(cm =>
                            cm.PredefinedMatchId == match.MatchId &&
                            cm.TournamentId == customTournament.TournamentId);

                    if (customMatch == null)
                    {
                        _logger.LogWarning($"No corresponding CustomMatch found in Tournament {customTournament.TournamentId} for PredefinedMatch {match.MatchId}");
                        continue;
                    }

                    _logger.LogInformation($"Updating CustomMatch {customMatch.MatchId} (Auto) in Tournament {customTournament.TournamentId}");

                    // Always sync MatchStart
                    customMatch.MatchStart = match.MatchStart;

                    if (isFinalised)
                    {
                        customMatch.HomeScore = match.HomeScore;
                        customMatch.AwayScore = match.AwayScore;
                        customMatch.Status = MatchStatus.Finished;

                        if (customMatch.Type == CustomMatch.MatchType.ExtendedWithQualification)
                        {
                            customMatch.Qualified = match.Qualified;
                        }

                        await _context.SaveChangesAsync();
                        await _betService.RecalculateBetsForMatchAsync(customMatch.MatchId);
                        await _notificationService.NotifyMatchClosureAsync(customMatch);
                    }
                    else
                    {
                        // Reset the match
                        customMatch.HomeScore = null;
                        customMatch.AwayScore = null;
                        customMatch.Qualified = null;
                        customMatch.Status = MatchStatus.Timed;

                        await _context.SaveChangesAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating predefined match ID {matchUpdateDto.MatchId}");
                return false;
            }
        }

        public async Task<List<MatchDto>> GetStartedMatchesAsync()
        {
            try
            {
                _logger.LogInformation($"Fetching predefined matches.");

                // Step 1: Fetch matches with Status = InProgress
                var matches = await _context.PredefinedMatches
                    .Include(m => m.PredefinedStage)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Where(m => m.Status == MatchStatus.In_Play)
                    .OrderBy(m => m.MatchStart)
                    .ToListAsync();

                if (!matches.Any())
                {
                    _logger.LogWarning($"No started predefined matches found.");
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
                    IsFinished = m.Status == MatchStatus.Finished
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching started predefined matches.");
                throw;
            }
        }
    }
}
