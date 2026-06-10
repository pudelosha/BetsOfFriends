using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using static Backend.Model.Entities.CustomMatch;
using static Backend.Model.Entities.CustomTournament;

namespace Backend.Repository.Services
{
    public class CustomTournamentService : ICustomTournamentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CustomTournamentService> _logger;
        private readonly IRegisterService _registerService;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly IBetService _betService;
        private readonly ITournamentSelectionService _tournamentSelectionService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomTournamentService(
            AppDbContext context,
            ILogger<CustomTournamentService> logger,
            IRegisterService registerService,
            IUserService userService,
            IBetService betService,
            IEmailTemplateService emailTemplateService,
            INotificationService notificationService,
            ITournamentSelectionService tournamentSelectionService,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _registerService = registerService;
            _userService = userService;
            _betService = betService;
            _tournamentSelectionService = tournamentSelectionService;
            _notificationService = notificationService;
            _emailService = emailService;
            _configuration = configuration;
            _userManager = userManager;
        }

        private static bool IsQualificationMatch(CustomMatch.MatchType matchType)
        {
            return matchType == CustomMatch.MatchType.ExtendedWithQualification;
        }

        private static bool HasValidQualificationOdds(decimal? homeQualifies, decimal? awayQualifies)
        {
            return homeQualifies.HasValue &&
                   awayQualifies.HasValue &&
                   homeQualifies.Value > 0m &&
                   awayQualifies.Value > 0m;
        }

        private static bool IsPlaceholderMatch(CustomMatchDto match)
        {
            return IsPlaceholderTeamName(match.HomeTeam) || IsPlaceholderTeamName(match.AwayTeam);
        }

        private static bool IsPlaceholderTeamName(string? teamName)
        {
            if (string.IsNullOrWhiteSpace(teamName))
            {
                return true;
            }

            var normalized = teamName.Trim();
            return string.Equals(normalized, "TBA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "TBD", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "To Be Announced", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "To Be Advised", StringComparison.OrdinalIgnoreCase);
        }

        private static (decimal? HomeQualifies, decimal? AwayQualifies) NormalizeQualificationOdds(
            CustomMatchDto match,
            CustomMatch.MatchType matchType)
        {
            if (!IsQualificationMatch(matchType) || IsPlaceholderMatch(match))
                return (null, null);

            if (HasValidQualificationOdds(match.HomeQualifies, match.AwayQualifies))
                return (match.HomeQualifies.Value, match.AwayQualifies.Value);

            var label = match.MatchId.HasValue
                ? $"match ID {match.MatchId.Value}"
                : $"{match.HomeTeam} vs {match.AwayTeam}";

            throw new ArgumentException($"Qualification odds must be greater than zero for {label}.");
        }

        public async Task<TournamentCreationResultDto> CreateCustomTournamentAsync(CustomTournamentDto tournamentDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Step 1: Creating CustomTournament entity");

                var tournament = new CustomTournament
                {
                    Name = tournamentDto.TournamentName,
                    Season = tournamentDto.Season,
                    EndDate = tournamentDto.TournamentEnd,
                    IsActive = tournamentDto.IsActive,
                    CreatedByUserId = tournamentDto.CreatedBy,
                    CreatedAt = DateTime.UtcNow,
                    PredefinedTournamentId = tournamentDto.PredefinedTournamentId,
                    Visibility = Enum.TryParse<TournamentVisibility>(tournamentDto.TournamentVisibility, true, out var parsedVisibility) ? parsedVisibility : TournamentVisibility.Private,
                    Update = Enum.TryParse<TournamentUpdate>(tournamentDto.UpdateMethod, true, out var parsedUpdate) ? parsedUpdate : TournamentUpdate.Manual,
                    CalculateBetsWithHomeAdvantage = tournamentDto.IncludeHomeAdvantage,
                    AllowExactResultBonus = tournamentDto.Settings?.AllowExactResultBonus ?? false,
                    ExactResultBonusCalculation = Enum.TryParse<CustomTournament.ExactResultBonusCalculationType>(tournamentDto.Settings?.ExactResultBonusCalculation, true, out var exactBonusCalculation) ? exactBonusCalculation : CustomTournament.ExactResultBonusCalculationType.Fixed,
                    ExactResultBonus = tournamentDto.Settings?.ExactResultBonus,
                    AllowWhoQualifiesBets = tournamentDto.Settings?.AllowWhoQualifiesBets ?? false,
                    AllowBetsWithBooster = tournamentDto.Settings?.AllowBetsWithBooster ?? false,
                    MaxBetBooster = tournamentDto.Settings?.MaxBetBooster ?? 1,
                    TotalBoosterPool = tournamentDto.Settings?.TotalBoosterPool,
                    AllowNonSubmittedBetsPenalty = tournamentDto.Settings?.AllowNonSubmittedBetsPenalty ?? false,
                    NonSubmittedBetPenalty = tournamentDto.Settings?.NonSubmittedBetPenalty
                };

                _context.CustomTournaments.Add(tournament);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Step 2: Tournament '{tournament.Name}' inserted with ID {tournament.TournamentId}");

                var creatorUser = await _userService.FindUserByIdAsync(tournamentDto.CreatedBy)
                    ?? throw new Exception($"User with ID {tournamentDto.CreatedBy} not found.");

                _context.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
                {
                    UserId = creatorUser.Id,
                    TournamentId = tournament.TournamentId,
                    UserName = creatorUser.Email.Split('@')[0],
                    UserAdminName = creatorUser.Email,
                    Role = UserTournamentRole.Admin,
                    Status = AssignmentStatus.Accepted,
                    IsVisible = true
                });

                _logger.LogInformation("Step 3: Assigned creator as Admin");

                var teams = tournamentDto.Teams.Select(t => new CustomTeam
                {
                    TeamName = t.TeamName,
                    TournamentId = tournament.TournamentId,
                    PredefinedTeamId = t.PredefinedTeamId,
                    EloRating = t.EloRating
                }).ToList();

                _context.CustomTeams.AddRange(teams);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Step 4: {teams.Count} teams inserted");

                var teamMap = await _context.CustomTeams
                    .Where(t => t.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.TeamName, t => t.TeamId);

                var stages = tournamentDto.Stages.Select(t => new CustomMatchStage
                {
                    StageName = t.StageName,
                    TournamentId = tournament.TournamentId,
                    PredefinedStageId = t.PredefinedStageId,
                    Order = t.Order
                }).ToList();

                _context.CustomMatchStages.AddRange(stages);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Step 5: {stages.Count} stages inserted");

                var stageMap = await _context.CustomMatchStages
                    .Where(t => t.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.StageName, t => t.StageId);

                var matches = tournamentDto.Matches.Select(m =>
                {
                    if (!teamMap.TryGetValue(m.HomeTeam, out var homeId))
                        throw new Exception($"Home team '{m.HomeTeam}' not found.");
                    if (!teamMap.TryGetValue(m.AwayTeam, out var awayId))
                        throw new Exception($"Away team '{m.AwayTeam}' not found.");
                    if (!stageMap.TryGetValue(m.StageName, out var stageId))
                        throw new Exception($"Stage '{m.StageName}' not found.");

                    var parsedType = Enum.TryParse<CustomMatch.MatchType>(m.MatchType, true, out var mt)
                        ? mt
                        : CustomMatch.MatchType.Regular90Min;
                    var qualificationOdds = NormalizeQualificationOdds(m, parsedType);
                    Enum.TryParse<CustomMatch.MatchStatus>(m.MatchStatus, true, out var parsedStatus);

                    return new CustomMatch
                    {
                        TournamentId = tournament.TournamentId,
                        StageId = stageId,
                        HomeTeamId = homeId,
                        AwayTeamId = awayId,
                        MatchStart = DateTime.SpecifyKind(m.MatchStart, DateTimeKind.Utc),
                        Type = parsedType,
                        Status = parsedStatus,
                        HomeWinOdds = m.HomeWinOdds,
                        DrawOdds = m.DrawOdds,
                        AwayWinOdds = m.AwayWinOdds,
                        HomeQualifies = qualificationOdds.HomeQualifies,
                        AwayQualifies = qualificationOdds.AwayQualifies,
                        IsVisible = m.IsVisible,
                        PredefinedMatchId = m.PredefinedMatchId,
                        HomeScore = m.ScoreHome,
                        AwayScore = m.ScoreAway,
                        Qualified = Enum.TryParse<TeamQualified>(m.QualifiedTeam, true, out var q) ? q : null
                    };
                }).ToList();

                _context.CustomMatches.AddRange(matches);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Step 6: {matches.Count} matches inserted");

                var invitedUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var existingUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var userDto in tournamentDto.Users)
                {
                    var existingUser = await _userService.FindUserByEmailAsync(userDto.UserEmail);
                    ApplicationUser userToAssign = existingUser;

                    if (existingUser == null)
                    {
                        _logger.LogInformation($"Registering new user: {userDto.UserEmail}");
                        var newUser = await _registerService.RegisterInvitedUserAsync(userDto.UserEmail);
                        if (newUser == null)
                            throw new Exception($"Failed to create user with email: {userDto.UserEmail}");

                        userToAssign = newUser;
                        invitedUsers.Add(userDto.UserEmail);
                    }
                    else
                    {
                        existingUsers.Add(userDto.UserEmail);
                    }

                    _context.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
                    {
                        UserId = userToAssign.Id,
                        TournamentId = tournament.TournamentId,
                        UserAdminName = userDto.UserAdminName,
                        Role = Enum.TryParse(userDto.UserRole, out UserTournamentRole parsedRole) ? parsedRole : UserTournamentRole.Player,
                        Status = AssignmentStatus.Invited,
                        IsVisible = true
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Step 7: Tournament '{tournament.Name}' committed to DB");

                return TournamentCreationResultDto.SuccessResult(
                    tournament.TournamentId,
                    tournament.Name,
                    creatorUser.Id,
                    invitedUsers,
                    existingUsers
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error inserting custom tournament: {ex.Message}");
                return TournamentCreationResultDto.ErrorResult($"Failed to create tournament: {ex.Message}");
            }
        }

        public async Task<List<CustomTournamentListDto>> GetAllCustomTournamentsAsync(string userId)
        {
            try
            {
                var tournaments = await _context.CustomTournaments
                    .AsNoTracking()
                    .Where(t => t.Participants.Any(p => p.UserId == userId && p.Role == UserTournamentRole.Admin)) // Ensure the user is an Admin in the tournament
                    .Select(t => new CustomTournamentListDto
                    {
                        TournamentId = t.TournamentId,
                        TournamentName = t.Name,
                        CreatedAt = DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc),
                        IsActive = t.IsActive
                    })
                    .ToListAsync();

                return tournaments;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving custom tournaments for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool?> UpdateCustomTournamentStatusAsync(int tournamentId, string userId, bool isActive)
        {
            try
            {
                var tournament = await _context.CustomTournaments
                    .Include(t => t.Participants)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Custom tournament with ID {tournamentId} not found.");
                    return null;
                }

                bool isTournamentAdmin = tournament.Participants.Any(p => p.UserId == userId && p.Role == UserTournamentRole.Admin);

                if (!isTournamentAdmin)
                {
                    _logger.LogWarning($"User {userId} attempted to update tournament {tournamentId} without permission.");
                    return false;
                }

                tournament.IsActive = isActive;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Custom tournament ID {tournamentId} status updated to {(isActive ? "active" : "inactive")} by Admin {userId}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating status for custom tournament ID {tournamentId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool?> DeleteCustomTournamentByIdAsync(int tournamentId, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"Attempting to delete custom tournament with ID: {tournamentId}");

                // Step 1: Find the tournament with its related entities
                var tournament = await _context.CustomTournaments
                    .Include(t => t.Matches)
                        .ThenInclude(m => m.Bets)
                    .Include(t => t.Teams)
                    .Include(t => t.Participants) // User-Tournament Assignments
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Custom tournament with ID {tournamentId} not found.");
                    return false;
                }

                // Step 2: Check if the user is the creator
                if (tournament.CreatedByUserId != userId)
                {
                    _logger.LogWarning($"User {userId} attempted to delete tournament {tournamentId} without permission.");
                    return false; // Forbidden
                }

                // Step 3: Delete Bets linked to Matches in this Tournament
                var bets = await _context.Bets
                    .Where(b => b.Match.TournamentId == tournamentId)
                    .ToListAsync();

                if (bets.Any())
                {
                    _context.Bets.RemoveRange(bets);
                    _logger.LogInformation($"Deleted {bets.Count} bets linked directly via match to tournament ID: {tournamentId}");
                }

                // Step 4: Delete Matches
                if (tournament.Matches.Any())
                {
                    _context.CustomMatches.RemoveRange(tournament.Matches);
                    _logger.LogInformation($"Deleted {tournament.Matches.Count} matches associated with tournament ID: {tournamentId}");
                }

                // Step 5: Delete Teams
                if (tournament.Teams.Any())
                {
                    _context.CustomTeams.RemoveRange(tournament.Teams);
                    _logger.LogInformation($"Deleted {tournament.Teams.Count} teams associated with tournament ID: {tournamentId}");
                }

                // Step 6: Delete User-Tournament Assignments (Do NOT delete users!)
                if (tournament.Participants.Any())
                {
                    _context.CustomTournamentUserAssignments.RemoveRange(tournament.Participants);
                    _logger.LogInformation($"Deleted {tournament.Participants.Count} user assignments for tournament ID: {tournamentId}");
                }

                // Step 7: Delete the Tournament itself
                _context.CustomTournaments.Remove(tournament);
                _logger.LogInformation($"Deleted tournament with ID: {tournamentId}");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully deleted custom tournament with ID: {tournamentId}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error occurred while deleting custom tournament with ID: {tournamentId}");
                throw;
            }
        }

        public async Task<TournamentUpdateResultDto> UpdateCustomTournamentAsync(CustomTournamentDto tournamentDto, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Step 1: Fetch the tournament
                var tournament = await _context.CustomTournaments
                    .Include(t => t.Teams)
                    .Include(t => t.Matches)
                    .Include(t => t.Stages)
                    .Include(t => t.Participants)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentDto.TournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Custom tournament ID {tournamentDto.TournamentId} not found.");
                    return TournamentUpdateResultDto.ErrorResult($"Tournament ID {tournamentDto.TournamentId} not found.");
                }

                // Step 2: Ensure the user is an Admin
                bool isTournamentAdmin = tournament.Participants.Any(p => p.UserId == userId && p.Role == UserTournamentRole.Admin);
                if (!isTournamentAdmin)
                {
                    _logger.LogWarning($"User {userId} attempted to update tournament {tournamentDto.TournamentId} without permission.");
                    return TournamentUpdateResultDto.ErrorResult("User is not authorized to update this tournament.");
                }

                // Step 3: Update Tournament Details & Settings
                tournament.Name = tournamentDto.TournamentName;
                tournament.Season = tournamentDto.Season;
                tournament.EndDate = tournamentDto.TournamentEnd;
                tournament.IsActive = tournamentDto.IsActive;
                tournament.CalculateBetsWithHomeAdvantage = tournamentDto.IncludeHomeAdvantage;
                tournament.Visibility = Enum.TryParse<TournamentVisibility>(tournamentDto.TournamentVisibility, true, out var visibilityEnum)
                    ? visibilityEnum
                    : TournamentVisibility.Private;
                tournament.Update = Enum.TryParse<TournamentUpdate>(tournamentDto.UpdateMethod, true, out var updateEnum)
                    ? updateEnum
                    : TournamentUpdate.Manual;

                if (tournamentDto.Settings != null)
                {
                    tournament.AllowExactResultBonus = tournamentDto.Settings.AllowExactResultBonus;
                    tournament.ExactResultBonusCalculation = Enum.TryParse<CustomTournament.ExactResultBonusCalculationType>(
                        tournamentDto.Settings.ExactResultBonusCalculation, true, out var exactBonusCalculation)
                        ? exactBonusCalculation : CustomTournament.ExactResultBonusCalculationType.Fixed;
                    tournament.ExactResultBonus = tournamentDto.Settings.ExactResultBonus;
                    tournament.AllowWhoQualifiesBets = tournamentDto.Settings.AllowWhoQualifiesBets;
                    tournament.AllowBetsWithBooster = tournamentDto.Settings.AllowBetsWithBooster;
                    tournament.MaxBetBooster = tournamentDto.Settings.MaxBetBooster;
                    tournament.TotalBoosterPool = tournamentDto.Settings.TotalBoosterPool;
                    tournament.AllowNonSubmittedBetsPenalty = tournamentDto.Settings.AllowNonSubmittedBetsPenalty;
                    tournament.NonSubmittedBetPenalty = tournamentDto.Settings.NonSubmittedBetPenalty;
                }

                await _context.SaveChangesAsync();

                // Step 4: Handle Teams
                var existingTeams = tournament.Teams.ToDictionary(t => t.TeamId);
                var teamsToRemove = tournamentDto.Teams.Where(t => t.RecordStatus == "Delete").ToList();
                var teamsToUpdate = tournamentDto.Teams.Where(t => t.RecordStatus == "Update").ToList();
                var newTeams = tournamentDto.Teams.Where(t => t.RecordStatus == "New").ToList();

                foreach (var team in teamsToRemove)
                {
                    var relatedMatches = _context.CustomMatches.Where(m => m.HomeTeamId == team.TeamId || m.AwayTeamId == team.TeamId).ToList();
                    _context.CustomMatches.RemoveRange(relatedMatches);
                    _context.CustomTeams.Remove(existingTeams[team.TeamId.Value]);
                }

                foreach (var team in teamsToUpdate)
                {
                    if (existingTeams.TryGetValue(team.TeamId.Value, out var existingTeam))
                    {
                        existingTeam.TeamName = team.TeamName;
                        existingTeam.EloRating = team.EloRating;
                    }
                }

                foreach (var team in newTeams)
                {
                    tournament.Teams.Add(new CustomTeam
                    {
                        TeamName = team.TeamName,
                        TournamentId = tournament.TournamentId,
                        PredefinedTeamId = team.PredefinedTeamId, // This can be null or a valid ID
                        EloRating = team.EloRating
                    });
                }

                await _context.SaveChangesAsync();

                var teamMap = await _context.CustomTeams
                    .Where(t => t.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.TeamName, t => t.TeamId);

                // Step 5: Process Stages
                var existingStages = tournament.Stages.ToDictionary(s => s.StageId);
                var stagesToRemove = tournamentDto.Stages.Where(s => s.RecordStatus == "Delete").ToList();
                var stagesToUpdate = tournamentDto.Stages.Where(s => s.RecordStatus == "Update").ToList();
                var newStages = tournamentDto.Stages.Where(s => s.RecordStatus == "New").ToList();

                foreach (var stage in stagesToRemove)
                {
                    var relatedMatches = _context.CustomMatches.Where(m => m.StageId == stage.StageId).ToList();
                    _context.CustomMatches.RemoveRange(relatedMatches);
                    _context.CustomMatchStages.Remove(existingStages[stage.StageId.Value]);
                }

                foreach (var stage in stagesToUpdate)
                {
                    if (existingStages.TryGetValue(stage.StageId.Value, out var existingStage))
                    {
                        existingStage.StageName = stage.StageName;
                        existingStage.Order = stage.Order;
                    }
                }

                foreach (var stage in newStages)
                {
                    tournament.Stages.Add(new CustomMatchStage
                    {
                        StageName = stage.StageName,
                        TournamentId = tournament.TournamentId,
                        Order = stage.Order,
                        PredefinedStageId = stage.PredefinedStageId
                    });
                }

                await _context.SaveChangesAsync();

                var stageMap = await _context.CustomMatchStages
                    .Where(s => s.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(s => s.StageName, s => s.StageId);

                // Step 6: Process Matches
                var existingMatches = tournament.Matches.ToDictionary(m => m.MatchId);
                var matchesToRemove = tournamentDto.Matches.Where(m => m.RecordStatus == "Delete" && m.MatchId.HasValue).ToList();
                var matchesToUpdate = tournamentDto.Matches.Where(m => m.RecordStatus == "Update" && m.MatchId.HasValue).ToList();
                var newMatches = tournamentDto.Matches.Where(m => m.RecordStatus == "New").ToList();

                // Ensure deleted matches exist before removing them
                foreach (var match in matchesToRemove)
                {
                    if (existingMatches.TryGetValue(match.MatchId.Value, out var existingMatch))
                    {
                        _context.CustomMatches.Remove(existingMatch);
                    }
                }

                // Update existing matches
                foreach (var match in matchesToUpdate)
                {
                    if (existingMatches.TryGetValue(match.MatchId.Value, out var existingMatch))
                    {
                        existingMatch.StageId = stageMap.TryGetValue(match.StageName, out var stageId) ? stageId : throw new Exception($"Stage '{match.StageName}' not found.");
                        existingMatch.HomeTeamId = match.HomeTeamId ?? throw new Exception($"Home team ID missing for match {match.MatchId}");
                        existingMatch.AwayTeamId = match.AwayTeamId ?? throw new Exception($"Away team ID missing for match {match.MatchId}");
                        existingMatch.MatchStart = DateTime.SpecifyKind(match.MatchStart, DateTimeKind.Utc);
                        var parsedType = Enum.TryParse<CustomMatch.MatchType>(match.MatchType, true, out var mt)
                            ? mt
                            : CustomMatch.MatchType.Regular90Min;
                        var qualificationOdds = NormalizeQualificationOdds(match, parsedType);

                        existingMatch.Type = parsedType;
                        existingMatch.HomeWinOdds = match.HomeWinOdds;
                        existingMatch.DrawOdds = match.DrawOdds;
                        existingMatch.AwayWinOdds = match.AwayWinOdds;
                        existingMatch.HomeQualifies = qualificationOdds.HomeQualifies;
                        existingMatch.AwayQualifies = qualificationOdds.AwayQualifies;
                        existingMatch.HomeScore = match.ScoreHome;
                        existingMatch.AwayScore = match.ScoreAway;
                        existingMatch.Qualified = Enum.TryParse<TeamQualified>(match.QualifiedTeam, true, out var q) ? q : null;
                        if (!string.IsNullOrWhiteSpace(match.MatchStatus) && Enum.TryParse<CustomMatch.MatchStatus>(match.MatchStatus, true, out var parsedStatus))
                        {
                            existingMatch.Status = parsedStatus;
                        }
                        existingMatch.IsVisible = match.IsVisible;
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

                    var parsedType = Enum.TryParse<CustomMatch.MatchType>(newMatch.MatchType, true, out var mt)
                        ? mt
                        : CustomMatch.MatchType.Regular90Min;
                    var qualificationOdds = NormalizeQualificationOdds(newMatch, parsedType);

                    tournament.Matches.Add(new CustomMatch
                    {
                        TournamentId = tournament.TournamentId,
                        StageId = stageId,
                        HomeTeamId = homeTeamId,
                        AwayTeamId = awayTeamId,
                        MatchStart = DateTime.SpecifyKind(newMatch.MatchStart, DateTimeKind.Utc),
                        Type = parsedType,
                        HomeWinOdds = newMatch.HomeWinOdds,
                        DrawOdds = newMatch.DrawOdds,
                        AwayWinOdds = newMatch.AwayWinOdds,
                        HomeQualifies = qualificationOdds.HomeQualifies,
                        AwayQualifies = qualificationOdds.AwayQualifies,
                        HomeScore = newMatch.ScoreHome,
                        AwayScore = newMatch.ScoreAway,
                        Qualified = Enum.TryParse<TeamQualified>(newMatch.QualifiedTeam, true, out var q) ? q : null,
                        IsVisible = newMatch.IsVisible,
                        PredefinedMatchId = newMatch.PredefinedMatchId
                    });
                }

                await _context.SaveChangesAsync();

                // Step 7: Handle Users Based on `recordStatus`
                var existingAssignments = tournament.Participants.ToDictionary(u => u.AssignmentId);
                var usersToRemove = tournamentDto.Users.Where(u => u.RecordStatus == "Delete").ToList();
                var usersToUpdate = tournamentDto.Users.Where(u => u.RecordStatus == "Update").ToList();
                var newUsers = tournamentDto.Users.Where(u => u.RecordStatus == "New").ToList();

                var invitedUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var existingUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var removedUserIds = new HashSet<string>();

                foreach (var user in usersToRemove)
                {
                    if (existingAssignments.TryGetValue(user.AssignmentId!.Value, out var assignment))
                    {
                        // Prevent deletion of the tournament creator
                        if (assignment.UserId == tournament.CreatedByUserId)
                        {
                            _logger.LogWarning($"Attempted to remove the tournament creator: {assignment.UserAdminName} ({assignment.UserId}) — skipping.");
                            continue;
                        }

                        _context.CustomTournamentUserAssignments.Remove(assignment);
                        removedUserIds.Add(assignment.UserId);
                        _logger.LogInformation($"Removed tournament assignment for user {assignment.UserAdminName} ({assignment.UserId})");
                    }
                }

                foreach (var user in usersToUpdate)
                {
                    if (existingAssignments.TryGetValue(user.AssignmentId!.Value, out var assignment))
                    {
                        assignment.UserAdminName = user.UserAdminName;
                        assignment.Role = Enum.Parse<UserTournamentRole>(user.UserRole);
                        _logger.LogInformation($"Updated tournament assignment for user {assignment.UserAdminName} ({assignment.UserId}): Role = {user.UserRole}");
                    }
                }

                var processedNewUserEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var assignedUserIds = new HashSet<string>(
                    tournament.Participants
                        .Where(p => !removedUserIds.Contains(p.UserId))
                        .Select(p => p.UserId));

                foreach (var newUserDto in newUsers)
                {
                    if (string.IsNullOrWhiteSpace(newUserDto.UserEmail))
                    {
                        throw new Exception("Cannot invite a user without an email address.");
                    }

                    var normalizedEmail = newUserDto.UserEmail.Trim().ToLowerInvariant();
                    if (!processedNewUserEmails.Add(normalizedEmail))
                    {
                        _logger.LogWarning("Skipping duplicate new participant row for {Email} in tournament {TournamentId}.", normalizedEmail, tournament.TournamentId);
                        continue;
                    }

                    var existingUser = await _userService.FindUserByEmailAsync(normalizedEmail);
                    ApplicationUser userToAssign;
                    var isNewlyRegistered = false;

                    if (existingUser == null)
                    {
                        var newUser = await _registerService.RegisterInvitedUserAsync(normalizedEmail);
                        if (newUser == null)
                            throw new Exception($"Failed to create user with email: {normalizedEmail}");

                        userToAssign = newUser;
                        isNewlyRegistered = true;

                        _logger.LogInformation($"Registered new user {newUser.Email} and assigned to tournament {tournament.TournamentId}");
                    }
                    else
                    {
                        userToAssign = existingUser;

                        _logger.LogInformation($"Assigned existing user {existingUser.Email} to tournament {tournament.TournamentId}");
                    }

                    if (!assignedUserIds.Add(userToAssign.Id))
                    {
                        _logger.LogWarning("Skipping assignment insert for {Email}; user is already assigned to tournament {TournamentId}.", normalizedEmail, tournament.TournamentId);
                        continue;
                    }

                    if (isNewlyRegistered)
                    {
                        invitedUsers.Add(normalizedEmail);
                    }
                    else
                    {
                        existingUsers.Add(normalizedEmail);
                    }

                    _context.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
                    {
                        UserId = userToAssign.Id,
                        TournamentId = tournament.TournamentId,
                        Role = Enum.TryParse(newUserDto.UserRole, out UserTournamentRole parsedRole) ? parsedRole : UserTournamentRole.Player,
                        Status = AssignmentStatus.Invited,
                        IsVisible = true,
                        UserAdminName = newUserDto.UserAdminName
                    });
                }

                // Step 8: Finalize and return result
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return result DTO for controller to handle email sending
                return TournamentUpdateResultDto.SuccessResult(
                    tournament.TournamentId,
                    tournament.Name,
                    invitedUsers,
                    existingUsers
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error updating custom tournament ID {tournamentDto.TournamentId}: {ex.Message}");
                throw;
            }
        }

        public async Task<CustomTournamentDto?> GetCustomTournamentByIdAsync(int tournamentId, string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching custom tournament with ID: {tournamentId} for user {userId}");

                var tournament = await _context.CustomTournaments
                    .Include(t => t.Matches)
                        .ThenInclude(m => m.Stage)
                    .Include(t => t.Matches)
                        .ThenInclude(m => m.HomeTeam)
                    .Include(t => t.Matches)
                        .ThenInclude(m => m.AwayTeam)
                    .Include(t => t.Teams)
                    .Include(t => t.Stages)
                    .Include(t => t.Participants)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Custom tournament with ID {tournamentId} not found.");
                    return null;
                }

                bool isTournamentAdmin = tournament.Participants.Any(p => p.UserId == userId && p.Role == UserTournamentRole.Admin);

                if (!isTournamentAdmin)
                {
                    _logger.LogWarning($"User {userId} attempted to access tournament {tournamentId} without permission.");
                    return null;
                }

                var dto = new CustomTournamentDto
                {
                    TournamentId = tournament.TournamentId,
                    TournamentName = tournament.Name,
                    Season = tournament.Season,
                    TournamentEnd = tournament.EndDate,
                    CreatedBy = tournament.CreatedByUserId,
                    CreatedAt = DateTime.SpecifyKind(tournament.CreatedAt, DateTimeKind.Utc),
                    TournamentVisibility = tournament.Visibility.ToString(),
                    UpdateMethod = tournament.Update.ToString(),
                    IsActive = tournament.IsActive,
                    IncludeHomeAdvantage = tournament.CalculateBetsWithHomeAdvantage,
                    Teams = tournament.Teams.Select(team => new CustomTeamDto
                    {
                        TeamId = team.TeamId,
                        TeamName = team.TeamName,
                        PredefinedTeamId = team.PredefinedTeamId,
                        EloRating = team.EloRating
                    }).ToList(),
                    Stages = tournament.Stages.Select(stage => new CustomStageDto
                    {
                        StageId = stage.StageId,
                        StageName = stage.StageName,
                        PredefinedStageId = stage.PredefinedStageId,
                        Order = stage.Order
                    }).ToList(),
                    Matches = tournament.Matches.Select(match => new CustomMatchDto
                    {
                        MatchId = match.MatchId,
                        PredefinedMatchId = match.PredefinedMatchId,
                        StageId = match.StageId,
                        StageName = match.Stage.StageName,
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
                        MatchStatus = match.Status.ToString(),
                        ScoreHome = match.HomeScore,
                        ScoreAway = match.AwayScore,
                        QualifiedTeam = match.Qualified?.ToString(),
                        IsVisible = match.IsVisible
                    }).ToList(),
                    Users = tournament.Participants.Select(p => new CustomUserDto
                    {
                        AssignmentId = p.AssignmentId,  // Use AssignmentId instead of UserId
                        UserAdminName = p.UserAdminName,
                        UserName = p.UserName,
                        UserEmail = p.User.Email,
                        Status = p.Status.ToString(),
                        UserRole = p.Role.ToString()
                    }).ToList(),
                    Settings = new CustomTournamentSettingsDto
                    {
                        TournamentVisibility = tournament.Visibility.ToString(),
                        UpdateMethod = tournament.Update.ToString(),

                        AllowExactResultBonus = tournament.AllowExactResultBonus,
                        ExactResultBonusCalculation = tournament.ExactResultBonusCalculation.ToString(),
                        ExactResultBonus = tournament.ExactResultBonus,

                        AllowWhoQualifiesBets = tournament.AllowWhoQualifiesBets,

                        AllowBetsWithBooster = tournament.AllowBetsWithBooster,
                        MaxBetBooster = tournament.MaxBetBooster,
                        TotalBoosterPool = tournament.TotalBoosterPool,

                        AllowNonSubmittedBetsPenalty = tournament.AllowNonSubmittedBetsPenalty,
                        NonSubmittedBetPenalty = tournament.NonSubmittedBetPenalty
                    }
                };

                _logger.LogInformation($"Successfully fetched custom tournament with ID: {tournamentId} for user {userId}");
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching custom tournament with ID: {tournamentId}");
                throw;
            }
        }

        public async Task<List<UserActiveTournamentDto>> GetUserActiveTournamentsAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching active tournaments for user {userId}");

                var activeTournaments = await _context.CustomTournamentUserAssignments
                    .Where(a => a.UserId == userId && a.Tournament.IsActive) // User must be assigned & Tournament must be active
                    .Select(a => new UserActiveTournamentDto
                    {
                        TournamentId = a.Tournament.TournamentId,
                        TournamentName = a.Tournament.Name,
                        AssignmentId = a.AssignmentId,
                        UserName = a.User.UserName,
                        NumberOfParticipants = a.Tournament.Participants.Count,
                        Role = a.Role.ToString(),
                        AssignmentStatus = a.Status.ToString(),
                        Visibility = a.Tournament.Visibility.ToString(),
                        IsVisible = a.IsVisible
                    })
                    .ToListAsync();

                return activeTournaments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching active tournaments for user {userId}");
                throw;
            }
        }

        public async Task<bool> QuitTournamentAsync(int tournamentId, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"User {userId} attempting to quit tournament ID {tournamentId}");

                // Step 1: Get assignment
                var assignment = await _context.CustomTournamentUserAssignments
                    .FirstOrDefaultAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (assignment == null)
                {
                    _logger.LogWarning($"No tournament assignment found for user {userId} in tournament ID {tournamentId}");
                    return false;
                }

                // Step 2: Get match IDs from tournament (safe, in memory)
                var matchIds = await _context.CustomMatches
                    .Where(m => m.TournamentId == tournamentId)
                    .Select(m => m.MatchId)
                    .ToListAsync();

                var matchIdSet = matchIds.ToHashSet(); // More efficient lookup

                // Step 3: Load user's bets and filter in memory
                var userBets = await _context.Bets
                    .Where(b => b.UserId == userId)
                    .ToListAsync();

                var betsToRemove = userBets
                    .Where(b => matchIdSet.Contains(b.MatchId))
                    .ToList();

                // Step 4: Remove bets in batches
                const int batchSize = 100;
                for (int i = 0; i < betsToRemove.Count; i += batchSize)
                {
                    var batch = betsToRemove.Skip(i).Take(batchSize).ToList();
                    _context.Bets.RemoveRange(batch);
                    await _context.SaveChangesAsync();
                }

                // Step 5: Remove assignment
                _context.CustomTournamentUserAssignments.Remove(assignment);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation($"User {userId} has quit tournament ID {tournamentId} successfully");

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error while quitting tournament ID {tournamentId} for user {userId}");
                throw;
            }
        }

        public async Task<bool?> ToggleTournamentVisibilityAsync(int tournamentId, string userId)
        {
            try
            {
                var assignment = await _context.CustomTournamentUserAssignments
                    .FirstOrDefaultAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (assignment == null)
                {
                    _logger.LogWarning($"Tournament assignment not found for tournament ID {tournamentId} and user ID {userId}");
                    return null;
                }

                assignment.IsVisible = !assignment.IsVisible;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} updated tournament {tournamentId} visibility to {assignment.IsVisible}");

                return assignment.IsVisible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling tournament visibility for tournament ID {tournamentId}");
                throw;
            }
        }

        public async Task<bool> RecalculateTournamentBetsAsync(int tournamentId, string userId)
        {
            try
            {
                // Fetch tournament and check if the user is an admin
                var tournament = await _context.CustomTournaments
                    .Include(t => t.Participants)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament with ID {tournamentId} not found.");
                    return false;
                }

                bool isTournamentAdmin = tournament.Participants.Any(p => p.UserId == userId && p.Role == UserTournamentRole.Admin);
                if (!isTournamentAdmin)
                {
                    _logger.LogWarning($"User {userId} attempted to recalculate bets for tournament {tournamentId} without permission.");
                    return false; // Unauthorized access
                }

                _logger.LogInformation($"Admin {userId} authorized to recalculate bets for tournament {tournamentId}. Initiating recalculation...");

                // Call the bet recalculation process
                bool success = await _betService.RecalculateBetsForTournamentAsync(tournamentId);

                if (!success)
                {
                    _logger.LogWarning($"No matches or bets found to recalculate for tournament ID {tournamentId}.");
                    return false;
                }

                _logger.LogInformation($"Bet recalculation completed successfully for tournament ID {tournamentId}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error recalculating bets for tournament ID {tournamentId}");
                throw;
            }
        }

        public async Task<List<TournamentSummaryDto>?> GetTournamentSummaryAsync(int tournamentId, string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching tournament summary for ID {tournamentId}, requested by user {userId}");

                var isParticipant = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId && a.Status == AssignmentStatus.Accepted);

                if (!isParticipant)
                {
                    _logger.LogWarning($"User {userId} is not an accepted participant in tournament {tournamentId}.");
                    return null;
                }

                var tournament = await _context.CustomTournaments
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament ID {tournamentId} not found.");
                    return null;
                }

                // Fetch all matches in the tournament (used for MatchesCount and FinalisedMatchesCount)
                var matches = await _context.CustomMatches
                    .Where(m => m.TournamentId == tournamentId)
                    .ToListAsync();

                int matchesCount = matches.Count;
                int finalisedMatchesCount = matches.Count(m => m.Status == CustomMatch.MatchStatus.Finished);

                // Fetch bets
                var tournamentBets = await _context.Bets
                    .Where(b => b.Match.TournamentId == tournamentId)
                    .Include(b => b.Match)
                    .ToListAsync();

                if (!tournamentBets.Any())
                {
                    _logger.LogWarning($"No bets found for tournament ID {tournamentId}.");
                    return new List<TournamentSummaryDto>();
                }

                var acceptedAssignments = await _context.CustomTournamentUserAssignments
                    .Where(a => a.TournamentId == tournamentId && a.Status == AssignmentStatus.Accepted)
                    .ToListAsync();

                var userAssignments = acceptedAssignments
                    .ToDictionary(a => a.UserId, a => a.UserName ?? a.UserAdminName);

                bool showExactResult = tournament.AllowExactResultBonus;
                bool showQualified = tournament.AllowWhoQualifiesBets;

                var summary = tournamentBets
                    .GroupBy(b => b.UserId)
                    .Where(group => userAssignments.ContainsKey(group.Key))
                    .Select(group =>
                    {
                        var userId = group.Key;
                        var bets = group.ToList();

                        int totalBetsPlaced = bets.Count;

                        int finalisedBets = bets.Count(b => b.Status == Bet.BetStatus.Closed);
                        int wonBets = bets.Count(b => b.Status == Bet.BetStatus.Closed && b.Result == Bet.BetResult.Won);
                        decimal betSuccessRate = finalisedBets > 0
                            ? Math.Round((decimal)wonBets / finalisedBets * 100, 2)
                            : 0;

                        int successful1X2 = wonBets;

                        int successfulQualification = bets.Count(b =>
                            b.Status == Bet.BetStatus.Closed &&
                            b.Match.Type == CustomMatch.MatchType.ExtendedWithQualification &&
                            b.Qualified == b.Match.Qualified);

                        int successfulExactResults = bets.Count(b =>
                            b.Status == Bet.BetStatus.Closed &&
                            b.HomeGoals == b.Match.HomeScore &&
                            b.AwayGoals == b.Match.AwayScore);

                        decimal totalPayout = bets
                            .Where(b => b.Status == Bet.BetStatus.Closed)
                            .Sum(b =>
                                (b.BasePayout ?? 0) +
                                (showQualified ? (b.QualificationPayout ?? 0) : 0) +
                                (showExactResult ? (b.ExactScorePayout ?? 0) : 0)
                            );

                        return new TournamentSummaryDto
                        {
                            UserId = userId,
                            UserName = userAssignments.ContainsKey(userId) ? userAssignments[userId] : "Unknown",

                            TotalBetsPlaced = totalBetsPlaced,
                            Successful1X2Results = successful1X2,
                            SuccessfulQualifications = successfulQualification,
                            SuccessfulExactResults = successfulExactResults,
                            TotalPayout = totalPayout,

                            BetSuccessRate = betSuccessRate,
                            MatchesCount = matchesCount,
                            FinalisedMatchesCount = finalisedMatchesCount,

                            ShowExactResult = showExactResult,
                            ShowQualified = showQualified
                        };
                    })
                    .OrderByDescending(s => s.TotalPayout)
                    .ToList();

                for (int i = 0; i < summary.Count; i++)
                {
                    summary[i].Position = i + 1;
                }

                _logger.LogInformation($"Successfully generated tournament summary for ID {tournamentId}.");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching tournament summary for ID {tournamentId}");
                throw;
            }
        }

        public async Task<List<UserBettingStatsDto>> GetUserBettingStatsAsync(string userId, int tournamentId, string statsUserId)
        {
            try
            {
                _logger.LogInformation($"Fetching betting stats for user {statsUserId} in tournament {tournamentId}, requested by {userId}");

                bool isParticipant = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (!isParticipant)
                {
                    _logger.LogWarning($"User {userId} is not assigned to tournament {tournamentId}.");
                    return new List<UserBettingStatsDto>();
                }

                var statsUserAssignment = await _context.CustomTournamentUserAssignments
                    .Where(a => a.TournamentId == tournamentId && a.UserId == statsUserId)
                    .FirstOrDefaultAsync();

                if (statsUserAssignment == null)
                {
                    _logger.LogWarning($"Stats user {statsUserId} is not assigned to tournament {tournamentId}.");
                    return new List<UserBettingStatsDto>();
                }

                var tournament = await _context.CustomTournaments
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament {tournamentId} not found.");
                    return new List<UserBettingStatsDto>();
                }

                var tournamentMatches = await _context.CustomMatches
                    .Where(m => m.TournamentId == tournamentId && m.IsVisible)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Include(m => m.Stage)
                    .Include(m => m.Bets.Where(b => b.UserId == statsUserId))
                    .ToListAsync();

                var nickname = statsUserAssignment.UserName ?? "Unknown";

                var bettingStats = tournamentMatches.Select(match =>
                {
                    var isFinalised = match.Status == CustomMatch.MatchStatus.Finished;
                    var userBet = match.Bets.FirstOrDefault();

                    string? matchResult = isFinalised && match.HomeScore.HasValue && match.AwayScore.HasValue
                        ? $"{match.HomeScore}:{match.AwayScore}"
                        : null;

                    string? betPlaced = userBet?.HomeGoals.HasValue == true && userBet?.AwayGoals.HasValue == true
                        ? $"{userBet.HomeGoals}:{userBet.AwayGoals}"
                        : userBet != null ? "-" : null;

                    bool showExactResult = tournament.AllowExactResultBonus && isFinalised;
                    bool showQualified = tournament.AllowWhoQualifiesBets &&
                                         match.Type == CustomMatch.MatchType.ExtendedWithQualification &&
                                         userBet?.Qualified != null;

                    return new UserBettingStatsDto
                    {
                        PlayerName = nickname,
                        MatchId = match.MatchId,
                        MatchStatus = match.Status.ToString(),
                        Stage = match.Stage.StageName,
                        HomeTeam = match.HomeTeam.TeamName,
                        AwayTeam = match.AwayTeam.TeamName,

                        BetPlaced = betPlaced,
                        WhoQualifiedBet = userBet?.Qualified?.ToString(),

                        MatchResult = match.Type == CustomMatch.MatchType.ExtendedWithQualification && match.Qualified != null
                            ? matchResult
                            : matchResult,

                        WhoQualifiedResult = match.Type == CustomMatch.MatchType.ExtendedWithQualification
                            ? match.Qualified?.ToString()
                            : null,

                        OutcomeRegular = isFinalised && userBet != null
                            ? (userBet.BasePayout > 0 ? "V" : "X")
                            : null,

                        OutcomeExactResult = isFinalised && userBet != null
                            ? (userBet.ExactScorePayout > 0 ? "V" : "X")
                            : null,

                        OutcomeQualification = isFinalised && userBet != null
                            ? (userBet.QualificationPayout > 0 ? "V" : "X")
                            : null,

                        PayoutRegular = isFinalised ? userBet?.BasePayout : null,
                        PayoutExactResult = isFinalised ? userBet?.ExactScorePayout : null,
                        PayoutQualification = isFinalised ? userBet?.QualificationPayout : null,
                        TotalPayout = isFinalised
                            ? ((userBet?.BasePayout ?? 0) +
                               (showExactResult ? (userBet?.ExactScorePayout ?? 0) : 0) +
                               (showQualified ? (userBet?.QualificationPayout ?? 0) : 0))
                            : null,

                        ShowExactResult = showExactResult,
                        ShowQualified = showQualified
                    };
                }).ToList();

                _logger.LogInformation($"Successfully fetched betting stats for user {statsUserId} in tournament {tournamentId}");
                return bettingStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching betting stats for user {statsUserId} in tournament {tournamentId}");
                throw;
            }
        }

        public async Task<List<MatchInsightDto>> GetMatchInsightsAsync(string userId, int tournamentId)
        {
            // Validate the requesting user's participation
            bool isRequesterParticipant = await _context.CustomTournamentUserAssignments
                .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

            if (!isRequesterParticipant)
            {
                _logger.LogWarning($"User {userId} is not a participant of tournament {tournamentId}.");
                return new List<MatchInsightDto>();
            }

            // Fetch all tournament participants
            var participants = await _context.CustomTournamentUserAssignments
                .Where(a => a.TournamentId == tournamentId
                         && a.Status == AssignmentStatus.Accepted)
                .ToListAsync();

            var participantIds = participants.Select(p => p.UserId).ToHashSet();
            var nicknameMap = participants.ToDictionary(p => p.UserId, p => p.UserName ?? "Unknown");

            var tournament = await _context.CustomTournaments
                .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

            if (tournament == null)
            {
                return new List<MatchInsightDto>();
            }

            var matches = await _context.CustomMatches
                .Where(m => m.TournamentId == tournamentId && m.IsVisible)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Stage)
                .Include(m => m.Bets)
                .ToListAsync();

            var result = matches.Select(match =>
            {
                bool isFinalised = match.Status == CustomMatch.MatchStatus.Finished;

                bool showExactResult = tournament.AllowExactResultBonus && isFinalised;
                bool showQualified = tournament.AllowWhoQualifiesBets &&
                                     match.Type == CustomMatch.MatchType.ExtendedWithQualification;

                string matchStatus = match.Status switch
                {
                    CustomMatch.MatchStatus.Timed => "Upcoming",
                    CustomMatch.MatchStatus.In_Play => "InProgress",
                    CustomMatch.MatchStatus.Finished => "Finalized",
                    _ => "Upcoming"
                };

                string? resultScore = isFinalised && match.HomeScore.HasValue && match.AwayScore.HasValue
                    ? $"{match.HomeScore}:{match.AwayScore}"
                    : null;

                var userBets = match.Bets
                    .Where(b => participantIds.Contains(b.UserId))
                    .Select(bet =>
                    {
                        string betScore = bet.HomeGoals.HasValue && bet.AwayGoals.HasValue
                            ? $"{bet.HomeGoals}:{bet.AwayGoals}"
                            : "-";

                        return new MatchUserBetDto
                        {
                            PlayerName = nicknameMap.TryGetValue(bet.UserId, out var name) ? name : "Unknown",
                            BetScore = betScore,
                            ResultSuccess = (bet.BasePayout > 0 ? 1 : 0),
                            PreciseResultSuccess = showExactResult ? (bet.ExactScorePayout > 0 ? 1 : 0) : null,
                            QualificationSuccess = showQualified ? (bet.QualificationPayout > 0 ? 1 : 0) : null,
                            TotalPayout = (bet.BasePayout ?? 0)
                                        + (showExactResult ? (bet.ExactScorePayout ?? 0) : 0)
                                        + (showQualified ? (bet.QualificationPayout ?? 0) : 0)
                        };
                    })
                    .ToList();

                return new MatchInsightDto
                {
                    MatchId = match.MatchId,
                    Stage = match.Stage.StageName,
                    HomeTeam = match.HomeTeam.TeamName,
                    AwayTeam = match.AwayTeam.TeamName,
                    Result = resultScore,
                    MatchDateTime = DateTime.SpecifyKind(match.MatchStart, DateTimeKind.Utc).ToString("o"),
                    MatchStatus = matchStatus,
                    ShowExactResult = showExactResult,
                    ShowQualified = showQualified,
                    UserBets = userBets
                };
            }).ToList();

            return result;
        }

        public async Task<List<TournamentPlayerResultDto>> GetTournamentPlayerResultAsync(int tournamentId, string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching tournament player results for tournament ID {tournamentId}, requested by user {userId}");

                // Check if the user is a participant
                var isParticipant = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId && a.Status == AssignmentStatus.Accepted);

                if (!isParticipant)
                {
                    _logger.LogWarning($"User {userId} is not an accepted participant in tournament {tournamentId}.");
                    return new List<TournamentPlayerResultDto>(); // Unauthorized access
                }

                // Get accepted user assignments
                var acceptedAssignments = await _context.CustomTournamentUserAssignments
                    .Where(a => a.TournamentId == tournamentId && a.Status == AssignmentStatus.Accepted)
                    .ToListAsync();

                var userIdToUsername = acceptedAssignments.ToDictionary(a => a.UserId, a => a.UserName);

                // Fetch all bets for the tournament
                var tournamentBets = await _context.Bets
                    .Where(b => b.Match.TournamentId == tournamentId)
                    .Include(b => b.Match)
                    .ToListAsync();

                if (!tournamentBets.Any())
                {
                    _logger.LogWarning($"No bets found for tournament ID {tournamentId}.");
                    return new List<TournamentPlayerResultDto>();
                }

                // Calculate total payout per accepted user
                var playerResults = tournamentBets
                    .GroupBy(b => b.UserId)
                    .Where(g => userIdToUsername.ContainsKey(g.Key))
                    .Select(group =>
                    {
                        var userIdKey = group.Key;
                        var userName = userIdToUsername[userIdKey];
                        var totalPayout = group
                            .Where(b => b.Status == Bet.BetStatus.Closed)
                            .Sum(b => (b.BasePayout ?? 0) + (b.QualificationPayout ?? 0) + (b.ExactScorePayout ?? 0));

                        return new TournamentPlayerResultDto
                        {
                            UserName = userName,
                            Points = totalPayout,
                            IsCurrentUser = userIdKey == userId
                        };
                    })
                    .OrderByDescending(s => s.Points)
                    .ToList();

                // Assign positions based on payout ranking
                for (int i = 0; i < playerResults.Count; i++)
                {
                    playerResults[i].Position = i + 1;
                }

                // Select the top 5 players
                var top5Players = playerResults.Take(5).ToList();

                // Check if the requesting user is already in the top 5
                bool userInTop5 = top5Players.Any(p => p.IsCurrentUser);

                if (userInTop5)
                {
                    return top5Players;
                }
                else
                {
                    // User is NOT in the top 5, find their position and include them
                    var userPosition = playerResults.FirstOrDefault(p => p.IsCurrentUser);
                    if (userPosition != null)
                    {
                        // Return the top 4 and the requesting user
                        return playerResults.Take(4).ToList().Concat(new List<TournamentPlayerResultDto> { userPosition }).ToList();
                    }

                    // If somehow the user is missing, return just the top 5
                    return top5Players;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching tournament player results for tournament ID {tournamentId}");
                throw;
            }
        }

        public async Task<List<TournamentInviteDto>> GetPendingTournamentInvitesAsync(string userId)
        {
            try
            {
                var invites = await _context.CustomTournamentUserAssignments
                    .Where(a => a.UserId == userId && a.Status == AssignmentStatus.Invited)
                    .Select(a => new TournamentInviteDto
                    {
                        TournamentName = a.Tournament.Name,
                        NumberOfParticipants = a.Tournament.Participants.Count(),
                        AssignmentStatus = a.Status.ToString()
                    })
                    .ToListAsync();

                return invites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending tournament invites for user {userId}", userId);
                throw;
            }
        }

        public async Task<List<string>> GetTournamentStagesAsync(int tournamentId, string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching tournament stages for tournament ID {tournamentId}, requested by user {userId}");

                // Check if the tournament exists
                var tournamentExists = await _context.CustomTournaments.AnyAsync(t => t.TournamentId == tournamentId);
                if (!tournamentExists)
                {
                    _logger.LogWarning($"Tournament {tournamentId} not found.");
                    return null;
                }

                // Check if the user is a participant
                var isParticipant = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (!isParticipant)
                {
                    _logger.LogWarning($"User {userId} is not assigned to tournament {tournamentId}.");
                    return null; // Unauthorized access
                }

                // Fetch tournament stages ordered by 'Order'
                var stages = await _context.CustomMatchStages
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

        public async Task<string?> GetFirstStageWithPendingBetsAsync(int tournamentId, string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching first stage with pending bets for tournament {tournamentId}, requested by {userId}");

                // User must be assigned to the tournament (any role)
                var isParticipant = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (!isParticipant)
                {
                    _logger.LogWarning($"User {userId} is not assigned to tournament {tournamentId}");
                    return null;
                }

                var nowUtc = DateTime.UtcNow;

                var stageName = await _context.Bets
                    .Where(b =>
                        b.Match.TournamentId == tournamentId &&
                        b.UserId == userId &&
                        b.Status == Bet.BetStatus.ToPlace &&
                        b.Match.MatchStart > nowUtc &&
                        (b.Match.Status == CustomMatch.MatchStatus.Scheduled ||
                         b.Match.Status == CustomMatch.MatchStatus.Timed))
                    .OrderBy(b => b.Match.Stage.Order)
                    .Select(b => b.Match.Stage.StageName)
                    .FirstOrDefaultAsync();

                return stageName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching first pending bet stage for user {userId}, tournament {tournamentId}");
                throw new ApplicationException("Could not retrieve pending bet stage.");
            }
        }


        public async Task<string?> GetFirstStageWithUpcomingMatchesAsync(int tournamentId, string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching first stage with upcoming matches for tournament {tournamentId}, requested by user {userId}");

                // Check if user is an Admin in the tournament
                var isAdmin = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId && a.Role == UserTournamentRole.Admin);

                if (!isAdmin)
                {
                    _logger.LogWarning($"User {userId} is not an Admin in tournament {tournamentId} and is not authorized.");
                    return null;
                }

                var stageName = await _context.CustomMatches
                    .Include(m => m.Stage)
                    .Where(m =>
                        m.TournamentId == tournamentId &&
                        (m.Status == CustomMatch.MatchStatus.Scheduled || m.Status == CustomMatch.MatchStatus.Timed))
                    .OrderBy(m => m.Stage.Order)
                    .Select(m => m.Stage.StageName)
                    .FirstOrDefaultAsync();

                return stageName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching stage with upcoming matches for tournament {tournamentId}, user {userId}");
                throw new ApplicationException("Could not retrieve upcoming stage.");
            }
        }

        public async Task<bool> IsTournamentNameTakenAsync(string name, string visibility, string userId)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;

            name = name.Trim();

            try
            {
                if (visibility == CustomTournament.TournamentVisibility.Public.ToString())
                {
                    return await _context.CustomTournaments
                        .AnyAsync(t =>
                            t.Visibility == CustomTournament.TournamentVisibility.Public &&
                            t.Name.ToLower() == name.ToLower());
                }
                else if (visibility == CustomTournament.TournamentVisibility.Private.ToString())
                {
                    return await _context.CustomTournaments
                        .AnyAsync(t =>
                            t.Visibility == CustomTournament.TournamentVisibility.Private &&
                            t.Name.ToLower() == name.ToLower() &&
                            t.Participants.Any(p => p.UserId == userId));
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking tournament name availability: {name}");
                throw;
            }
        }

        public async Task<List<PublicTournamentDto>> GetPublicActiveTournamentsAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching public active tournaments (excluding accepted ones) for user {userId}");

                var rawTournaments = await _context.CustomTournaments
                    .AsNoTracking()
                    .Where(t =>
                        t.IsActive &&
                        t.Visibility == CustomTournament.TournamentVisibility.Public &&
                        !t.Participants.Any(p => p.UserId == userId && p.Status == AssignmentStatus.Accepted)
                    )
                    .Select(t => new
                    {
                        t.TournamentId,
                        t.Name,
                        t.CreatedAt,
                        ParticipantsCount = t.Participants.Count,
                        JoinRequested = t.Participants.Any(p => p.UserId == userId && p.Status == AssignmentStatus.Requested)
                    })
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                var tournaments = rawTournaments
                    .Select(t => new PublicTournamentDto
                    {
                        TournamentId = t.TournamentId,
                        TournamentName = t.Name,
                        CreatedAt = DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc),
                        Participants = t.ParticipantsCount,
                        JoinRequested = t.JoinRequested
                    })
                    .ToList();

                return tournaments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching public active tournaments.");
                throw;
            }
        }

        public async Task<List<TournamentParticipantDto>?> GetTournamentParticipantsAsync(int tournamentId, string userId, string status)
        {
            try
            {
                _logger.LogInformation($"Fetching tournament participants for tournament {tournamentId}, status '{status}', requested by user {userId}");

                var isAdmin = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId && a.Role == UserTournamentRole.Admin);

                if (!isAdmin)
                {
                    _logger.LogWarning($"User {userId} is not an Admin in tournament {tournamentId} and is not authorized.");
                    return null;
                }

                if (!Enum.TryParse<AssignmentStatus>(status, true, out var parsedStatus))
                {
                    _logger.LogWarning($"Invalid assignment status value: '{status}'");
                    return new List<TournamentParticipantDto>(); // Empty if status is invalid
                }

                var participants = await _context.CustomTournamentUserAssignments
                    .Where(a => a.TournamentId == tournamentId && a.Status == parsedStatus)
                    .Include(a => a.User)
                    .Select(a => new TournamentParticipantDto
                    {
                        AssignmentId = a.AssignmentId,
                        UserName = a.UserName ?? a.UserAdminName,
                        UserEmail = a.User.Email,
                        Role = a.Role.ToString()
                    })
                    .ToListAsync();

                return participants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching tournament participants for tournament ID {tournamentId}");
                throw;
            }
        }

        public async Task<ActionResultDto> ExcludeParticipantAsync(int tournamentId, string requesterUserId, string targetUserEmail)
        {
            try
            {
                var tournament = await _context.CustomTournaments
                    .Include(t => t.Participants)
                    .Include(t => t.Matches)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                    return ActionResultDto.ErrorResult("Tournament not found.");

                var requesterAssignment = tournament.Participants.FirstOrDefault(a => a.UserId == requesterUserId);
                if (requesterAssignment == null || requesterAssignment.Role != UserTournamentRole.Admin)
                    return ActionResultDto.ErrorResult("You are not authorized to exclude participants.");

                var userToExclude = await _userService.FindUserByEmailAsync(targetUserEmail);
                if (userToExclude == null)
                    return ActionResultDto.ErrorResult("User not found.");

                if (userToExclude.Id == requesterUserId)
                    return ActionResultDto.ErrorResult("You cannot exclude yourself.");

                if (tournament.CreatedByUserId == userToExclude.Id)
                    return ActionResultDto.ErrorResult("You cannot exclude the tournament creator.");

                var assignmentToRemove = tournament.Participants.FirstOrDefault(p => p.UserId == userToExclude.Id);
                if (assignmentToRemove == null)
                    return ActionResultDto.ErrorResult("User is not a participant.");

                // SAFELY collect match IDs from tournament (in memory)
                var matchIdSet = tournament.Matches.Select(m => m.MatchId).ToHashSet();

                // Query only this user's bets in the tournament (efficient)
                var userBets = await _context.Bets
                    .Where(b => b.UserId == userToExclude.Id)
                    .ToListAsync();

                // Filter in memory using the HashSet
                var betsToRemove = userBets
                    .Where(b => matchIdSet.Contains(b.MatchId))
                    .ToList();

                // Process deletions in batches if needed
                const int batchSize = 100;
                for (int i = 0; i < betsToRemove.Count; i += batchSize)
                {
                    var batch = betsToRemove.Skip(i).Take(batchSize).ToList();
                    _context.Bets.RemoveRange(batch);
                    await _context.SaveChangesAsync(); // Flush between batches (optional)
                }

                // Remove tournament assignment
                _context.CustomTournamentUserAssignments.Remove(assignmentToRemove);
                await _context.SaveChangesAsync();

                return ActionResultDto.SuccessResult($"{targetUserEmail} has been excluded from the tournament.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error excluding user {targetUserEmail} from tournament {tournamentId}");
                return ActionResultDto.ErrorResult("An error occurred while excluding the participant.");
            }
        }

        public async Task<ActionResultDto> AcceptParticipantAsync(int tournamentId, string requesterUserId, string targetUserEmail)
        {
            try
            {
                var tournament = await _context.CustomTournaments
                    .Include(t => t.Participants)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                    return ActionResultDto.ErrorResult("Tournament not found.");

                var requesterAssignment = tournament.Participants.FirstOrDefault(p => p.UserId == requesterUserId);
                if (requesterAssignment == null || requesterAssignment.Role != UserTournamentRole.Admin)
                    return ActionResultDto.ErrorResult("You are not authorized to accept participants.");

                var targetUser = await _userService.FindUserByEmailAsync(targetUserEmail);
                if (targetUser == null)
                    return ActionResultDto.ErrorResult("User not found.");

                var assignment = tournament.Participants.FirstOrDefault(p => p.UserId == targetUser.Id && p.Status == AssignmentStatus.Requested);
                if (assignment == null)
                    return ActionResultDto.ErrorResult("No join request found for this user.");

                assignment.Status = AssignmentStatus.Accepted;
                await _context.SaveChangesAsync();

                await _notificationService.NotifyUserJoinRequestApprovedAsync(assignment);

                return ActionResultDto.SuccessResult($"{targetUserEmail} has been accepted into the tournament.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accepting participant {targetUserEmail} in tournament {tournamentId}");
                return ActionResultDto.ErrorResult("An error occurred while accepting the participant.");
            }
        }

        public async Task<ActionResultDto> ResendInviteAsync(int tournamentId, string requesterUserId, string targetUserEmail)
        {
            try
            {
                var tournament = await _context.CustomTournaments
                    .Include(t => t.Participants)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                    return ActionResultDto.ErrorResult("Tournament not found.");

                var isAdmin = tournament.Participants.Any(p => p.UserId == requesterUserId && p.Role == UserTournamentRole.Admin);
                if (!isAdmin)
                    return ActionResultDto.ErrorResult("You are not authorized to resend invites.");

                var targetAssignment = tournament.Participants
                    .FirstOrDefault(p => p.User.Email == targetUserEmail && p.Status == AssignmentStatus.Invited);

                if (targetAssignment == null)
                    return ActionResultDto.ErrorResult("No invitation found for this user.");

                await _notificationService.NotifyTournamentInvitationsAsync(tournamentId, new[] { targetUserEmail });

                return ActionResultDto.SuccessResult($"Invitation resent to {targetUserEmail}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resending invite to {targetUserEmail} for tournament {tournamentId}");
                return ActionResultDto.ErrorResult("An error occurred while resending the invitation.");
            }
        }

        public async Task<TournamentAssignmentDto?> GetAssignmentDetailsAsync(int tournamentId, string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching assignment details for user {userId} in tournament {tournamentId}");

                var assignment = await _context.CustomTournamentUserAssignments
                    .FirstOrDefaultAsync(a => a.TournamentId == tournamentId && a.UserId == userId && a.Status == AssignmentStatus.Accepted);

                if (assignment == null)
                {
                    _logger.LogWarning($"No assignment found for user {userId} in tournament {tournamentId}");
                    return null;
                }

                return new TournamentAssignmentDto
                {
                    Nickname = assignment.UserName ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving assignment details for tournament {tournamentId}, user {userId}");
                throw;
            }
        }

        public async Task<TournamentInvitationResponseDto> UpdateTournamentAssignmentAsync(int tournamentId, string userId, string newNickname)
        {
            try
            {
                _logger.LogInformation($"User {userId} attempting to update tournament assignment for tournament ID {tournamentId} with new nickname '{newNickname}'");

                // Validate nickname length
                if (string.IsNullOrWhiteSpace(newNickname) || newNickname.Length > 20)
                {
                    _logger.LogWarning($"Invalid nickname '{newNickname}' for tournament ID {tournamentId}");
                    return new TournamentInvitationResponseDto
                    {
                        Success = false,
                        Message = "Nickname cannot be empty or exceed 20 characters."
                    };
                }

                // Check if nickname is already taken in the tournament (excluding current user)
                bool isNicknameTaken = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId &&
                                   a.UserId != userId &&
                                   a.UserName == newNickname);

                if (isNicknameTaken)
                {
                    _logger.LogWarning($"Nickname '{newNickname}' is already taken in tournament ID {tournamentId}");
                    return new TournamentInvitationResponseDto
                    {
                        Success = false,
                        Message = "This nickname is already taken. Please choose a different one."
                    };
                }

                // Fetch the user's assignment
                var assignment = await _context.CustomTournamentUserAssignments
                    .FirstOrDefaultAsync(a => a.TournamentId == tournamentId &&
                                              a.UserId == userId &&
                                              a.Status == AssignmentStatus.Accepted);

                if (assignment == null)
                {
                    _logger.LogWarning($"No accepted assignment found for user {userId} in tournament ID {tournamentId}");
                    return new TournamentInvitationResponseDto
                    {
                        Success = false,
                        Message = "No accepted assignment found. Cannot update nickname."
                    };
                }

                assignment.UserName = newNickname;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} successfully updated nickname to '{newNickname}' for tournament ID {tournamentId}");

                return new TournamentInvitationResponseDto
                {
                    Success = true,
                    Message = "Nickname updated successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating assignment for user {userId} in tournament ID {tournamentId}");
                return new TournamentInvitationResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while updating your nickname. Please try again."
                };
            }
        }

        public async Task<TournamentInvitationResponseDto> AcceptTournamentInvitationAsync(int tournamentId, string userId, string nickname)
        {
            try
            {
                _logger.LogInformation($"User {userId} attempting to accept invitation for tournament ID {tournamentId} with nickname {nickname}");

                // Validate nickname length
                if (string.IsNullOrWhiteSpace(nickname) || nickname.Length > 20)
                {
                    _logger.LogWarning($"Invalid nickname '{nickname}' for tournament ID {tournamentId}");
                    return new TournamentInvitationResponseDto
                    {
                        Success = false,
                        Message = "Nickname cannot be empty or exceed 20 characters."
                    };
                }

                // Check if the nickname is already taken within this tournament
                bool isNicknameTaken = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserName == nickname);

                if (isNicknameTaken)
                {
                    _logger.LogWarning($"Nickname '{nickname}' is already taken in tournament ID {tournamentId}.");
                    return new TournamentInvitationResponseDto
                    {
                        Success = false,
                        Message = "This nickname is already taken. Please choose a different one."
                    };
                }

                // Fetch user tournament assignment
                var assignment = await _context.CustomTournamentUserAssignments
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                var tournamentAssignment = assignment.FirstOrDefault(a => a.TournamentId == tournamentId);

                if (tournamentAssignment == null || tournamentAssignment.Status != AssignmentStatus.Invited)
                {
                    _logger.LogWarning($"No valid invitation found for user {userId} in tournament ID {tournamentId}");
                    return new TournamentInvitationResponseDto
                    {
                        Success = false,
                        Message = "Tournament invitation could not be accepted. You may not be invited."
                    };
                }

                // Accept the invitation and set the nickname
                tournamentAssignment.Status = AssignmentStatus.Accepted;
                tournamentAssignment.UserName = nickname;

                // Unselect all other tournaments and select this one
                foreach (var entry in assignment)
                {
                    entry.IsSelected = entry.TournamentId == tournamentId;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} successfully accepted invitation for tournament ID {tournamentId} with nickname {nickname}. Tournament set as default.");

                try
                {
                    var selected = await _tournamentSelectionService.SetSelectedTournamentAsync(userId, tournamentAssignment.TournamentId);
                    if (!selected)
                    {
                        _logger.LogWarning("User {UserId} accepted tournament {TournamentId}, but setting it as selected returned false.", userId, tournamentId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "User {UserId} accepted tournament {TournamentId}, but setting the selected tournament failed.", userId, tournamentId);
                }

                try
                {
                    await _notificationService.NotifyUserAcceptedTournamentInviteAsync(tournamentAssignment);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "User {UserId} accepted tournament {TournamentId}, but admin notifications failed.", userId, tournamentId);
                }

                return new TournamentInvitationResponseDto
                {
                    Success = true,
                    Message = $"You have successfully joined the tournament as {nickname}."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while accepting invitation for tournament ID {tournamentId}");
                return new TournamentInvitationResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while accepting the invitation. Please try again."
                };
            }
        }

        public async Task<TournamentInvitationResponseDto> RequestToJoinTournamentAsync(string userId, int tournamentId, string nickname, string message)
        {
            try
            {
                _logger.LogInformation($"User {userId} is requesting to join tournament {tournamentId} as '{nickname}'");

                // Validate nickname
                if (string.IsNullOrWhiteSpace(nickname) || nickname.Length > 20)
                {
                    return new TournamentInvitationResponseDto
                    {
                        Success = false,
                        Message = "Nickname cannot be empty or exceed 20 characters."
                    };
                }

                // Check if user is already assigned to this tournament
                bool alreadyAssigned = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (alreadyAssigned)
                {
                    return new TournamentInvitationResponseDto
                    {
                        Success = false,
                        Message = "You have already requested or joined this tournament."
                    };
                }

                // Check if nickname is already taken
                bool nicknameTaken = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserName == nickname);

                if (nicknameTaken)
                {
                    return new TournamentInvitationResponseDto
                    {
                        Success = false,
                        Message = "This nickname is already taken in the tournament. Please choose a different one."
                    };
                }

                // Create join request
                var assignment = new CustomTournamentUserAssignment
                {
                    TournamentId = tournamentId,
                    UserId = userId,
                    UserAdminName = nickname,
                    UserName = nickname,
                    Status = AssignmentStatus.Requested,
                    IsSelected = false
                };

                await _context.CustomTournamentUserAssignments.AddAsync(assignment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} successfully requested to join tournament {tournamentId} as '{nickname}'.");

                try
                {
                    await _notificationService.NotifyAdminsJoinRequestAsync(assignment);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "User {UserId} requested to join tournament {TournamentId}, but admin notifications failed.", userId, tournamentId);
                }

                return new TournamentInvitationResponseDto
                {
                    Success = true,
                    Message = $"You have successfully requested to join the tournament as '{nickname}'."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while processing join request for user {userId} in tournament {tournamentId}");
                return new TournamentInvitationResponseDto
                {
                    Success = false,
                    Message = "An error occurred while requesting to join the tournament. Please try again."
                };
            }
        }

        public async Task<CustomTournamentDto?> CheckForPendingUpdatesAsync(int tournamentId, string userId)
        {
            _logger.LogInformation($"Checking for pending updates in custom tournament {tournamentId} for user {userId}");

            var tournament = await _context.CustomTournaments
                .Include(t => t.Teams)
                .Include(t => t.Stages)
                .Include(t => t.Matches)
                .Include(t => t.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

            if (tournament == null)
            {
                _logger.LogWarning($"Tournament {tournamentId} not found");
                return null;
            }

            bool isAdmin = tournament.Participants.Any(p => p.UserId == userId && p.Role == UserTournamentRole.Admin);
            if (!isAdmin)
            {
                _logger.LogWarning($"Unauthorized update check attempt by user {userId}");
                return null;
            }

            var dto = new CustomTournamentDto
            {
                TournamentId = tournament.TournamentId,
                PredefinedTournamentId = tournament.PredefinedTournamentId,
                TournamentName = tournament.Name,
                CreatedBy = tournament.CreatedByUserId,
                CreatedAt = DateTime.SpecifyKind(tournament.CreatedAt, DateTimeKind.Utc),
                TournamentVisibility = tournament.Visibility.ToString(),
                UpdateMethod = tournament.Update.ToString(),
                IsActive = tournament.IsActive,
                IncludeHomeAdvantage = tournament.CalculateBetsWithHomeAdvantage,
                Users = new(),
                Settings = null
            };

            var predefinedTournamentId = tournament.PredefinedTournamentId;

            // === TEAMS ===
            var predefinedTeams = await _context.PredefinedTeams
                .Where(pt => pt.PredefinedTournamentId == predefinedTournamentId)
                .ToListAsync();
            var predefinedTeamDict = predefinedTeams.ToDictionary(pt => pt.TeamId);

            foreach (var team in tournament.Teams)
            {
                var teamDto = new CustomTeamDto
                {
                    TeamId = team.TeamId,
                    PredefinedTeamId = team.PredefinedTeamId,
                    TeamName = team.TeamName,
                    RecordStatus = "Uploaded"
                };

                if (team.PredefinedTeamId == null || !predefinedTeamDict.ContainsKey(team.PredefinedTeamId.Value))
                {
                    teamDto.RecordStatus = "Delete";
                }
                else
                {
                    var predefined = predefinedTeamDict[team.PredefinedTeamId.Value];
                    if (predefined.UpdatedAt > (team.UpdatedAt ?? team.CreatedAt))
                    {
                        teamDto.TeamName = predefined.TeamName;
                        teamDto.RecordStatus = "Update";
                    }
                }

                dto.Teams.Add(teamDto);
            }

            foreach (var predefined in predefinedTeams)
            {
                if (!tournament.Teams.Any(t => t.PredefinedTeamId == predefined.TeamId))
                {
                    dto.Teams.Add(new CustomTeamDto
                    {
                        TeamId = null,
                        PredefinedTeamId = predefined.TeamId,
                        TeamName = predefined.TeamName,
                        RecordStatus = "New"
                    });
                }
            }

            // === STAGES ===
            var predefinedStages = await _context.PredefinedMatchStages
                .Where(ps => ps.TournamentId == predefinedTournamentId)
                .ToListAsync();
            var predefinedStageDict = predefinedStages.ToDictionary(ps => ps.StageId);

            foreach (var stage in tournament.Stages)
            {
                var stageDto = new CustomStageDto
                {
                    StageId = stage.StageId,
                    PredefinedStageId = stage.PredefinedStageId,
                    StageName = stage.StageName,
                    Order = stage.Order,
                    RecordStatus = "Uploaded"
                };

                if (stage.PredefinedStageId == null || !predefinedStageDict.ContainsKey(stage.PredefinedStageId.Value))
                {
                    stageDto.RecordStatus = "Delete";
                }
                else
                {
                    var predefined = predefinedStageDict[stage.PredefinedStageId.Value];
                    if (predefined.UpdatedAt > (stage.UpdatedAt ?? stage.CreatedAt))
                    {
                        stageDto.StageName = predefined.StageName;
                        stageDto.Order = predefined.Order;
                        stageDto.RecordStatus = "Update";
                    }
                }

                dto.Stages.Add(stageDto);
            }

            foreach (var predefined in predefinedStages)
            {
                if (!tournament.Stages.Any(s => s.PredefinedStageId == predefined.StageId))
                {
                    dto.Stages.Add(new CustomStageDto
                    {
                        StageId = null,
                        PredefinedStageId = predefined.StageId,
                        StageName = predefined.StageName,
                        Order = predefined.Order,
                        RecordStatus = "New"
                    });
                }
            }

            // === MATCHES ===
            var predefinedMatches = await _context.PredefinedMatches
                .Include(pm => pm.PredefinedStage)
                .Include(pm => pm.HomeTeam)
                .Include(pm => pm.AwayTeam)
                .Where(pm => pm.PredefinedTournament.TournamentId == predefinedTournamentId)
                .ToListAsync();
            var predefinedMatchDict = predefinedMatches.ToDictionary(pm => pm.MatchId);

            foreach (var match in tournament.Matches)
            {
                var matchDto = new CustomMatchDto
                {
                    MatchId = match.MatchId,
                    PredefinedMatchId = match.PredefinedMatchId,
                    StageId = match.StageId,
                    StageName = match.Stage?.StageName ?? "Unknown",
                    HomeTeamId = match.HomeTeamId,
                    AwayTeamId = match.AwayTeamId,
                    HomeTeam = match.HomeTeam?.TeamName ?? "Unknown",
                    AwayTeam = match.AwayTeam?.TeamName ?? "Unknown",
                    MatchType = match.Type.ToString(),
                    MatchStart = DateTime.SpecifyKind(match.MatchStart, DateTimeKind.Utc),
                    HomeWinOdds = match.HomeWinOdds,
                    DrawOdds = match.DrawOdds,
                    AwayWinOdds = match.AwayWinOdds,
                    HomeQualifies = match.HomeQualifies,
                    AwayQualifies = match.AwayQualifies,
                    IsVisible = match.IsVisible,
                    RecordStatus = "Uploaded"
                };

                if (match.PredefinedMatchId == null || !predefinedMatchDict.ContainsKey(match.PredefinedMatchId.Value))
                {
                    matchDto.RecordStatus = "Delete";
                }
                else
                {
                    var predefined = predefinedMatchDict[match.PredefinedMatchId.Value];
                    if (predefined.UpdatedAt > (match.UpdatedAt ?? match.CreatedAt))
                    {
                        matchDto.StageId = predefined.StageId;
                        matchDto.StageName = predefined.PredefinedStage?.StageName ?? "Unknown";
                        matchDto.HomeTeamId = predefined.HomeTeamId;
                        matchDto.AwayTeamId = predefined.AwayTeamId;
                        matchDto.HomeTeam = predefined.HomeTeam?.TeamName ?? "Unknown";
                        matchDto.AwayTeam = predefined.AwayTeam?.TeamName ?? "Unknown";
                        matchDto.MatchStart = DateTime.SpecifyKind(predefined.MatchStart, DateTimeKind.Utc);
                        matchDto.MatchType = predefined.Type.ToString();
                        matchDto.HomeWinOdds = predefined.HomeWinOdds;
                        matchDto.DrawOdds = predefined.DrawOdds;
                        matchDto.AwayWinOdds = predefined.AwayWinOdds;
                        matchDto.HomeQualifies = predefined.HomeQualifies;
                        matchDto.AwayQualifies = predefined.AwayQualifies;
                        matchDto.IsVisible = predefined.IsVisible;
                        matchDto.ScoreHome = predefined.HomeScore;
                        matchDto.ScoreAway = predefined.AwayScore;
                        matchDto.MatchStatus = predefined.Status.ToString();
                        matchDto.RecordStatus = "Update";
                    }
                }

                dto.Matches.Add(matchDto);
            }

            foreach (var predefined in predefinedMatches)
            {
                if (!tournament.Matches.Any(m => m.PredefinedMatchId == predefined.MatchId))
                {
                    dto.Matches.Add(new CustomMatchDto
                    {
                        MatchId = null,
                        PredefinedMatchId = predefined.MatchId,
                        StageId = predefined.StageId,
                        StageName = predefined.PredefinedStage?.StageName ?? "Unknown",
                        HomeTeamId = predefined.HomeTeamId,
                        HomeTeam = predefined.HomeTeam?.TeamName ?? "Unknown",
                        AwayTeamId = predefined.AwayTeamId,
                        AwayTeam = predefined.AwayTeam?.TeamName ?? "Unknown",
                        MatchStart = DateTime.SpecifyKind(predefined.MatchStart, DateTimeKind.Utc),
                        MatchType = predefined.Type.ToString(),
                        HomeWinOdds = predefined.HomeWinOdds,
                        DrawOdds = predefined.DrawOdds,
                        AwayWinOdds = predefined.AwayWinOdds,
                        HomeQualifies = predefined.HomeQualifies,
                        AwayQualifies = predefined.AwayQualifies,
                        IsVisible = predefined.IsVisible,
                        ScoreHome = predefined.HomeScore,
                        ScoreAway = predefined.AwayScore,
                        MatchStatus = predefined.Status.ToString(),
                        RecordStatus = "New"
                    });
                }
            }

            _logger.LogInformation($"Finished checking updates for tournament {tournamentId}");
            return dto;
        }

        public async Task<SelectedTournamentDetailsDto?> GetSelectedTournamentDetailsAsync(int tournamentId, string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching selected tournament details for ID {tournamentId}, user {userId}");

                var isParticipant = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (!isParticipant)
                {
                    _logger.LogWarning($"User {userId} is not assigned to tournament {tournamentId}.");
                    return null;
                }

                var tournament = await _context.CustomTournaments
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament ID {tournamentId} not found.");
                    return null;
                }

                var matches = await _context.CustomMatches
                    .Where(m => m.TournamentId == tournamentId)
                    .ToListAsync();

                int matchesCount = matches.Count;
                int finalisedMatchesCount = matches.Count(m => m.Status == CustomMatch.MatchStatus.Finished);

                return new SelectedTournamentDetailsDto
                {
                    TournamentName = tournament.Name,
                    MatchesCount = matchesCount,
                    FinalisedMatchesCount = finalisedMatchesCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching selected tournament details for tournament ID {tournamentId}");
                throw;
            }
        }
    }
}
