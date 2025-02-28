using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Services
{
    public class CustomTournamentService : ICustomTournamentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CustomTournamentService> _logger;
        private readonly IRegisterService _registerService;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly IBetService _betService;

        public CustomTournamentService(
            AppDbContext context,
            ILogger<CustomTournamentService> logger,
            IRegisterService registerService,
            IUserService userService,
            IBetService betService,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _registerService = registerService;
            _userService = userService;
            _betService = betService;
            _emailService = emailService;
            _configuration = configuration;
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

                // Step 2: Insert Teams and Map Their Actual IDs
                var teams = tournamentDto.Teams.Select(t => new CustomTeam
                {
                    Name = t.TeamName,
                    TournamentId = tournament.TournamentId
                }).ToList();

                _context.CustomTeams.AddRange(teams);
                await _context.SaveChangesAsync();

                // Step 3: Create a Map of Team Names to Their Actual Database IDs
                var teamMap = await _context.CustomTeams
                    .Where(t => t.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.Name, t => t.TeamId);

                // Step 4: Insert Matches Using the Actual IDs from Database
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
                    AwayQualifies = m.AwayQualifies ?? 0
                }).ToList();

                _context.CustomMatches.AddRange(matches);
                await _context.SaveChangesAsync();

                // Step 5: Process User Assignments (Create users if needed)
                var invitedUsers = new List<ApplicationUser>(); // Store users who need email invitations
                foreach (var userDto in tournamentDto.Users)
                {
                    var existingUser = await _userService.FindUserByEmailAsync(userDto.UserEmail);

                    if (existingUser == null)
                    {
                        // Create user without password (invite process)
                        var newUser = await _registerService.RegisterInvitedUserAsync(userDto.UserEmail);
                        if (newUser == null)
                        {
                            throw new Exception($"Failed to create user with email: {userDto.UserEmail}");
                        }
                        existingUser = newUser;
                        invitedUsers.Add(newUser); // Add to email list
                    }

                    // Assign user to the tournament
                    var assignment = new CustomTournamentUserAssignment
                    {
                        UserId = existingUser.Id,
                        TournamentId = tournament.TournamentId,
                        UserAdminName = userDto.UserAdminName,
                        Role = UserTournamentRole.Guest, // Default role
                        Status = AssignmentStatus.Invited
                    };

                    _context.CustomTournamentUserAssignments.Add(assignment);
                }

                // Commit Transaction Before Sending Emails
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Custom tournament {tournament.Name} created successfully.");

                // Step 6: Generate Bets for Users & Matches
                await _betService.CreateBetsForTournamentAsync(tournament.TournamentId);

                // Step 7: Send Email Invitations (Outside Transaction)
                foreach (var user in invitedUsers)
                {
                    string inviteLink = GenerateTournamentInviteLink(user.Email, tournament.TournamentId);

                    string emailBody = $@"
                                    <p>Hi,</p>
                                    <p>You have been invited to join the tournament <strong>{tournament.Name}</strong>.</p>
                                    <p>Click the button below to accept the invitation and start participating:</p>
                                    <p>
                                    <a href='{inviteLink}' style='display:inline-block;padding:10px 20px;font-size:16px;color:#fff;background:#007bff;text-decoration:none;border-radius:5px;'>
                                    Accept Invitation
                                    </a>
                                    </p>
                                    <p>If you did not expect this invitation, you can ignore this email.</p>
                                    <p>Best regards,<br/>Tournament Management Team</p>";

                    _logger.LogInformation($"Sending tournament invite email to {user.Email} with invite link: {inviteLink}");

                    await _emailService.SendEmailAsync(user.Email, $"You're Invited to {tournament.Name}!", emailBody);
                }

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
            var baseUrl = _configuration["App:ClientBaseUrl"]; // Get frontend base URL
            var encodedEmail = Uri.EscapeDataString(email); // Encode email safely
            return $"{baseUrl}/accept-invite?tournamentId={tournamentId}&email={encodedEmail}";
        }

        public async Task<List<CustomTournamentListDto>> GetAllCustomTournamentsAsync()
        {
            try
            {
                var tournaments = await _context.CustomTournaments
                    .AsNoTracking()
                    .Select(t => new CustomTournamentListDto
                    {
                        TournamentId = t.TournamentId,
                        TournamentName = t.Name, // Match property name
                        CreatedAt = t.CreatedAt,
                        IsActive = t.IsActive
                    })
                    .ToListAsync();

                return tournaments;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving custom tournaments: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateCustomTournamentStatusAsync(int tournamentId, bool isActive)
        {
            try
            {
                var tournament = await _context.CustomTournaments
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Custom tournament with ID {tournamentId} not found.");
                    return false;
                }

                tournament.IsActive = isActive;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Custom tournament ID {tournamentId} status updated to {(isActive ? "active" : "inactive")}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating status for custom tournament ID {tournamentId}: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> DeleteCustomTournamentByIdAsync(int tournamentId)
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

                // Step 2: Delete Bets linked to Matches in this Tournament
                var matchIds = tournament.Matches.Select(m => m.MatchId).ToList();
                var bets = await _context.Bets.Where(b => matchIds.Contains(b.MatchId)).ToListAsync();
                if (bets.Any())
                {
                    _context.Bets.RemoveRange(bets);
                    _logger.LogInformation($"Deleted {bets.Count} bets associated with tournament ID: {tournamentId}");
                }

                // Step 3: Delete Matches
                if (tournament.Matches.Any())
                {
                    _context.CustomMatches.RemoveRange(tournament.Matches);
                    _logger.LogInformation($"Deleted {tournament.Matches.Count} matches associated with tournament ID: {tournamentId}");
                }

                // Step 4: Delete Teams
                if (tournament.Teams.Any())
                {
                    _context.CustomTeams.RemoveRange(tournament.Teams);
                    _logger.LogInformation($"Deleted {tournament.Teams.Count} teams associated with tournament ID: {tournamentId}");
                }

                // Step 5: Delete User-Tournament Assignments (Do NOT delete users!)
                if (tournament.Participants.Any())
                {
                    _context.CustomTournamentUserAssignments.RemoveRange(tournament.Participants);
                    _logger.LogInformation($"Deleted {tournament.Participants.Count} user assignments for tournament ID: {tournamentId}");
                }

                // Step 6: Delete the Tournament itself
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

        public async Task<bool> UpdateCustomTournamentAsync(CustomTournamentDto tournamentDto)
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

                // Step 2: Update tournament details
                tournament.Name = tournamentDto.TournamentName;
                tournament.IsActive = tournamentDto.IsActive;

                // Step 3: Handle Teams
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

                // Update existing teams
                foreach (var teamDto in updatedTeamsWithIds.Values)
                {
                    if (existingTeams.TryGetValue(teamDto.TeamId.Value, out var team))
                    {
                        team.Name = teamDto.TeamName;
                    }
                }

                // Add new teams
                foreach (var newTeamDto in newTeams)
                {
                    tournament.Teams.Add(new CustomTeam
                    {
                        Name = newTeamDto.TeamName,
                        TournamentId = tournament.TournamentId
                    });
                }

                await _context.SaveChangesAsync();

                // Step 4: Map team names to IDs
                var teamMap = await _context.CustomTeams
                    .Where(t => t.TournamentId == tournament.TournamentId)
                    .ToDictionaryAsync(t => t.Name, t => t.TeamId);

                // Step 5: Handle Matches
                var existingMatches = tournament.Matches.ToDictionary(m => m.MatchId);
                var updatedMatchesWithIds = tournamentDto.Matches.Where(m => m.MatchId.HasValue).ToDictionary(m => m.MatchId.Value);
                var newMatches = tournamentDto.Matches.Where(m => !m.MatchId.HasValue).ToList();

                // Remove matches not in the updated list
                var matchesToRemove = existingMatches.Values.Where(em => !updatedMatchesWithIds.ContainsKey(em.MatchId)).ToList();
                _context.CustomMatches.RemoveRange(matchesToRemove);

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

                    tournament.Matches.Add(new CustomMatch
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
                    });
                }

                // Step 6: Handle User Assignments (Correct Emails, Remove Wrong Assignments)
                var existingAssignments = tournament.Participants.ToDictionary(p => p.AssignmentId);
                var updatedUsers = tournamentDto.Users.Where(u => u.AssignmentId.HasValue).ToDictionary(u => u.AssignmentId!.Value);
                var newUsers = tournamentDto.Users.Where(u => !u.AssignmentId.HasValue).ToList();

                // Find and remove assignments that no longer exist in frontend data
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

                // Process existing users with changes
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

                // Step 7: Add New Users (Not Previously Assigned)
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

                    // Assign the new user to the tournament
                    _context.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
                    {
                        UserId = userToAssign.Id,
                        TournamentId = tournament.TournamentId,
                        Role = UserTournamentRole.Guest, // Default role
                        Status = AssignmentStatus.Invited, // New users get invited status
                        UserAdminName = newUserDto.UserAdminName
                    });

                    _logger.LogInformation($"Added new user {userToAssign.Email} to tournament {tournament.TournamentId}");
                }

                // Save the changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Step 8: Send Email Invitations to New Users (Outside Transaction)
                foreach (var user in invitedUsers)
                {
                    string inviteLink = GenerateTournamentInviteLink(user.Email, tournament.TournamentId);

                    string emailBody = $@"
                        <p>Hi {user.Email},</p>
                        <p>You have been invited to join the tournament <strong>{tournament.Name}</strong>.</p>
                        <p>Click the button below to accept the invitation and start participating:</p>
                        <p>
                        <a href='{inviteLink}' style='display:inline-block;padding:10px 20px;font-size:16px;color:#fff;background:#007bff;text-decoration:none;border-radius:5px;'>
                        Accept Invitation
                        </a>
                        </p>
                        <p>If you did not expect this invitation, you can ignore this email.</p>
                        <p>Best regards,<br/>Tournament Management Team</p>";

                    _logger.LogInformation($"Sending tournament invite email to {user.Email} with invite link: {inviteLink}");

                    await _emailService.SendEmailAsync(user.Email, $"You're Invited to {tournament.Name}!", emailBody);
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

        public async Task<CustomTournamentDto?> GetCustomTournamentByIdAsync(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Fetching custom tournament with ID: {tournamentId}");

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

                _logger.LogInformation($"Successfully fetched custom tournament with ID: {tournamentId}");
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching custom tournament with ID: {tournamentId}");
                throw;
            }
        }
    }
}
