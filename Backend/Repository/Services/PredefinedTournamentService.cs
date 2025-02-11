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

                // Step 2: Insert Teams and Map Their Actual IDs
                var teams = tournamentDto.Teams.Select(t => new PredefinedTeam
                {
                    TeamName = t.TeamName,
                    PredefinedTournamentId = tournament.TournamentId
                }).ToList();

                _context.PredefinedTeams.AddRange(teams);
                await _context.SaveChangesAsync();

                // Step 3: Create a Map of Team Names to Their Actual Database IDs
                var teamMap = await _context.PredefinedTeams
                    .Where(t => t.PredefinedTournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.TeamName, t => t.TeamId);

                // Step 4: Insert Matches Using the Actual IDs from Database
                var matches = tournamentDto.Matches.Select(m => new PredefinedMatch
                {
                    PredefinedTournamentId = tournament.TournamentId,
                    Stage = m.Stage,
                    HomeTeamId = teamMap.TryGetValue(m.HomeTeam, out var homeId) ? homeId : throw new Exception($"Home team '{m.HomeTeam}' not found."),
                    AwayTeamId = teamMap.TryGetValue(m.AwayTeam, out var awayId) ? awayId : throw new Exception($"Away team '{m.AwayTeam}' not found."),
                    MatchStart = DateTime.SpecifyKind(m.MatchStart, DateTimeKind.Utc),
                    BetType = m.BetType,
                    HomeWinOdds = m.HomeWinOdds,
                    DrawOdds = m.DrawOdds,
                    AwayWinOdds = m.AwayWinOdds,
                    HomeQualifies = m.HomeQualifies ?? 0,
                    AwayQualifies = m.AwayQualifies ?? 0
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
                var updatedTeamsWithIds = tournamentDto.Teams.Where(t => t.TeamId.HasValue).ToDictionary(t => t.TeamId.Value);
                var newTeams = tournamentDto.Teams.Where(t => !t.TeamId.HasValue).ToList();

                // Remove teams not in the updated list (and their dependent matches)
                var teamsToRemove = existingTeams.Values.Where(et => !updatedTeamsWithIds.ContainsKey(et.TeamId)).ToList();

                foreach (var team in teamsToRemove)
                {
                    // Remove related matches
                    var relatedMatches = _context.PredefinedMatches
                        .Where(m => m.HomeTeamId == team.TeamId || m.AwayTeamId == team.TeamId)
                        .ToList();
                    _context.PredefinedMatches.RemoveRange(relatedMatches);
                }

                _context.PredefinedTeams.RemoveRange(teamsToRemove);

                // Update existing teams
                foreach (var teamDto in updatedTeamsWithIds.Values)
                {
                    if (existingTeams.TryGetValue(teamDto.TeamId.Value, out var team))
                    {
                        team.TeamName = teamDto.TeamName;
                    }
                }

                // Add new teams
                foreach (var newTeamDto in newTeams)
                {
                    tournament.PredefinedTeams.Add(new PredefinedTeam
                    {
                        TeamName = newTeamDto.TeamName,
                        PredefinedTournamentId = tournament.TournamentId
                    });
                }

                // Save changes to ensure updated teams are available for match mapping
                await _context.SaveChangesAsync();

                // Step 4: Map team names to IDs
                var teamMap = await _context.PredefinedTeams
                    .Where(t => t.PredefinedTournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.TeamName, t => t.TeamId);

                // Step 5: Handle Matches
                var existingMatches = tournament.PredefinedMatches.ToDictionary(m => m.MatchId);
                var updatedMatchesWithIds = tournamentDto.Matches.Where(m => m.MatchId.HasValue).ToDictionary(m => m.MatchId.Value);
                var newMatches = tournamentDto.Matches.Where(m => !m.MatchId.HasValue).ToList();

                // Remove matches not in the updated list
                var matchesToRemove = existingMatches.Values.Where(em => !updatedMatchesWithIds.ContainsKey(em.MatchId)).ToList();
                _context.PredefinedMatches.RemoveRange(matchesToRemove);

                // Update existing matches
                foreach (var matchDto in updatedMatchesWithIds.Values)
                {
                    if (existingMatches.TryGetValue(matchDto.MatchId.Value, out var match))
                    {
                        match.Stage = matchDto.Stage;
                        match.HomeTeamId = matchDto.HomeTeamId.Value;
                        match.AwayTeamId = matchDto.AwayTeamId.Value;
                        match.MatchStart = DateTime.SpecifyKind(matchDto.MatchStart, DateTimeKind.Utc);
                        match.BetType = matchDto.BetType;
                        match.HomeWinOdds = matchDto.HomeWinOdds;
                        match.DrawOdds = matchDto.DrawOdds;
                        match.AwayWinOdds = matchDto.AwayWinOdds;
                        match.HomeQualifies = matchDto.HomeQualifies;
                        match.AwayQualifies = matchDto.AwayQualifies;
                    }
                }

                // Add new matches
                foreach (var newMatchDto in newMatches)
                {
                    var homeTeamId = teamMap.TryGetValue(newMatchDto.HomeTeam, out var homeId)
                        ? homeId
                        : throw new Exception($"Home team '{newMatchDto.HomeTeam}' not found.");
                    var awayTeamId = teamMap.TryGetValue(newMatchDto.AwayTeam, out var awayId)
                        ? awayId
                        : throw new Exception($"Away team '{newMatchDto.AwayTeam}' not found.");

                    tournament.PredefinedMatches.Add(new PredefinedMatch
                    {
                        PredefinedTournamentId = tournament.TournamentId,
                        Stage = newMatchDto.Stage,
                        HomeTeamId = homeTeamId,
                        AwayTeamId = awayTeamId,
                        MatchStart = DateTime.SpecifyKind(newMatchDto.MatchStart, DateTimeKind.Utc),
                        BetType = newMatchDto.BetType,
                        HomeWinOdds = newMatchDto.HomeWinOdds,
                        DrawOdds = newMatchDto.DrawOdds,
                        AwayWinOdds = newMatchDto.AwayWinOdds,
                        HomeQualifies = newMatchDto.HomeQualifies,
                        AwayQualifies = newMatchDto.AwayQualifies
                    });
                }

                // Save all changes
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
                    Matches = tournament.PredefinedMatches.Select(match => new PredefinedMatchDto
                    {
                        MatchId = match.MatchId,
                        Stage = match.Stage,
                        HomeTeamId = match.HomeTeamId,
                        HomeTeam = match.HomeTeam.TeamName,
                        AwayTeamId = match.AwayTeamId,
                        AwayTeam = match.AwayTeam.TeamName,
                        BetType = match.BetType,
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
    }
}
