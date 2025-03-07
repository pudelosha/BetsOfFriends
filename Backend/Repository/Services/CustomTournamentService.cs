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
                // Step 1: Insert Tournament
                var tournament = new CustomTournament
                {
                    Name = tournamentDto.TournamentName,
                    IsActive = tournamentDto.IsActive,
                    CreatedByUserId = tournamentDto.CreatedBy,
                    CreatedAt = DateTime.UtcNow
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
                    BetType = m.BetType,
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

                // Step 3: Update tournament details
                tournament.Name = tournamentDto.TournamentName;
                tournament.IsActive = tournamentDto.IsActive;

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
                        match.BetType = matchDto.BetType;
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
                        BetType = newMatchDto.BetType,
                        HomeWinOdds = newMatchDto.HomeWinOdds,
                        DrawOdds = newMatchDto.DrawOdds,
                        AwayWinOdds = newMatchDto.AwayWinOdds,
                        HomeQualifies = newMatchDto.HomeQualifies,
                        AwayQualifies = newMatchDto.AwayQualifies
                    };

                    _context.CustomMatches.Add(newMatch);
                    await _context.SaveChangesAsync();

                    // Call BetsService to generate bets for this match
                    await _betService.GenerateBetsForNewMatchAsync(newMatch.MatchId, tournament.TournamentId);
                }

                // Step 6: Handle User Assignments
                var existingAssignments = tournament.Participants.ToDictionary(p => p.AssignmentId);
                var updatedUsers = tournamentDto.Users.Where(u => u.AssignmentId.HasValue).ToDictionary(u => u.AssignmentId!.Value);
                var newUsers = tournamentDto.Users.Where(u => !u.AssignmentId.HasValue).ToList();

                var removedAssignments = existingAssignments.Values.Where(ea => !updatedUsers.ContainsKey(ea.AssignmentId)).ToList();
                foreach (var assignment in removedAssignments)
                {
                    if (assignment.Status == AssignmentStatus.New || assignment.Status == AssignmentStatus.Invited)
                    {
                        _context.CustomTournamentUserAssignments.Remove(assignment);
                        _logger.LogInformation($"Removed incorrect user assignment for email: {assignment.User.Email}");
                    }
                }

                var invitedUsers = new List<ApplicationUser>();

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
                            }

                            _context.CustomTournamentUserAssignments.Remove(assignment);
                            _logger.LogInformation($"Replaced incorrect email {assignment.User.Email} with {userDto.UserEmail}");
                        }
                        else
                        {
                            assignment.UserAdminName = userDto.UserAdminName;
                        }
                    }
                }

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

                // Step 8: Send Email Invitations (Outside Transaction)
                foreach (var user in invitedUsers)
                {
                    string inviteLink = GenerateTournamentInviteLink(user.Email, tournament.TournamentId);
                    await SendTournamentInvitationEmailAsync(user.Email, tournament.Name, inviteLink);
                }

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
                        BetType = match.BetType,
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
                    }).ToList()
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

        public async Task<bool> AcceptTournamentInvitationAsync(int tournamentId, string userId)
        {
            try
            {
                _logger.LogInformation($"User {userId} attempting to accept invitation for tournament ID {tournamentId}");

                // Find the user assignment
                var assignment = await _context.CustomTournamentUserAssignments
                    .FirstOrDefaultAsync(a => a.TournamentId == tournamentId && a.UserId == userId);

                if (assignment == null || assignment.Status != AssignmentStatus.Invited)
                {
                    _logger.LogWarning($"No valid invitation found for user {userId} in tournament ID {tournamentId}");
                    return false;
                }

                // Update assignment status to Accepted
                assignment.Status = AssignmentStatus.Accepted;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} has accepted the invitation for tournament ID {tournamentId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while accepting invitation for tournament ID {tournamentId}");
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
    }
}
