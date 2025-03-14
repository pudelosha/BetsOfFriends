using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using static Backend.Model.Entities.CustomMatch;

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
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomTournamentService(
            AppDbContext context,
            ILogger<CustomTournamentService> logger,
            IRegisterService registerService,
            IUserService userService,
            IBetService betService,
            IEmailService emailService,
            IEmailTemplateService emailTemplateService,
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

                    // Tournament Settings Mapping
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
                    UserAdminName = creatorUser.Email,
                    Role = UserTournamentRole.Admin,
                    Status = AssignmentStatus.Accepted,
                    IsVisible = true
                });

                // Step 3: Insert Teams
                var teams = tournamentDto.Teams.Select(t => new CustomTeam
                {
                    Name = t.TeamName,
                    TournamentId = tournament.TournamentId
                }).ToList();

                _context.CustomTeams.AddRange(teams);
                await _context.SaveChangesAsync();

                // Step 4: Map Team Names to IDs
                var teamMap = await _context.CustomTeams
                    .Where(t => t.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.Name, t => t.TeamId);

                // Step 5: Insert Matches
                var matches = tournamentDto.Matches.Select(m => new CustomMatch
                {
                    TournamentId = tournament.TournamentId,
                    Stage = m.Stage,
                    HomeTeamId = teamMap.TryGetValue(m.HomeTeam, out var homeId) ? homeId : throw new Exception($"Home team '{m.HomeTeam}' not found."),
                    AwayTeamId = teamMap.TryGetValue(m.AwayTeam, out var awayId) ? awayId : throw new Exception($"Away team '{m.AwayTeam}' not found."),
                    MatchStart = DateTime.SpecifyKind(m.MatchStart, DateTimeKind.Utc),
                    Type = Enum.Parse<CustomMatch.MatchType>(m.MatchType),
                    HomeWinOdds = m.HomeWinOdds,
                    DrawOdds = m.DrawOdds,
                    AwayWinOdds = m.AwayWinOdds,
                    HomeQualifies = m.HomeQualifies ?? 0,
                    AwayQualifies = m.AwayQualifies ?? 0,
                    Status = MatchStatus.Upcoming
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
                        Role = UserTournamentRole.Guest,
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
                // Step 1: Fetch the tournament from the database
                var tournament = await _context.CustomTournaments
                    .Include(t => t.Teams)
                    .Include(t => t.Matches)
                    .Include(t => t.Participants)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentDto.TournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Custom tournament ID {tournamentDto.TournamentId} not found.");
                    return false;
                }

                // Step 2: Check if the user is an Admin in this tournament
                bool isTournamentAdmin = tournament.Participants.Any(p => p.UserId == userId && p.Role == UserTournamentRole.Admin);

                if (!isTournamentAdmin)
                {
                    _logger.LogWarning($"User {userId} attempted to update tournament {tournamentDto.TournamentId} without permission.");
                    return false; // Forbidden
                }

                // Step 3: Update tournament details and settings
                tournament.Name = tournamentDto.TournamentName;
                tournament.IsActive = tournamentDto.IsActive;

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
                var updatedTeamsWithIds = tournamentDto.Teams.Where(t => t.TeamId.HasValue).ToDictionary(t => t.TeamId.Value);
                var newTeams = tournamentDto.Teams.Where(t => !t.TeamId.HasValue).ToList();

                var teamsToRemove = existingTeams.Values.Where(et => !updatedTeamsWithIds.ContainsKey(et.TeamId)).ToList();
                foreach (var team in teamsToRemove)
                {
                    var relatedMatches = _context.CustomMatches
                        .Where(m => m.HomeTeamId == team.TeamId || m.AwayTeamId == team.TeamId)
                        .ToList();
                    _context.CustomMatches.RemoveRange(relatedMatches);
                }
                _context.CustomTeams.RemoveRange(teamsToRemove);

                foreach (var teamDto in updatedTeamsWithIds.Values)
                {
                    if (existingTeams.TryGetValue(teamDto.TeamId.Value, out var team))
                    {
                        team.Name = teamDto.TeamName;
                    }
                }

                foreach (var newTeamDto in newTeams)
                {
                    tournament.Teams.Add(new CustomTeam
                    {
                        Name = newTeamDto.TeamName,
                        TournamentId = tournament.TournamentId
                    });
                }

                await _context.SaveChangesAsync();

                // Step 5: Handle Matches
                var teamMap = await _context.CustomTeams
                    .Where(t => t.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.Name, t => t.TeamId);

                var existingMatches = tournament.Matches.ToDictionary(m => m.MatchId);
                var updatedMatchesWithIds = tournamentDto.Matches.Where(m => m.MatchId.HasValue).ToDictionary(m => m.MatchId.Value);
                var newMatches = tournamentDto.Matches.Where(m => !m.MatchId.HasValue).ToList();

                var matchesToRemove = existingMatches.Values.Where(em => !updatedMatchesWithIds.ContainsKey(em.MatchId)).ToList();
                _context.CustomMatches.RemoveRange(matchesToRemove);

                foreach (var matchDto in updatedMatchesWithIds.Values)
                {
                    if (existingMatches.TryGetValue(matchDto.MatchId.Value, out var match))
                    {
                        match.Stage = matchDto.Stage;
                        match.HomeTeamId = matchDto.HomeTeamId.Value;
                        match.AwayTeamId = matchDto.AwayTeamId.Value;
                        match.MatchStart = DateTime.SpecifyKind(matchDto.MatchStart, DateTimeKind.Utc);
                        match.Type = Enum.Parse<CustomMatch.MatchType>(matchDto.MatchType);
                        match.HomeWinOdds = matchDto.HomeWinOdds;
                        match.DrawOdds = matchDto.DrawOdds;
                        match.AwayWinOdds = matchDto.AwayWinOdds;
                        match.HomeQualifies = matchDto.HomeQualifies;
                        match.AwayQualifies = matchDto.AwayQualifies;
                    }
                }

                foreach (var newMatchDto in newMatches)
                {
                    var homeTeamId = teamMap.TryGetValue(newMatchDto.HomeTeam, out var homeId)
                        ? homeId
                        : throw new Exception($"Home team '{newMatchDto.HomeTeam}' not found.");
                    var awayTeamId = teamMap.TryGetValue(newMatchDto.AwayTeam, out var awayId)
                        ? awayId
                        : throw new Exception($"Away team '{newMatchDto.AwayTeam}' not found.");

                    var newMatch = new CustomMatch
                    {
                        TournamentId = tournament.TournamentId,
                        Stage = newMatchDto.Stage,
                        HomeTeamId = homeTeamId,
                        AwayTeamId = awayTeamId,
                        MatchStart = DateTime.SpecifyKind(newMatchDto.MatchStart, DateTimeKind.Utc),
                        Type = Enum.Parse<CustomMatch.MatchType>(newMatchDto.MatchType),
                        HomeWinOdds = newMatchDto.HomeWinOdds,
                        DrawOdds = newMatchDto.DrawOdds,
                        AwayWinOdds = newMatchDto.AwayWinOdds,
                        HomeQualifies = newMatchDto.HomeQualifies,
                        AwayQualifies = newMatchDto.AwayQualifies
                    };

                    _context.CustomMatches.Add(newMatch);
                    await _context.SaveChangesAsync();
                }

                // Step 6: Handle User Assignments (Track Changes)
                var invitedUsers = new List<ApplicationUser>(); // New users needing setup emails
                var newTournamentAssignments = new List<ApplicationUser>(); // Existing users getting a new tournament invite
                var updatedAssignments = new List<ApplicationUser>(); // Updated assignments (no email needed)

                var existingAssignments = tournament.Participants.ToDictionary(p => p.AssignmentId);
                var updatedUsers = tournamentDto.Users
                    .Where(u => u.AssignmentId.HasValue)
                    .ToDictionary(u => u.AssignmentId!.Value);
                var newUsers = tournamentDto.Users.Where(u => !u.AssignmentId.HasValue).ToList();

                var removedAssignments = existingAssignments.Values
                    .Where(ea => !updatedUsers.ContainsKey(ea.AssignmentId))
                    .ToList();

                // Remove incorrect user assignments, but do not delete users
                foreach (var assignment in removedAssignments)
                {
                    if (assignment.Status == AssignmentStatus.New || assignment.Status == AssignmentStatus.Invited)
                    {
                        _context.CustomTournamentUserAssignments.Remove(assignment);
                        _logger.LogInformation($"Removed incorrect assignment for email: {assignment.User.Email}");
                    }
                }

                foreach (var userDto in updatedUsers.Values)
                {
                    var assignment = existingAssignments[userDto.AssignmentId!.Value];

                    if (assignment.Status == AssignmentStatus.New || assignment.Status == AssignmentStatus.Invited)
                    {
                        if (!string.Equals(assignment.User.Email, userDto.UserEmail, StringComparison.OrdinalIgnoreCase))
                        {
                            var existingUser = await _userService.FindUserByEmailAsync(userDto.UserEmail);

                            if (existingUser == null)
                            {
                                var newUser = await _registerService.RegisterInvitedUserAsync(userDto.UserEmail);
                                if (newUser == null) throw new Exception($"Failed to create user with email: {userDto.UserEmail}");

                                invitedUsers.Add(newUser);

                                _context.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
                                {
                                    UserId = newUser.Id,
                                    TournamentId = tournament.TournamentId,
                                    Role = assignment.Role,
                                    Status = AssignmentStatus.Invited,
                                    IsVisible = true,
                                    UserAdminName = userDto.UserAdminName
                                });
                            }
                            else
                            {
                                _context.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
                                {
                                    UserId = existingUser.Id,
                                    TournamentId = tournament.TournamentId,
                                    Role = assignment.Role,
                                    Status = AssignmentStatus.Invited,
                                    IsVisible = true,
                                    UserAdminName = userDto.UserAdminName
                                });

                                newTournamentAssignments.Add(existingUser);
                            }

                            _context.CustomTournamentUserAssignments.Remove(assignment);
                            _logger.LogInformation($"Replaced incorrect email {assignment.User.Email} with {userDto.UserEmail}");
                        }
                        else
                        {
                            // User assignment updated, but no email needed
                            assignment.UserAdminName = userDto.UserAdminName;
                            updatedAssignments.Add(assignment.User);
                        }
                    }
                }

                // Process New Users (Users not previously assigned)
                foreach (var newUserDto in newUsers)
                {
                    var existingUser = await _userService.FindUserByEmailAsync(newUserDto.UserEmail);
                    ApplicationUser userToAssign;

                    if (existingUser == null)
                    {
                        var newUser = await _registerService.RegisterInvitedUserAsync(newUserDto.UserEmail);
                        if (newUser == null) throw new Exception($"Failed to create user with email: {newUserDto.UserEmail}");

                        invitedUsers.Add(newUser);
                        userToAssign = newUser;
                    }
                    else
                    {
                        newTournamentAssignments.Add(existingUser);
                        userToAssign = existingUser;
                    }

                    _context.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
                    {
                        UserId = userToAssign.Id,
                        TournamentId = tournament.TournamentId,
                        Role = UserTournamentRole.Guest,
                        Status = AssignmentStatus.Invited,
                        IsVisible = true,
                        UserAdminName = newUserDto.UserAdminName
                    });

                    _logger.LogInformation($"Added new user {userToAssign.Email} to tournament {tournament.TournamentId}");
                }

                // Step 7: Commit transaction before sending emails
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Step 8:  Populate bets
                await _betService.CreateBetsForTournamentAsync(tournament.TournamentId);

                // Step 9: Send Emails (Outside Transaction)
                var emailTasks = new List<Task>();

                foreach (var user in invitedUsers)
                {
                    string setupLink = await GenerateAccountSetupLinkAsync(user);
                    emailTasks.Add(SendAccountSetupEmailAsync(user.Email, tournament.Name, setupLink));
                }

                foreach (var user in newTournamentAssignments)
                {
                    string inviteLink = GenerateTournamentInviteLink(user.Email, tournament.TournamentId);
                    emailTasks.Add(SendTournamentInvitationEmailAsync(user.Email, tournament.Name, inviteLink));
                }

                // Wait for all email tasks to complete asynchronously
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
                    IsActive = tournament.IsActive,
                    Teams = tournament.Teams.Select(team => new CustomTeamDto
                    {
                        TeamId = team.TeamId,
                        TeamName = team.Name
                    }).ToList(),
                    Matches = tournament.Matches.Select(match => new CustomMatchDto
                    {
                        MatchId = match.MatchId,
                        Stage = match.Stage,
                        HomeTeamId = match.HomeTeamId,
                        HomeTeam = match.HomeTeam.Name,
                        AwayTeamId = match.AwayTeamId,
                        AwayTeam = match.AwayTeam.Name,
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
                        UserAdminName = p.User.UserName,
                        UserEmail = p.User.Email,
                        Status = p.Status.ToString()
                    }).ToList(),
                                Settings = new CustomTournamentSettingsDto
            {
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
                    .FirstOrDefaultAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (assignment == null || assignment.Status != AssignmentStatus.Invited)
                {
                    _logger.LogWarning($"No valid invitation found for user {userId} in tournament ID {tournamentId}");
                    return new TournamentInvitationResponseDto
                    {
                        Success = false,
                        Message = "Tournament invitation could not be accepted. You may not be invited."
                    };
                }

                // Accept the invitation and set the nickname
                assignment.Status = AssignmentStatus.Accepted;
                assignment.UserName = nickname;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} successfully accepted invitation for tournament ID {tournamentId} with nickname {nickname}");

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
                    .Include(b => b.User)
                    .ToListAsync();

                if (!tournamentBets.Any())
                {
                    _logger.LogWarning($"No bets found for tournament ID {tournamentId}.");
                    return new List<TournamentSummaryDto>();
                }

                // Group by user to generate statistics
                var summary = tournamentBets
                    .GroupBy(b => b.User)
                    .Select(group =>
                    {
                        var user = group.Key;
                        var bets = group.ToList();
                        var successful1X2 = bets.Count(b => b.Status == Bet.BetStatus.Finalised && b.Result == Bet.BetResult.Won);
                        var successfulQualification = bets.Count(b =>
                            b.Status == Bet.BetStatus.Finalised &&
                            b.Match.Type == CustomMatch.MatchType.ExtendedWithQualification &&
                            b.QualifiedTeam.ToString() == b.Match.Qualified.ToString());    // TODO temp solution!!
                        var successfulExactResults = bets.Count(b =>
                            b.Status == Bet.BetStatus.Finalised &&
                            b.HomeGoals == b.Match.HomeScore &&
                            b.AwayGoals == b.Match.AwayScore);
                        var totalPayout = bets.Where(b => b.Status == Bet.BetStatus.Finalised).Sum(b => b.Payout ?? 0);

                        return new TournamentSummaryDto
                        {
                            ParticipantEmail = user.Email,
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
    }
}
