using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using static Backend.Model.Entities.CustomMatch;
using static Backend.Model.Entities.CustomTournament;

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
                    ExternalTournamentId = tournamentDto.ExternalTournamentId,
                    Season = tournamentDto.Season,
                    ExternalSeasonId = tournamentDto.SeasonId,
                    EndDate = tournamentDto.TournamentEnd,
                    IsActive = false,   // by default new tournament is not visible
                    CreatedBy = tournamentDto.CreatedBy,
                    CreatedAt = DateTime.UtcNow,
                    Update = Enum.TryParse<TournamentUpdate>(tournamentDto.UpdateMethod, true, out var u)
                        ? u
                        : TournamentUpdate.Manual
                };

                _context.PredefinedTournaments.Add(tournament);
                await _context.SaveChangesAsync();

                // Step 2: Insert Teams and Fetch their IDs
                var teams = tournamentDto.Teams.Select(t => new PredefinedTeam
                {
                    TeamName = t.TeamName,
                    ExternalTeamId = t.ExternalTeamId,
                    EloRating = t.EloRating,
                    PredefinedTournamentId = tournament.TournamentId
                }).ToList();

                _context.PredefinedTeams.AddRange(teams);
                await _context.SaveChangesAsync();

                // Map by TeamName (as your logic expects)
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

                    if (!Enum.TryParse(m.MatchType, true, out CustomMatch.MatchType parsedType))
                        parsedType = CustomMatch.MatchType.Regular90Min;

                    // MatchStatus in DTO can be null; default safely
                    CustomMatch.MatchStatus parsedStatus = CustomMatch.MatchStatus.Timed;
                    if (!string.IsNullOrWhiteSpace(m.MatchStatus) && Enum.TryParse(m.MatchStatus, true, out CustomMatch.MatchStatus s))
                        parsedStatus = s;

                    return new PredefinedMatch
                    {
                        TournamentId = tournament.TournamentId,
                        StageId = stageId,
                        HomeTeamId = homeId,
                        AwayTeamId = awayId,
                        MatchStart = DateTime.SpecifyKind(m.MatchStart, DateTimeKind.Utc),
                        Type = parsedType,
                        HomeWinOdds = m.HomeWinOdds,
                        DrawOdds = m.DrawOdds,
                        AwayWinOdds = m.AwayWinOdds,
                        HomeQualifies = m.HomeQualifies ?? 0,
                        AwayQualifies = m.AwayQualifies ?? 0,
                        Status = parsedStatus,
                        IsVisible = m.IsVisible,
                        ExternalMatchId = m.ExternalMatchId,
                        HomeScore = m.ScoreHome,
                        AwayScore = m.ScoreAway,
                        Qualified = Enum.TryParse<TeamQualified>(m.QualifiedTeam, true, out var qualifiedEnum) ? qualifiedEnum : null,
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
                if (tournamentDto.TournamentId == null)
                    throw new ArgumentException("TournamentId is required for update.");

                // Fetch tournament with children
                var tournament = await _context.PredefinedTournaments
                    .Include(t => t.PredefinedTeams)
                    .Include(t => t.PredefinedStages)
                    .Include(t => t.PredefinedMatches)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentDto.TournamentId.Value);

                if (tournament == null)
                {
                    _logger.LogWarning($"Predefined tournament ID {tournamentDto.TournamentId} not found.");
                    return false;
                }

                // ---- Step 1: Update tournament header ----
                tournament.TournamentName = tournamentDto.TournamentName;
                tournament.ExternalTournamentId = tournamentDto.ExternalTournamentId;
                tournament.Season = tournamentDto.Season;
                tournament.ExternalSeasonId = tournamentDto.SeasonId;
                tournament.EndDate = tournamentDto.TournamentEnd;
                tournament.CreatedBy = tournamentDto.CreatedBy;

                tournament.Update = Enum.TryParse<TournamentUpdate>(tournamentDto.UpdateMethod, true, out var updateEnum)
                    ? updateEnum
                    : TournamentUpdate.Manual;

                // ---- Step 2: Teams (Update + optional Add/Delete) ----
                var existingTeamsById = tournament.PredefinedTeams.ToDictionary(t => t.TeamId);

                var teamsToRemove = tournamentDto.Teams.Where(t => t.RecordStatus == "Delete").ToList();
                var teamsToUpdate = tournamentDto.Teams.Where(t => t.RecordStatus == "Update").ToList();
                var teamsToAdd = tournamentDto.Teams.Where(t => t.RecordStatus == "New").ToList();

                // Delete teams (and remove related matches as you had)
                foreach (var dtoTeam in teamsToRemove)
                {
                    if (!dtoTeam.TeamId.HasValue) continue;

                    if (existingTeamsById.TryGetValue(dtoTeam.TeamId.Value, out var existingTeam))
                    {
                        var relatedMatches = tournament.PredefinedMatches
                            .Where(m => m.HomeTeamId == existingTeam.TeamId || m.AwayTeamId == existingTeam.TeamId)
                            .ToList();

                        _context.PredefinedMatches.RemoveRange(relatedMatches);
                        _context.PredefinedTeams.Remove(existingTeam);
                    }
                }

                // Update teams (Name + External + ELO)
                foreach (var dtoTeam in teamsToUpdate)
                {
                    if (!dtoTeam.TeamId.HasValue) continue;

                    if (existingTeamsById.TryGetValue(dtoTeam.TeamId.Value, out var existingTeam))
                    {
                        existingTeam.TeamName = dtoTeam.TeamName;
                        existingTeam.ExternalTeamId = dtoTeam.ExternalTeamId;

                        // ELO update (default to 1000 if missing)
                        existingTeam.EloRating = dtoTeam.EloRating;
                    }
                }

                // Add new teams
                foreach (var dtoTeam in teamsToAdd)
                {
                    tournament.PredefinedTeams.Add(new PredefinedTeam
                    {
                        TeamName = dtoTeam.TeamName,
                        ExternalTeamId = dtoTeam.ExternalTeamId,
                        PredefinedTournamentId = tournament.TournamentId,
                        EloRating = dtoTeam.EloRating
                    });
                }

                await _context.SaveChangesAsync();

                // ---- Step 3: Maps (team/stage) ----
                // Prefer ID mapping; fallback to name mapping only if you must.
                var teamMapByName = await _context.PredefinedTeams
                    .Where(t => t.PredefinedTournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.TeamName, t => t.TeamId);

                var stageMapByName = await _context.PredefinedMatchStages
                    .Where(s => s.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(s => s.StageName, s => s.StageId);

                // ---- Step 4: Matches (ONLY Update rule for updates) ----
                var existingMatchesById = tournament.PredefinedMatches.ToDictionary(m => m.MatchId);

                var matchesToRemove = tournamentDto.Matches.Where(m => m.RecordStatus == "Delete").ToList();
                var matchesToUpdate = tournamentDto.Matches.Where(m => m.RecordStatus == "Update").ToList();
                var matchesToAdd = tournamentDto.Matches.Where(m => m.RecordStatus == "New").ToList();

                // Delete matches
                foreach (var dtoMatch in matchesToRemove)
                {
                    if (!dtoMatch.MatchId.HasValue) continue;

                    if (existingMatchesById.TryGetValue(dtoMatch.MatchId.Value, out var existingMatch))
                    {
                        _context.PredefinedMatches.Remove(existingMatch);
                    }
                }

                // Update matches (apply "Update" rule)
                foreach (var dtoMatch in matchesToUpdate)
                {
                    if (!dtoMatch.MatchId.HasValue) continue;

                    if (!existingMatchesById.TryGetValue(dtoMatch.MatchId.Value, out var existingMatch))
                        continue;

                    // stage
                    if (!stageMapByName.TryGetValue(dtoMatch.StageName, out var stageId))
                        throw new Exception($"Stage '{dtoMatch.StageName}' not found.");

                    // team ids: DTO should send IDs; if not, fallback to name map (optional)
                    var homeTeamId = dtoMatch.HomeTeamId;
                    var awayTeamId = dtoMatch.AwayTeamId;

                    if (!homeTeamId.HasValue)
                    {
                        if (!teamMapByName.TryGetValue(dtoMatch.HomeTeam, out var hid))
                            throw new Exception($"Home team '{dtoMatch.HomeTeam}' not found.");
                        homeTeamId = hid;
                    }

                    if (!awayTeamId.HasValue)
                    {
                        if (!teamMapByName.TryGetValue(dtoMatch.AwayTeam, out var aid))
                            throw new Exception($"Away team '{dtoMatch.AwayTeam}' not found.");
                        awayTeamId = aid;
                    }

                    existingMatch.StageId = stageId;
                    existingMatch.HomeTeamId = homeTeamId.Value;
                    existingMatch.AwayTeamId = awayTeamId.Value;
                    existingMatch.MatchStart = DateTime.SpecifyKind(dtoMatch.MatchStart, DateTimeKind.Utc);

                    // Type
                    existingMatch.Type = Enum.TryParse<CustomMatch.MatchType>(dtoMatch.MatchType, true, out var mt)
                        ? mt
                        : CustomMatch.MatchType.Regular90Min;

                    // Status (if null -> keep existing)
                    if (!string.IsNullOrWhiteSpace(dtoMatch.MatchStatus) &&
                        Enum.TryParse<CustomMatch.MatchStatus>(dtoMatch.MatchStatus, true, out var ms))
                    {
                        existingMatch.Status = ms;
                    }

                    existingMatch.IsVisible = dtoMatch.IsVisible;

                    // Scores / qualified
                    existingMatch.HomeScore = dtoMatch.ScoreHome;
                    existingMatch.AwayScore = dtoMatch.ScoreAway;
                    existingMatch.Qualified = Enum.TryParse<TeamQualified>(dtoMatch.QualifiedTeam, true, out var q) ? q : null;

                    // --- Odds rule: update odds ONLY if match NOT started yet ---
                    // (Using UTC now to match stored UTC MatchStart)
                    var nowUtc = DateTime.UtcNow;
                    var matchHasStarted = existingMatch.MatchStart <= nowUtc;

                    if (!matchHasStarted)
                    {
                        existingMatch.HomeWinOdds = dtoMatch.HomeWinOdds <= 0 ? 1m : dtoMatch.HomeWinOdds;
                        existingMatch.DrawOdds = dtoMatch.DrawOdds <= 0 ? 1m : dtoMatch.DrawOdds;
                        existingMatch.AwayWinOdds = dtoMatch.AwayWinOdds <= 0 ? 1m : dtoMatch.AwayWinOdds;

                        existingMatch.HomeQualifies = dtoMatch.HomeQualifies ?? existingMatch.HomeQualifies;
                        existingMatch.AwayQualifies = dtoMatch.AwayQualifies ?? existingMatch.AwayQualifies;
                    }
                }

                // Add new matches (keep as you already do, but safe defaults)
                foreach (var dtoMatch in matchesToAdd)
                {
                    if (!teamMapByName.TryGetValue(dtoMatch.HomeTeam, out var homeId))
                        throw new Exception($"Home team '{dtoMatch.HomeTeam}' not found.");

                    if (!teamMapByName.TryGetValue(dtoMatch.AwayTeam, out var awayId))
                        throw new Exception($"Away team '{dtoMatch.AwayTeam}' not found.");

                    if (!stageMapByName.TryGetValue(dtoMatch.StageName, out var stgId))
                        throw new Exception($"Stage '{dtoMatch.StageName}' not found.");

                    var type = Enum.TryParse<CustomMatch.MatchType>(dtoMatch.MatchType, true, out var mt)
                        ? mt
                        : CustomMatch.MatchType.Regular90Min;

                    CustomMatch.MatchStatus status = CustomMatch.MatchStatus.Timed;
                    if (!string.IsNullOrWhiteSpace(dtoMatch.MatchStatus) &&
                        Enum.TryParse(dtoMatch.MatchStatus, true, out CustomMatch.MatchStatus s))
                    {
                        status = s;
                    }

                    tournament.PredefinedMatches.Add(new PredefinedMatch
                    {
                        TournamentId = tournament.TournamentId,
                        StageId = stgId,
                        HomeTeamId = homeId,
                        AwayTeamId = awayId,
                        MatchStart = DateTime.SpecifyKind(dtoMatch.MatchStart, DateTimeKind.Utc),
                        Type = type,
                        Status = status,
                        IsVisible = dtoMatch.IsVisible,

                        HomeWinOdds = dtoMatch.HomeWinOdds <= 0 ? 1m : dtoMatch.HomeWinOdds,
                        DrawOdds = dtoMatch.DrawOdds <= 0 ? 1m : dtoMatch.DrawOdds,
                        AwayWinOdds = dtoMatch.AwayWinOdds <= 0 ? 1m : dtoMatch.AwayWinOdds,

                        HomeQualifies = dtoMatch.HomeQualifies ?? 0m,
                        AwayQualifies = dtoMatch.AwayQualifies ?? 0m,

                        ExternalMatchId = dtoMatch.ExternalMatchId,
                        HomeScore = dtoMatch.ScoreHome,
                        AwayScore = dtoMatch.ScoreAway,
                        Qualified = Enum.TryParse<TeamQualified>(dtoMatch.QualifiedTeam, true, out var q2) ? q2 : null,
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error updating predefined tournament ID {tournamentDto.TournamentId}: {ex.Message}");
                return false;
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
                        CreatedAt = DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc),
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
                    ExternalTournamentId = tournament.ExternalTournamentId,
                    Season = tournament.Season,
                    SeasonId = tournament.ExternalSeasonId,
                    TournamentEnd = tournament.EndDate,
                    TournamentName = tournament.TournamentName,
                    CreatedBy = tournament.CreatedBy,
                    CreatedAt = DateTime.SpecifyKind(tournament.CreatedAt, DateTimeKind.Utc),
                    UpdateMethod = tournament.Update.ToString(),
                    IsActive = tournament.IsActive,
                    Teams = tournament.PredefinedTeams.Select(team => new PredefinedTeamDto
                    {
                        TeamId = team.TeamId,
                        TeamName = team.TeamName,
                        EloRating = team.EloRating
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
                        ExternalMatchId = match.ExternalMatchId,
                        StageId = match.StageId,
                        StageName = match.PredefinedStage.StageName,
                        HomeTeamId = match.HomeTeamId,
                        HomeTeam = match.HomeTeam.TeamName,
                        AwayTeamId = match.AwayTeamId,
                        AwayTeam = match.AwayTeam.TeamName,
                        MatchType = match.Type.ToString(),
                        MatchStart = DateTime.SpecifyKind(match.MatchStart, DateTimeKind.Utc),
                        HomeWinOdds = match.HomeWinOdds,
                        DrawOdds = match.DrawOdds,
                        AwayWinOdds = match.AwayWinOdds,
                        HomeQualifies = match.HomeQualifies,
                        AwayQualifies = match.AwayQualifies,
                        IsVisible = match.IsVisible,
                        MatchStatus = match.Status.ToString(),
                        ScoreHome = match.HomeScore,
                        ScoreAway = match.AwayScore,
                        QualifiedTeam = match.Qualified?.ToString(),
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
                        CreatedAt = DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc),
                        IsActive = t.IsActive,
                        HasLiveUpdates = t.ExternalTournamentId != null && t.Update == TournamentUpdate.Auto
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

        public async Task<List<string>> GetTournamentStagesAsync(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Fetching tournament stages for tournament ID {tournamentId}");

                // Check if the tournament exists
                var tournamentExists = await _context.PredefinedTournaments.AnyAsync(t => t.TournamentId == tournamentId);
                if (!tournamentExists)
                {
                    _logger.LogWarning($"Tournament {tournamentId} not found.");
                    return null;
                }

                // Fetch tournament stages ordered by 'Order'
                var stages = await _context.PredefinedMatchStages
                    .Where(s => s.TournamentId == tournamentId)
                    .OrderBy(s => s.Order)
                    .Select(s => s.StageName)
                    .ToListAsync();

                if (!stages.Any())
                {
                    _logger.LogWarning($"No stages found for tournament ID {tournamentId}.");
                }

                return stages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching tournament stages for tournament ID {tournamentId}");
                throw;
            }
        }

        public async Task<byte[]?> ExportMatchesToExcelAsync(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Exporting matches for tournament ID: {tournamentId}");

                var matches = await _context.PredefinedMatches
                    .Where(m => m.TournamentId == tournamentId)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .ToListAsync();

                if (!matches.Any())
                {
                    _logger.LogWarning($"No matches found for tournament ID: {tournamentId}");
                    return null;
                }

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Matches");

                // Header row
                worksheet.Cell(1, 1).Value = "MatchId";
                worksheet.Cell(1, 2).Value = "HomeTeam";
                worksheet.Cell(1, 3).Value = "AwayTeam";
                worksheet.Cell(1, 4).Value = "HomeWinOdds";
                worksheet.Cell(1, 5).Value = "DrawOdds";
                worksheet.Cell(1, 6).Value = "AwayWinOdds";
                worksheet.Cell(1, 7).Value = "HomeQualifies";
                worksheet.Cell(1, 8).Value = "AwayQualifies";

                // Data rows
                int row = 2;
                foreach (var match in matches)
                {
                    worksheet.Cell(row, 1).Value = match.MatchId;
                    worksheet.Cell(row, 2).Value = match.HomeTeam?.TeamName ?? "-";
                    worksheet.Cell(row, 3).Value = match.AwayTeam?.TeamName ?? "-";
                    worksheet.Cell(row, 4).Value = match.HomeWinOdds;
                    worksheet.Cell(row, 5).Value = match.DrawOdds;
                    worksheet.Cell(row, 6).Value = match.AwayWinOdds;
                    worksheet.Cell(row, 7).Value = match.HomeQualifies;
                    worksheet.Cell(row, 8).Value = match.AwayQualifies;
                    row++;
                }

                // Adjust column widths
                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                _logger.LogInformation($"Excel file created for tournament ID: {tournamentId}, total matches: {matches.Count}");
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to export Excel for tournament ID: {tournamentId}");
                throw new ApplicationException("Error generating Excel file.", ex);
            }
        }

        public async Task<string?> GetFirstStageWithUpcomingMatchesAsync(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Fetching first stage with upcoming matches for tournament {tournamentId}");

                var stageName = await _context.PredefinedMatches
                    .Include(m => m.PredefinedStage)
                    .Where(m =>
                        m.TournamentId == tournamentId &&
                        (m.Status == CustomMatch.MatchStatus.Scheduled || m.Status == CustomMatch.MatchStatus.Timed))
                    .OrderBy(m => m.PredefinedStage.Order)
                    .Select(m => m.PredefinedStage.StageName)
                    .FirstOrDefaultAsync();

                return stageName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching stage with upcoming matches for tournament {tournamentId}");
                throw new ApplicationException("Could not retrieve upcoming stage.");
            }
        }
    }
}
