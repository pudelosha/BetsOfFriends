using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Services
{
    public class PredefinedTournamentService : IPredefinedTournamentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PredefinedTournamentService> _logger;

        public PredefinedTournamentService(AppDbContext context, ILogger<PredefinedTournamentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CreatePredefinedTournamentAsync(PredefinedTournamentDto tournamentDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Step 1: Insert Tournament
                var tournament = new PredefinedTournament
                {
                    TournamentName = tournamentDto.TournamentName,
                    IsActive = tournamentDto.IsActive,
                    CreatedBy = tournamentDto.CreatedBy,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PredefinedTournaments.Add(tournament);
                await _context.SaveChangesAsync();

                // Step 2: Insert Teams and Fetch their IDs
                var teams = tournamentDto.Teams.Select(t => new PredefinedTeam
                {
                    TeamName = t.TeamName,
                    PredefinedTournamentId = tournament.TournamentId
                }).ToList();

                _context.PredefinedTeams.AddRange(teams);
                await _context.SaveChangesAsync();

                var teamMap = await _context.PredefinedTeams
                    .Where(t => t.PredefinedTournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.TeamName, t => t.TeamId);

                // Step 3: Insert Stages and Fetch their IDs
                var stages = tournamentDto.Stages.Select(s => new PredefinedMatchStage
                {
                    StageName = s.StageName,
                    TournamentId = tournament.TournamentId,
                    Order = s.Order
                }).ToList();

                _context.PredefinedMatchStages.AddRange(stages);
                await _context.SaveChangesAsync();

                var stageMap = await _context.PredefinedMatchStages
                    .Where(s => s.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(s => s.StageName, s => s.StageId);

                // Step 4: Validate and Insert Matches
                var matches = tournamentDto.Matches.Select(m =>
                {
                    if (!teamMap.TryGetValue(m.HomeTeam, out var homeId))
                        throw new Exception($"Home team '{m.HomeTeam}' not found.");

                    if (!teamMap.TryGetValue(m.AwayTeam, out var awayId))
                        throw new Exception($"Away team '{m.AwayTeam}' not found.");

                    if (!stageMap.TryGetValue(m.StageName, out var stageId))
                        throw new Exception($"Stage '{m.StageName}' not found.");

                    return new PredefinedMatch
                    {
                        TournamentId = tournament.TournamentId,
                        StageId = stageId,
                        HomeTeamId = homeId,
                        AwayTeamId = awayId,
                        MatchStart = DateTime.SpecifyKind(m.MatchStart, DateTimeKind.Utc),
                        Type = Enum.Parse<CustomMatch.MatchType>(m.MatchType),
                        HomeWinOdds = m.HomeWinOdds,
                        DrawOdds = m.DrawOdds,
                        AwayWinOdds = m.AwayWinOdds,
                        HomeQualifies = m.HomeQualifies ?? 0,
                        AwayQualifies = m.AwayQualifies ?? 0
                    };
                }).ToList();

                _context.PredefinedMatches.AddRange(matches);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error inserting predefined tournament: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdatePredefinedTournamentAsync(PredefinedTournamentDto tournamentDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Step 1: Fetch the tournament from the database
                var tournament = await _context.PredefinedTournaments
                    .Include(t => t.PredefinedTeams)
                    .Include(t => t.PredefinedStages)
                    .Include(t => t.PredefinedMatches)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentDto.TournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Predefined tournament ID {tournamentDto.TournamentId} not found.");
                    return false;
                }

                // Step 2: Update tournament details
                tournament.TournamentName = tournamentDto.TournamentName;
                tournament.IsActive = tournamentDto.IsActive;
                tournament.CreatedBy = tournamentDto.CreatedBy;

                // Step 3: Handle Teams
                var existingTeams = tournament.PredefinedTeams.ToDictionary(t => t.TeamId);
                var teamsToRemove = tournamentDto.Teams.Where(t => t.RecordStatus == "Delete").ToList();
                var teamsToUpdate = tournamentDto.Teams.Where(t => t.RecordStatus == "Update").ToList();
                var newTeams = tournamentDto.Teams.Where(t => t.RecordStatus == "New").ToList();

                // Delete teams & cascade match deletions
                foreach (var team in teamsToRemove)
                {
                    if (team.TeamId.HasValue && existingTeams.TryGetValue(team.TeamId.Value, out var existingTeam))
                    {
                        var relatedMatches = tournament.PredefinedMatches
                            .Where(m => m.HomeTeamId == team.TeamId || m.AwayTeamId == team.TeamId)
                            .ToList();

                        _context.PredefinedMatches.RemoveRange(relatedMatches);
                        _context.PredefinedTeams.Remove(existingTeam);
                    }
                }

                // Update teams
                foreach (var team in teamsToUpdate)
                {
                    if (team.TeamId.HasValue && existingTeams.TryGetValue(team.TeamId.Value, out var existingTeam))
                    {
                        existingTeam.TeamName = team.TeamName;
                    }
                }

                // Add new teams
                foreach (var newTeam in newTeams)
                {
                    tournament.PredefinedTeams.Add(new PredefinedTeam
                    {
                        TeamName = newTeam.TeamName,
                        PredefinedTournamentId = tournament.TournamentId
                    });
                }

                await _context.SaveChangesAsync();

                // Step 4: Map team names to IDs
                var teamMap = await _context.PredefinedTeams
                    .Where(t => t.PredefinedTournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.TeamName, t => t.TeamId);

                // Step 5: Handle Stages
                var existingStages = tournament.PredefinedStages.ToDictionary(s => s.StageId);
                var stagesToRemove = tournamentDto.Stages.Where(s => s.RecordStatus == "Delete").ToList();
                var stagesToUpdate = tournamentDto.Stages.Where(s => s.RecordStatus == "Update").ToList();
                var newStages = tournamentDto.Stages.Where(s => s.RecordStatus == "New").ToList();

                // Delete stages & cascade match deletions
                foreach (var stage in stagesToRemove)
                {
                    if (stage.StageId.HasValue && existingStages.TryGetValue(stage.StageId.Value, out var existingStage))
                    {
                        var relatedMatches = tournament.PredefinedMatches
                            .Where(m => m.StageId == stage.StageId)
                            .ToList();

                        _context.PredefinedMatches.RemoveRange(relatedMatches);
                        _context.PredefinedMatchStages.Remove(existingStage);
                    }
                }

                // Update stages
                foreach (var stage in stagesToUpdate)
                {
                    if (stage.StageId.HasValue && existingStages.TryGetValue(stage.StageId.Value, out var existingStage))
                    {
                        existingStage.StageName = stage.StageName;
                        existingStage.Order = stage.Order;
                    }
                }

                // Add new stages
                foreach (var newStage in newStages)
                {
                    tournament.PredefinedStages.Add(new PredefinedMatchStage
                    {
                        StageName = newStage.StageName,
                        TournamentId = tournament.TournamentId,
                        Order = newStage.Order
                    });
                }

                await _context.SaveChangesAsync();

                // Step 6: Map stage names to IDs
                var stageMap = await _context.PredefinedMatchStages
                    .Where(s => s.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(s => s.StageName, s => s.StageId);

                // Step 7: Handle Matches
                var existingMatches = tournament.PredefinedMatches.ToDictionary(m => m.MatchId);
                var matchesToRemove = tournamentDto.Matches.Where(m => m.RecordStatus == "Delete").ToList();
                var matchesToUpdate = tournamentDto.Matches.Where(m => m.RecordStatus == "Update").ToList();
                var newMatches = tournamentDto.Matches.Where(m => m.RecordStatus == "New").ToList();

                // Delete matches
                foreach (var match in matchesToRemove)
                {
                    if (match.MatchId.HasValue && existingMatches.TryGetValue(match.MatchId.Value, out var existingMatch))
                    {
                        _context.PredefinedMatches.Remove(existingMatch);
                    }
                }

                // Update matches
                foreach (var match in matchesToUpdate)
                {
                    if (match.MatchId.HasValue && existingMatches.TryGetValue(match.MatchId.Value, out var existingMatch))
                    {
                        existingMatch.StageId = stageMap.TryGetValue(match.StageName, out var stageId) ? stageId : throw new Exception($"Stage '{match.StageName}' not found.");
                        existingMatch.HomeTeamId = match.HomeTeamId.Value;
                        existingMatch.AwayTeamId = match.AwayTeamId.Value;
                        existingMatch.MatchStart = DateTime.SpecifyKind(match.MatchStart, DateTimeKind.Utc);
                        existingMatch.Type = Enum.Parse<CustomMatch.MatchType>(match.MatchType);
                        existingMatch.HomeWinOdds = match.HomeWinOdds;
                        existingMatch.DrawOdds = match.DrawOdds;
                        existingMatch.AwayWinOdds = match.AwayWinOdds;
                        existingMatch.HomeQualifies = match.HomeQualifies;
                        existingMatch.AwayQualifies = match.AwayQualifies;
                    }
                }

                // Add new matches
                foreach (var newMatch in newMatches)
                {
                    var homeTeamId = teamMap.TryGetValue(newMatch.HomeTeam, out var homeId)
                        ? homeId
                        : throw new Exception($"Home team '{newMatch.HomeTeam}' not found.");
                    var awayTeamId = teamMap.TryGetValue(newMatch.AwayTeam, out var awayId)
                        ? awayId
                        : throw new Exception($"Away team '{newMatch.AwayTeam}' not found.");
                    var stageId = stageMap.TryGetValue(newMatch.StageName, out var stgId)
                        ? stgId
                        : throw new Exception($"Stage '{newMatch.StageName}' not found.");

                    tournament.PredefinedMatches.Add(new PredefinedMatch
                    {
                        TournamentId = tournament.TournamentId,
                        StageId = stageId,
                        HomeTeamId = homeTeamId,
                        AwayTeamId = awayTeamId,
                        MatchStart = DateTime.SpecifyKind(newMatch.MatchStart, DateTimeKind.Utc),
                        Type = Enum.Parse<CustomMatch.MatchType>(newMatch.MatchType),
                        HomeWinOdds = newMatch.HomeWinOdds,
                        DrawOdds = newMatch.DrawOdds,
                        AwayWinOdds = newMatch.AwayWinOdds,
                        HomeQualifies = newMatch.HomeQualifies,
                        AwayQualifies = newMatch.AwayQualifies
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error updating predefined tournament ID {tournamentDto.TournamentId}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<PredefinedTournamentListDto>> GetAllPredefinedTournamentsAsync()
        {
            try
            {
                var tournaments = await _context.PredefinedTournaments
                    .AsNoTracking()
                    .Select(t => new PredefinedTournamentListDto
                    {
                        TournamentId = t.TournamentId,
                        TournamentName = t.TournamentName,
                        CreatedAt = t.CreatedAt,
                        IsActive = t.IsActive
                    })
                    .ToListAsync();

                return tournaments;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving predefined tournaments: {ex.Message}");
                throw;
            }
        }

        public async Task<PredefinedTournamentDto?> GetPredefinedTournamentByIdAsync(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Fetching tournament with ID: {tournamentId}");

                var tournament = await _context.PredefinedTournaments
                    .Include(t => t.PredefinedTeams)
                    .Include(t => t.PredefinedMatches)
                    .Include(t => t.PredefinedStages)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament with ID {tournamentId} not found.");
                    return null;
                }

                var dto = new PredefinedTournamentDto
                {
                    TournamentId = tournament.TournamentId,
                    TournamentName = tournament.TournamentName,
                    CreatedBy = tournament.CreatedBy,
                    CreatedAt = tournament.CreatedAt,
                    IsActive = tournament.IsActive,
                    Teams = tournament.PredefinedTeams.Select(team => new PredefinedTeamDto
                    {
                        TeamId = team.TeamId,
                        TeamName = team.TeamName
                    }).ToList(),
                    Stages = tournament.PredefinedStages.Select(stage => new PredefinedStageDto
                    {
                        StageId = stage.StageId,
                        StageName = stage.StageName,
                        Order = stage.Order
                    }).ToList(),
                    Matches = tournament.PredefinedMatches.Select(match => new PredefinedMatchDto
                    {
                        MatchId = match.MatchId,
                        StageId = match.StageId,
                        StageName = match.PredefinedStage.StageName,
                        HomeTeamId = match.HomeTeamId,
                        HomeTeam = match.HomeTeam.TeamName,
                        AwayTeamId = match.AwayTeamId,
                        AwayTeam = match.AwayTeam.TeamName,
                        MatchType = match.Type.ToString(),
                        MatchStart = match.MatchStart,
                        HomeWinOdds = match.HomeWinOdds,
                        DrawOdds = match.DrawOdds,
                        AwayWinOdds = match.AwayWinOdds,
                        HomeQualifies = match.HomeQualifies,
                        AwayQualifies = match.AwayQualifies
                    }).ToList()
                };

                _logger.LogInformation($"Successfully fetched tournament with ID: {tournamentId}");
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching tournament with ID: {tournamentId}");
                throw;
            }
        }

        public async Task<bool> DeletePredefinedTournamentByIdAsync(int tournamentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"Attempting to delete tournament with ID: {tournamentId}");

                var tournament = await _context.PredefinedTournaments
                    .Include(t => t.PredefinedMatches)
                    .Include(t => t.PredefinedTeams)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament with ID {tournamentId} not found.");
                    return false;
                }

                if (tournament.PredefinedMatches.Any())
                {
                    _context.PredefinedMatches.RemoveRange(tournament.PredefinedMatches);
                    _logger.LogInformation($"Deleted {tournament.PredefinedMatches.Count} matches associated with tournament ID: {tournamentId}");
                }

                if (tournament.PredefinedTeams.Any())
                {
                    _context.PredefinedTeams.RemoveRange(tournament.PredefinedTeams);
                    _logger.LogInformation($"Deleted {tournament.PredefinedTeams.Count} teams associated with tournament ID: {tournamentId}");
                }

                _context.PredefinedTournaments.Remove(tournament);
                _logger.LogInformation($"Deleted tournament with ID: {tournamentId}");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully deleted tournament with ID: {tournamentId}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error occurred while deleting tournament with ID: {tournamentId}");
                throw;
            }
        }

        public async Task<bool> UpdatePredefinedTournamentStatusAsync(int tournamentId, bool isActive)
        {
            try
            {
                var tournament = await _context.PredefinedTournaments
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament with ID {tournamentId} not found.");
                    return false;
                }

                tournament.IsActive = isActive;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Tournament ID {tournamentId} status updated to {(isActive ? "active" : "inactive")}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating status for tournament ID {tournamentId}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<PredefinedTournamentListDto>> GetActivePredefinedTournamentsAsync()
        {
            try
            {
                var activeTournaments = await _context.PredefinedTournaments
                    .Where(t => t.IsActive) // Filter only active tournaments
                    .Select(t => new PredefinedTournamentListDto
                    {
                        TournamentId = t.TournamentId,
                        TournamentName = t.TournamentName,
                        CreatedAt = t.CreatedAt,
                        IsActive = t.IsActive
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved active predefined tournaments.");
                return activeTournaments;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching active predefined tournaments: {ex.Message}", ex);
                throw new ApplicationException("An error occurred while fetching predefined tournaments.", ex);
            }
        }
    }
}
