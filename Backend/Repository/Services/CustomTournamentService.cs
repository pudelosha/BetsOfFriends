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
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IConfiguration _configuration;
        private readonly IBetService _betService;
        private readonly ITournamentSelectionService _tournamentSelectionService;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomTournamentService(
            AppDbContext context,
            ILogger<CustomTournamentService> logger,
            IRegisterService registerService,
            IUserService userService,
            IBetService betService,
            IEmailService emailService,
            IEmailTemplateService emailTemplateService,
            INotificationService notificationService,
            ITournamentSelectionService tournamentSelectionService,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _registerService = registerService;
            _userService = userService;
            _betService = betService;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
            _tournamentSelectionService = tournamentSelectionService;
            _notificationService = notificationService;
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<bool> CreateCustomTournamentAsync(CustomTournamentDto tournamentDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Step 1: Insert Tournament with Settings
                var tournament = new CustomTournament
                {
                    Name = tournamentDto.TournamentName,
                    IsActive = tournamentDto.IsActive,
                    CreatedByUserId = tournamentDto.CreatedBy,
                    CreatedAt = DateTime.UtcNow,
                    PredefinedTournamentId = tournamentDto.PredefinedTournamentId,

                    // Tournament Settings Mapping
                    Visibility = Enum.TryParse<TournamentVisibility>(tournamentDto.TournamentVisibility, true, out var parsedVisibility) ? parsedVisibility : TournamentVisibility.Private,
                    Update = Enum.TryParse<TournamentUpdate>(tournamentDto.UpdateMethod, true, out var parsedUpdate) ? parsedUpdate : TournamentUpdate.Manual,
                    AllowExactResultBonus = tournamentDto.Settings?.AllowExactResultBonus ?? false,
                    ExactResultBonusCalculation = Enum.TryParse<CustomTournament.ExactResultBonusCalculationType>(
                        tournamentDto.Settings?.ExactResultBonusCalculation, true, out var exactBonusCalculation)
                        ? exactBonusCalculation : CustomTournament.ExactResultBonusCalculationType.Fixed,
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

                // Step 2: Assign Creator as Admin
                var creatorUser = await _userService.FindUserByIdAsync(tournamentDto.CreatedBy);
                if (creatorUser == null)
                    throw new Exception($"User with ID {tournamentDto.CreatedBy} not found.");

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

                // Step 3: Insert Teams
                var teams = tournamentDto.Teams.Select(t => new CustomTeam
                {
                    TeamName = t.TeamName,
                    TournamentId = tournament.TournamentId,
                    PredefinedTeamId = t.PredefinedTeamId
                }).ToList();

                _context.CustomTeams.AddRange(teams);
                await _context.SaveChangesAsync();

                var teamMap = await _context.CustomTeams
                    .Where(t => t.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.TeamName, t => t.TeamId);

                // Step 4: Insert Stages
                var stages = tournamentDto.Stages.Select(t => new CustomMatchStage
                {
                    StageName = t.StageName,
                    TournamentId = tournament.TournamentId,
                    PredefinedStageId = t.PredefinedStageId,
                    Order = t.Order
                }).ToList();

                _context.CustomMatchStages.AddRange(stages);
                await _context.SaveChangesAsync();

                var stageMap = await _context.CustomMatchStages
                    .Where(t => t.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.StageName, t => t.StageId);

                // Step 5: Insert Matches
                var matches = tournamentDto.Matches.Select(m => new CustomMatch
                {
                    TournamentId = tournament.TournamentId,
                    StageId = stageMap.TryGetValue(m.StageName, out var stageId) ? stageId : throw new Exception($"Stage '{m.StageName}' not found."),
                    HomeTeamId = teamMap.TryGetValue(m.HomeTeam, out var homeId) ? homeId : throw new Exception($"Home team '{m.HomeTeam}' not found."),
                    AwayTeamId = teamMap.TryGetValue(m.AwayTeam, out var awayId) ? awayId : throw new Exception($"Away team '{m.AwayTeam}' not found."),
                    MatchStart = DateTime.SpecifyKind(m.MatchStart, DateTimeKind.Utc),
                    Type = Enum.Parse<CustomMatch.MatchType>(m.MatchType),
                    HomeWinOdds = m.HomeWinOdds,
                    DrawOdds = m.DrawOdds,
                    AwayWinOdds = m.AwayWinOdds,
                    HomeQualifies = m.HomeQualifies ?? 0,
                    AwayQualifies = m.AwayQualifies ?? 0,
                    Status = MatchStatus.Upcoming,
                    IsVisible = true,
                    PredefinedMatchId = m.PredefinedMatchId
                }).ToList();

                _context.CustomMatches.AddRange(matches);
                await _context.SaveChangesAsync();

                // Step 6: Process User Assignments
                var invitedUsers = new HashSet<string>(); // Track emails needing account setup
                var existingUsers = new HashSet<string>(); // Track emails needing tournament invite
                var emailTasks = new List<Task>();

                foreach (var userDto in tournamentDto.Users)
                {
                    var existingUser = await _userService.FindUserByEmailAsync(userDto.UserEmail);
                    ApplicationUser userToAssign = existingUser;

                    if (existingUser == null)
                    {
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

                    // Assign user to tournament
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

                // Commit Transaction Before Sending Emails
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation($"Custom tournament {tournament.Name} created successfully.");

                // Step 7: Generate Bets for Users & Matches
                await _betService.CreateBetsForTournamentAsync(tournament.TournamentId);

                // Step 8: Send Email Invitations (Outside Transaction)
                foreach (var email in invitedUsers)
                {
                    var user = await _userService.FindUserByEmailAsync(email);
                    if (user != null)
                    {
                        var setupLink = await GenerateAccountSetupLinkAsync(user);
                        emailTasks.Add(SendAccountSetupEmailAsync(user.Email, tournament.Name, setupLink));
                    }
                }

                foreach (var email in existingUsers)
                {
                    var inviteLink = GenerateTournamentInviteLink(email, tournament.TournamentId);
                    emailTasks.Add(SendTournamentInvitationEmailAsync(email, tournament.Name, inviteLink));
                }

                // Send all emails in parallel
                await Task.WhenAll(emailTasks);

                // Set created tournament as default
                await _tournamentSelectionService.SetSelectedTournamentAsync(creatorUser.Id, tournament.TournamentId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error inserting custom tournament: {ex.Message}");
                return false;
            }
        }

        private string GenerateTournamentInviteLink(string email, int tournamentId)
        {
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            var frontendBaseUrl = environment == "Development"
                ? _configuration["App:ClientBaseUrlDev"]
                : _configuration["App:ClientBaseUrlProd"];

            return $"{frontendBaseUrl}/my-tournaments";
        }

        private async Task SendTournamentInvitationEmailAsync(string email, string tournamentName, string inviteLink)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "TOURNAMENT_NAME", tournamentName },
                { "INVITE_LINK", inviteLink }
            };

            string emailBody = await _emailTemplateService.GetEmailTemplateAsync("TournamentInvite", placeholders);

            await _emailService.SendEmailAsync(email, $"You're Invited to {tournamentName}!", emailBody);
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
                        CreatedAt = t.CreatedAt,
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

        private async Task<string> GenerateAccountSetupLinkAsync(ApplicationUser user)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            var frontendBaseUrl = environment == "Development"
                ? _configuration["App:ClientBaseUrlDev"]
                : _configuration["App:ClientBaseUrlProd"];

            return $"{frontendBaseUrl}/setup-account?userId={user.Id}&token={encodedToken}";
        }

        private async Task SendAccountSetupEmailAsync(string email, string tournamentName, string setupLink)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "TOURNAMENT_NAME", tournamentName },
                { "SETUP_LINK", setupLink }
            };

            string emailBody = await _emailTemplateService.GetEmailTemplateAsync("AccountSetup", placeholders);

            await _emailService.SendEmailAsync(email, $"Set Up Your Account for {tournamentName}", emailBody);
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
                var matchIds = tournament.Matches.Select(m => m.MatchId).ToList();
                var bets = await _context.Bets.Where(b => matchIds.Contains(b.MatchId)).ToListAsync();
                if (bets.Any())
                {
                    _context.Bets.RemoveRange(bets);
                    _logger.LogInformation($"Deleted {bets.Count} bets associated with tournament ID: {tournamentId}");
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

        public async Task<bool?> UpdateCustomTournamentAsync(CustomTournamentDto tournamentDto, string userId)
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
                    return false;
                }

                // Step 2: Ensure the user is an Admin
                bool isTournamentAdmin = tournament.Participants.Any(p => p.UserId == userId && p.Role == UserTournamentRole.Admin);
                if (!isTournamentAdmin)
                {
                    _logger.LogWarning($"User {userId} attempted to update tournament {tournamentDto.TournamentId} without permission.");
                    return false;
                }

                // Step 3: Update Tournament Details & Settings
                tournament.Name = tournamentDto.TournamentName;
                tournament.IsActive = tournamentDto.IsActive;
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
                    }
                }

                foreach (var team in newTeams)
                {
                    tournament.Teams.Add(new CustomTeam { TeamName = team.TeamName, TournamentId = tournament.TournamentId });
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
                    tournament.Stages.Add(new CustomMatchStage { StageName = stage.StageName, TournamentId = tournament.TournamentId, Order = stage.Order });
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

                    tournament.Matches.Add(new CustomMatch
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

                // Step 7: Handle Users Based on `recordStatus`
                var existingAssignments = tournament.Participants.ToDictionary(u => u.AssignmentId);
                var usersToRemove = tournamentDto.Users.Where(u => u.RecordStatus == "Delete").ToList();
                var usersToUpdate = tournamentDto.Users.Where(u => u.RecordStatus == "Update").ToList();
                var newUsers = tournamentDto.Users.Where(u => u.RecordStatus == "New").ToList();

                // Remove tournament assignments for users marked as Delete
                foreach (var user in usersToRemove)
                {
                    if (existingAssignments.TryGetValue(user.AssignmentId!.Value, out var assignment))
                    {
                        _context.CustomTournamentUserAssignments.Remove(assignment);
                        _logger.LogInformation($"Removed tournament assignment for user {assignment.UserAdminName} ({assignment.UserId})");
                    }
                }

                // Update existing user assignments (Role Change, Admin Name Change)
                foreach (var user in usersToUpdate)
                {
                    if (existingAssignments.TryGetValue(user.AssignmentId!.Value, out var assignment))
                    {
                        assignment.UserAdminName = user.UserAdminName;
                        assignment.Role = Enum.Parse<UserTournamentRole>(user.UserRole);
                        _logger.LogInformation($"Updated tournament assignment for user {assignment.UserAdminName} ({assignment.UserId}): Role = {user.UserRole}");
                    }
                }

                // Process New Users (Assign to Tournament OR Register & Assign)
                var emailTasks = new List<Task>();

                foreach (var newUserDto in newUsers)
                {
                    var existingUser = await _userService.FindUserByEmailAsync(newUserDto.UserEmail);
                    ApplicationUser userToAssign;

                    if (existingUser == null)
                    {
                        // Register a new user who does not exist in the system
                        var newUser = await _registerService.RegisterInvitedUserAsync(newUserDto.UserEmail);
                        if (newUser == null) throw new Exception($"Failed to create user with email: {newUserDto.UserEmail}");

                        userToAssign = newUser;

                        // Send Account Setup Email
                        string setupLink = await GenerateAccountSetupLinkAsync(newUser);
                        emailTasks.Add(SendAccountSetupEmailAsync(newUser.Email, tournament.Name, setupLink));

                        _logger.LogInformation($"Registered new user {newUser.Email} and assigned to tournament {tournament.TournamentId}");
                    }
                    else
                    {
                        // Existing user: Only assign to tournament
                        userToAssign = existingUser;

                        // Send tournament invitation
                        string inviteLink = GenerateTournamentInviteLink(existingUser.Email, tournament.TournamentId);
                        emailTasks.Add(SendTournamentInvitationEmailAsync(existingUser.Email, tournament.Name, inviteLink));

                        _logger.LogInformation($"Assigned existing user {existingUser.Email} to tournament {tournament.TournamentId}");
                    }

                    // Assign user to tournament
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

                // Save changes before populating bets to ensure latest data is available
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Populate bets
                await _betService.CreateBetsForTournamentAsync(tournament.TournamentId);

                // Send emails in parallel
                await Task.WhenAll(emailTasks);

                return true;
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
                    .Include(t => t.Teams)
                    .Include(t => t.Matches)
                    .Include(t => t.Stages)
                    .Include(t => t.Participants)
                        .ThenInclude(p => p.User) // Include User Details
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
                    CreatedBy = tournament.CreatedByUserId,
                    CreatedAt = tournament.CreatedAt,
                    TournamentVisibility = tournament.Visibility.ToString(),
                    UpdateMethod = tournament.Update.ToString(),
                    IsActive = tournament.IsActive,
                    Teams = tournament.Teams.Select(team => new CustomTeamDto
                    {
                        TeamId = team.TeamId,
                        TeamName = team.TeamName,
                        PredefinedTeamId = team.PredefinedTeamId
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
                        MatchStart = match.MatchStart,
                        HomeWinOdds = match.HomeWinOdds,
                        DrawOdds = match.DrawOdds,
                        AwayWinOdds = match.AwayWinOdds,
                        HomeQualifies = match.HomeQualifies,
                        AwayQualifies = match.AwayQualifies
                    }).ToList(),
                    Users = tournament.Participants.Select(p => new CustomUserDto
                    {
                        AssignmentId = p.AssignmentId,  // Use AssignmentId instead of UserId
                        UserAdminName = p.UserAdminName,
                        UserName = p.UserName,
                        UserEmail = p.User.Email,
                        Status = p.Status.ToString()
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

                // Find the user assignment
                var assignment = await _context.CustomTournamentUserAssignments
                    .FirstOrDefaultAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (assignment == null)
                {
                    _logger.LogWarning($"No tournament assignment found for user {userId} in tournament ID {tournamentId}");
                    return false;
                }

                // Delete user's bets linked to this tournament
                var userBets = await _context.Bets
                    .Where(b => b.UserId == userId && _context.CustomMatches.Any(m => m.MatchId == b.MatchId && m.TournamentId == tournamentId))
                    .ToListAsync();

                if (userBets.Any())
                {
                    _context.Bets.RemoveRange(userBets);
                    _logger.LogInformation($"Deleted {userBets.Count} bets for user {userId} in tournament ID {tournamentId}");
                }

                // Remove the user from tournament assignments
                _context.CustomTournamentUserAssignments.Remove(assignment);
                _logger.LogInformation($"Removed user {userId} from tournament ID {tournamentId}");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error while quitting tournament ID {tournamentId} for user {userId}");
                throw;
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

                await _tournamentSelectionService.SetSelectedTournamentAsync(userId, tournamentAssignment.TournamentId);

                // Notify admins
                await _notificationService.NotifyUserAcceptedTournamentInviteAsync(tournamentAssignment);

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

                // Check if the user is assigned to the tournament
                var isParticipant = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (!isParticipant)
                {
                    _logger.LogWarning($"User {userId} is not assigned to tournament {tournamentId}.");
                    return null; // Unauthorized access
                }

                // Fetch all tournament bets and participants
                var tournamentBets = await _context.Bets
                    .Where(b => b.Match.TournamentId == tournamentId)
                    .Include(b => b.Match)
                    .ToListAsync();

                if (!tournamentBets.Any())
                {
                    _logger.LogWarning($"No bets found for tournament ID {tournamentId}.");
                    return new List<TournamentSummaryDto>();
                }

                // Fetch user assignments (to get UserName)
                var userAssignments = await _context.CustomTournamentUserAssignments
                    .Where(a => a.TournamentId == tournamentId)
                    .ToDictionaryAsync(a => a.UserId, a => a.UserName ?? a.UserAdminName);

                // Group by user to generate statistics
                var summary = tournamentBets
                    .GroupBy(b => b.UserId)
                    .Select(group =>
                    {
                        var userId = group.Key;
                        var bets = group.ToList();

                        var successful1X2 = bets.Count(b => b.Status == Bet.BetStatus.Finalised && b.Result == Bet.BetResult.Won);
                        var successfulQualification = bets.Count(b =>
                            b.Status == Bet.BetStatus.Finalised &&
                            b.Match.Type == CustomMatch.MatchType.ExtendedWithQualification &&
                            b.Qualified == b.Match.Qualified);
                        var successfulExactResults = bets.Count(b =>
                            b.Status == Bet.BetStatus.Finalised &&
                            b.HomeGoals == b.Match.HomeScore &&
                            b.AwayGoals == b.Match.AwayScore);
                        var totalPayout = bets.Where(b => b.Status == Bet.BetStatus.Finalised).Sum(b => b.Payout ?? 0);

                        return new TournamentSummaryDto
                        {
                            UserId = userId,
                            UserName = userAssignments.ContainsKey(userId) ? userAssignments[userId] : "Unknown",
                            TotalBetsPlaced = bets.Count,
                            Successful1X2Results = successful1X2,
                            SuccessfulQualifications = successfulQualification,
                            SuccessfulExactResults = successfulExactResults,
                            TotalPayout = totalPayout
                        };
                    })
                    .OrderByDescending(s => s.TotalPayout)
                    .ToList();

                // Assign positions based on payout ranking
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

                // Step 1: Validate that the requesting user is assigned to the tournament
                bool isParticipant = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (!isParticipant)
                {
                    _logger.LogWarning($"User {userId} is not assigned to tournament {tournamentId}.");
                    return new List<UserBettingStatsDto>(); // Unauthorized access
                }

                // Step 2: Validate that statsUserId is also part of the tournament
                bool isStatsUserParticipant = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == statsUserId);

                if (!isStatsUserParticipant)
                {
                    _logger.LogWarning($"Stats user {statsUserId} is not assigned to tournament {tournamentId}.");
                    return new List<UserBettingStatsDto>(); // Invalid stats target
                }

                // Step 3: Fetch all matches (Finalised & Non-Finalised)
                var tournamentMatches = await _context.CustomMatches
                    .Where(m => m.TournamentId == tournamentId)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Include(m => m.Bets.Where(b => b.UserId == statsUserId)) // Fetch only bets for the stats user
                    .ToListAsync();

                // Step 4: Build Stats DTO List
                var bettingStats = tournamentMatches.Select(match =>
                {
                    bool isFinalised = match.Status == CustomMatch.MatchStatus.Finalised;
                    var userBet = isFinalised ? match.Bets.FirstOrDefault() : null; // Only show bets for finalised matches

                    return new UserBettingStatsDto
                    {
                        MatchId = match.MatchId,
                        HomeTeam = match.HomeTeam.TeamName,
                        AwayTeam = match.AwayTeam.TeamName,
                        BetPlaced = userBet != null
                            ? $"{userBet.HomeGoals?.ToString() ?? "-"}:{userBet.AwayGoals?.ToString() ?? "-"}"
                            : isFinalised ? "No Bet" : "N/A",
                        BetOutcome = userBet != null
                            ? (userBet.Result == Bet.BetResult.Won ? "Won" : "Lost")
                            : isFinalised ? "N/A" : "Not Finalised",
                        WhoQualifiedBet = isFinalised ? (userBet?.Qualified?.ToString() ?? "N/A") : "N/A",
                        WhoQualifiedResult = isFinalised && match.Type == CustomMatch.MatchType.ExtendedWithQualification
                            ? match.Qualified.ToString()
                            : "N/A",
                        Payout = isFinalised ? (userBet?.Payout ?? 0) : 0 // Show 0 payout for non-finalised matches
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

        public async Task<List<TournamentPlayerResultDto>> GetTournamentPlayerResultAsync(int tournamentId, string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching tournament player results for tournament ID {tournamentId}, requested by user {userId}");

                // Check if the user is a participant
                var isParticipant = await _context.CustomTournamentUserAssignments
                    .AnyAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (!isParticipant)
                {
                    _logger.LogWarning($"User {userId} is not assigned to tournament {tournamentId}.");
                    return new List<TournamentPlayerResultDto>(); // Unauthorized access
                }

                // Fetch all bets for the tournament
                var tournamentBets = await _context.Bets
                    .Where(b => b.Match.TournamentId == tournamentId)
                    .Include(b => b.Match)
                    .Include(b => b.User)
                    .ToListAsync();

                if (!tournamentBets.Any())
                {
                    _logger.LogWarning($"No bets found for tournament ID {tournamentId}.");
                    return new List<TournamentPlayerResultDto>();
                }

                // Calculate total payout per user
                var playerResults = tournamentBets
                    .GroupBy(b => b.User)
                    .Select(group =>
                    {
                        var user = group.Key;
                        var totalPayout = group
                            .Where(b => b.Status == Bet.BetStatus.Finalised)
                            .Sum(b => b.Payout ?? 0);

                        return new TournamentPlayerResultDto
                        {
                            UserName = user.UserName,
                            Points = totalPayout,
                            IsCurrentUser = user.Id == userId
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

        public async Task<bool> TournamentNameExistsAsync(string publicTournamentName)
        {
            try
            {
                return await _context.CustomTournaments.AnyAsync(t => t.Name == publicTournamentName);  //TODO change model, add prop
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error querying database for tournament name: {publicTournamentName}");
                throw;
            }
        }

        public async Task<List<PublicTournamentDto>> GetPublicActiveTournamentsAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching public active tournaments (excluding accepted ones) for user {userId}");

                var tournaments = await _context.CustomTournaments
                    .AsNoTracking()
                    .Where(t =>
                        t.IsActive &&
                        t.Visibility == CustomTournament.TournamentVisibility.Public &&
                        !t.Participants.Any(p => p.UserId == userId && p.Status == AssignmentStatus.Accepted)
                    )
                    .Select(t => new PublicTournamentDto
                    {
                        TournamentId = t.TournamentId,
                        TournamentName = t.Name,
                        CreatedAt = t.CreatedAt,
                        Participants = t.Participants.Count,
                        JoinRequested = t.Participants.Any(p => p.UserId == userId && p.Status == AssignmentStatus.Requested)
                    })
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

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

                var userBets = await _context.Bets
                    .Where(b => b.UserId == userToExclude.Id && tournament.Matches.Select(m => m.MatchId).Contains(b.MatchId))
                    .ToListAsync();

                _context.Bets.RemoveRange(userBets);
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

                string inviteLink = GenerateTournamentInviteLink(targetUserEmail, tournamentId);
                await SendTournamentInvitationEmailAsync(targetUserEmail, tournament.Name, inviteLink);

                return ActionResultDto.SuccessResult($"Invitation resent to {targetUserEmail}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resending invite to {targetUserEmail} for tournament {tournamentId}");
                return ActionResultDto.ErrorResult("An error occurred while resending the invitation.");
            }
        }
    }
}
