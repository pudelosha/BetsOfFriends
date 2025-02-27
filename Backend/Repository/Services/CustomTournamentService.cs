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
                        var newUser = await _registerService.RegisterInvitedUserAsync(userDto.UserEmail, userDto.UserName);
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
                        Role = UserTournamentRole.Guest, // Default role
                        IsConfirmed = false // Awaiting confirmation
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
                                    <p>Hi {user.UserName},</p>
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



    }
}
