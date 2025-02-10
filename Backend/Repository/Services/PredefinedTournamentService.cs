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
                    //TODO add later after model change IsActive = tournamentDto.IsActive,
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
            try
            {
                var tournament = await _context.PredefinedTournaments
                    .Include(t => t.PredefinedTeams)
                    .Include(t => t.PredefinedMatches)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentDto.TournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Predefined tournament ID {tournamentDto.TournamentId} not found.");
                    return false;
                }

                // Update basic properties
                tournament.TournamentName = tournamentDto.TournamentName;
                tournament.CreatedBy = tournamentDto.CreatedBy;

                // Remove old teams and add new ones
                _context.PredefinedTeams.RemoveRange(tournament.PredefinedTeams);
                tournament.PredefinedTeams.Clear();
                foreach (var teamDto in tournamentDto.Teams)
                {
                    tournament.PredefinedTeams.Add(new PredefinedTeam { TeamName = teamDto.TeamName });
                }

                // Remove old matches and add new ones
                _context.PredefinedMatches.RemoveRange(tournament.PredefinedMatches);
                tournament.PredefinedMatches.Clear();
                foreach (var matchDto in tournamentDto.Matches)
                {
                    tournament.PredefinedMatches.Add(new PredefinedMatch
                    {
                        Stage = matchDto.Stage,
                        //HomeTeamId = matchDto.HomeTeamId,
                        //AwayTeamId = matchDto.AwayTeamId,
                        MatchStart = matchDto.MatchStart,
                        BetType = matchDto.BetType,
                        HomeWinOdds = matchDto.HomeWinOdds,
                        DrawOdds = matchDto.DrawOdds,
                        AwayWinOdds = matchDto.AwayWinOdds,
                        HomeQualifies = matchDto.HomeQualifies,
                        AwayQualifies = matchDto.AwayQualifies
                    });
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
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
                        IsActive = true // TODO update model later t.IsActive
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
    }
}
