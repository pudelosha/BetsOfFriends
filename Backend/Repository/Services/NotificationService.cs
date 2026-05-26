using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILocalizationService _localizationService;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<NotificationService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILocalizationService localizationService,
        IPushNotificationService pushNotificationService,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _userManager = userManager;
        _emailTemplateService = emailTemplateService;
        _localizationService = localizationService;
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    public async Task NotifyMatchStartingSoonAsync(CustomMatch match, TimeSpan threshold)
    {
        // Get users who have a "ToPlace" bet for this match (means they haven't submitted it yet)
        var usersWithToPlaceBets = await _dbContext.Bets
            .Where(b =>
                b.MatchId == match.MatchId &&
                b.Status == Bet.BetStatus.ToPlace &&
                b.Match.Tournament.Participants.Any(a => a.UserId == b.UserId && a.Status == AssignmentStatus.Accepted))
            .Include(b => b.User)
            .Select(b => b.User)
            .Distinct()
            .ToListAsync();

        if (!usersWithToPlaceBets.Any())
        {
            _logger.LogInformation($"No pending bets to place for Match ID {match.MatchId}. No reminders needed.");
            return;
        }

        var isOneHour = threshold.TotalHours == 1;

        _logger.LogInformation($"Sending match start reminders to {usersWithToPlaceBets.Count} users for Match ID {match.MatchId} (threshold: {threshold.TotalHours}h)");

        // Build dynamic URL
        var tournamentId = match.TournamentId;
        var stageNameEncoded = Uri.EscapeDataString(match.Stage?.StageName ?? ""); // fallback to empty
        var url = $"/my-bets?tab=to-place&stage={stageNameEncoded}&tournamentId={tournamentId}";

        await ProcessLocalizedNotificationsAsync(
            usersWithToPlaceBets,
            isOneHour ? "Notifications.PendingBet.Title1Hour" : "Notifications.PendingBet.Title24Hours",
            isOneHour ? "Notifications.PendingBet.Message1Hour" : "Notifications.PendingBet.Message24Hours",
            new Dictionary<string, string>
            {
                { "HOME_TEAM", match.HomeTeam?.TeamName ?? "Home team" },
                { "AWAY_TEAM", match.AwayTeam?.TeamName ?? "Away team" }
            },
            url,
            u => (u.ReceiveEmailPendingBets, u.ReceivePushPendingBets)
        );
    }


    public async Task NotifyMatchClosureAsync(CustomMatch match)
    {
        var fullMatch = await _dbContext.CustomMatches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Stage)
            .FirstOrDefaultAsync(m => m.MatchId == match.MatchId);

        if (fullMatch == null)
        {
            _logger.LogWarning($"Match ID {match.MatchId} not found. Skipping result recalculation notifications.");
            return;
        }

        var tournamentId = fullMatch.TournamentId;

        var participants = await _dbContext.CustomTournamentUserAssignments
            .Where(a => a.TournamentId == tournamentId && a.Status == AssignmentStatus.Accepted)
            .Select(a => a.User)
            .ToListAsync();

        if (!participants.Any())
        {
            _logger.LogWarning($"No accepted participants found for Tournament ID {tournamentId}. Skipping result recalculation notifications.");
            return;
        }

        _logger.LogInformation($"Sending result recalculation notifications to {participants.Count} users for Match ID {fullMatch.MatchId}");

        var homeTeamName = fullMatch.HomeTeam?.TeamName ?? "Home team";
        var awayTeamName = fullMatch.AwayTeam?.TeamName ?? "Away team";

        await ProcessLocalizedNotificationsAsync(
            participants,
            "Notifications.ResultsUpdated.Title",
            "Notifications.ResultsUpdated.Message",
            new Dictionary<string, string>
            {
                { "HOME_TEAM", homeTeamName },
                { "AWAY_TEAM", awayTeamName },
                { "SCORE", $"{fullMatch.HomeScore}-{fullMatch.AwayScore}" }
            },
            "/results",
            user => (user.ReceiveEmailMatchClosed, user.ReceivePushMatchClosed)
        );
    }

    public async Task NotifyDailyTournamentUpdatesAsync(DateTime nowUtc)
    {
        var windowEndUtc = nowUtc.AddHours(24);
        var rows = await _dbContext.CustomTournamentUserAssignments
            .Where(a => a.Status == AssignmentStatus.Accepted)
            .SelectMany(a => a.Tournament.Matches
                .Where(m =>
                    m.Status == CustomMatch.MatchStatus.Timed &&
                    m.MatchStart >= nowUtc &&
                    m.MatchStart < windowEndUtc)
                .Select(m => new
                {
                    a.UserId,
                    a.User,
                    m.MatchId
                }))
            .ToListAsync();

        var groupedUsers = rows
            .GroupBy(r => r.UserId)
            .Select(g => new
            {
                User = g.First().User,
                Count = g.Select(r => r.MatchId).Distinct().Count()
            })
            .Where(x => x.Count > 0)
            .ToList();

        if (!groupedUsers.Any())
        {
            _logger.LogInformation("No upcoming matches found for daily tournament updates.");
            return;
        }

        var userIds = groupedUsers.Select(g => g.User.Id).ToList();
        var dayStartUtc = nowUtc.Date;
        var dayEndUtc = dayStartUtc.AddDays(1);

        var alreadyNotifiedUserIds = await _dbContext.NotificationRecipients
            .Where(nr =>
                userIds.Contains(nr.UserId) &&
                nr.Notification.Route == "/my-bets?tab=to-place" &&
                nr.Notification.CreatedAt >= dayStartUtc &&
                nr.Notification.CreatedAt < dayEndUtc)
            .Select(nr => nr.UserId)
            .Distinct()
            .ToListAsync();

        var alreadyNotified = alreadyNotifiedUserIds.ToHashSet();
        var usersToNotify = groupedUsers
            .Where(g => !alreadyNotified.Contains(g.User.Id))
            .ToList();

        foreach (var countGroup in usersToNotify.GroupBy(g => g.Count))
        {
            var count = countGroup.Key;
            await ProcessLocalizedNotificationsAsync(
                countGroup.Select(g => g.User).ToList(),
                "Notifications.DailyUpdate.Title",
                count == 1 ? "Notifications.DailyUpdate.MessageOne" : "Notifications.DailyUpdate.MessageMany",
                new Dictionary<string, string> { { "COUNT", count.ToString() } },
                "/my-bets?tab=to-place",
                user => (user.ReceiveEmailDailyUpdates, user.ReceivePushDailyUpdates)
            );
        }
    }

    public async Task NotifyNewGamesToBetAsync(CustomMatch match)
    {
        var fullMatch = await _dbContext.CustomMatches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Stage)
            .FirstOrDefaultAsync(m => m.MatchId == match.MatchId);

        if (fullMatch == null)
        {
            _logger.LogWarning($"Match ID {match.MatchId} not found. Skipping new game notifications.");
            return;
        }

        var participants = await _dbContext.CustomTournamentUserAssignments
            .Where(a => a.TournamentId == fullMatch.TournamentId && a.Status == AssignmentStatus.Accepted)
            .Select(a => a.User)
            .ToListAsync();

        if (!participants.Any())
        {
            _logger.LogInformation($"No accepted participants found for Tournament ID {fullMatch.TournamentId}. Skipping new game notifications.");
            return;
        }

        var stageNameEncoded = Uri.EscapeDataString(fullMatch.Stage?.StageName ?? "");
        var homeTeamName = fullMatch.HomeTeam?.TeamName ?? "Home team";
        var awayTeamName = fullMatch.AwayTeam?.TeamName ?? "Away team";

        await ProcessLocalizedNotificationsAsync(
            participants,
            "Notifications.NewGame.Title",
            "Notifications.NewGame.Message",
            new Dictionary<string, string>
            {
                { "HOME_TEAM", homeTeamName },
                { "AWAY_TEAM", awayTeamName }
            },
            $"/my-bets?tab=to-place&stage={stageNameEncoded}&tournamentId={fullMatch.TournamentId}",
            user => (user.ReceiveEmailNewGames, user.ReceivePushNewGames)
        );
    }

    public async Task NotifyTournamentInvitationsAsync(int tournamentId, IEnumerable<string> userEmails)
    {
        var emails = userEmails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (!emails.Any())
        {
            return;
        }

        var tournamentName = await _dbContext.CustomTournaments
            .Where(t => t.TournamentId == tournamentId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(tournamentName))
        {
            _logger.LogWarning($"Tournament ID {tournamentId} not found. Skipping invitation notifications.");
            return;
        }

        var recipients = await _dbContext.Users
            .Where(u => u.Email != null && emails.Contains(u.Email.ToLower()))
            .ToListAsync();

        if (!recipients.Any())
        {
            _logger.LogInformation($"No registered users found for tournament invitation notifications in Tournament ID {tournamentId}.");
            return;
        }

        await ProcessLocalizedNotificationsAsync(
            recipients,
            "Notifications.TournamentInvitation.Title",
            "Notifications.TournamentInvitation.Message",
            new Dictionary<string, string> { { "TOURNAMENT_NAME", tournamentName } },
            "/my-tournaments",
            user => (user.ReceiveEmailTournamentInvitation, user.ReceivePushTournamentInvitation)
        );
    }

    public async Task NotifyUserAcceptedTournamentInviteAsync(CustomTournamentUserAssignment assignment)
    {
        var tournamentId = assignment.TournamentId;

        // Load full user details for the accepted user (to get email)
        var acceptedUser = await _dbContext.Users
            .Where(u => u.Id == assignment.UserId)
            .FirstOrDefaultAsync();

        if (acceptedUser == null)
        {
            _logger.LogWarning($"Accepted user with ID {assignment.UserId} not found. Cannot send notifications.");
            return;
        }

        // Get all tournament admins
        var admins = await _dbContext.CustomTournamentUserAssignments
            .Where(a => a.TournamentId == tournamentId && a.Role == UserTournamentRole.Admin)
            .Select(a => a.User)
            .ToListAsync();

        if (!admins.Any())
        {
            _logger.LogWarning($"No admins found for Tournament ID {tournamentId}. Skipping invite acceptance notifications.");
            return;
        }

        var displayName = $"{assignment.UserName} ({acceptedUser.Email})";

        _logger.LogInformation($"Sending tournament invite acceptance notifications to {admins.Count} admin(s) for Tournament ID {tournamentId}. Accepted by: {displayName}");

        await ProcessLocalizedNotificationsAsync(
            admins,
            "Notifications.UserJoined.Title",
            "Notifications.UserJoined.Message",
            new Dictionary<string, string> { { "DISPLAY_NAME", displayName } },
            "/tournaments/participants",
            user => (user.ReceiveEmailTournamentInvitation, user.ReceivePushTournamentInvitation)
        );
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int? limit = null)
    {
        try
        {
            int maxResults = limit ?? int.MaxValue; // If limit is null, fetch all notifications

            return await _dbContext.NotificationRecipients
                .Where(nr => nr.UserId == userId)
                .OrderByDescending(nr => nr.Notification.CreatedAt)
                .Take(maxResults)
                .Select(nr => new NotificationDto
                {
                    NotificationId = nr.NotificationId,
                    Title = nr.Notification.Title,
                    Message = nr.Notification.Message,
                    Route = nr.Notification.Route,
                    CreatedAt = DateTime.SpecifyKind(nr.Notification.CreatedAt, DateTimeKind.Utc),
                    IsRead = nr.IsRead
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching notifications for user {userId}: {ex.Message}");
            return new List<NotificationDto>(); // Return empty list on failure
        }
    }

    public async Task NotifyAdminsJoinRequestAsync(CustomTournamentUserAssignment joinRequest)
    {
        var tournamentId = joinRequest.TournamentId;

        var populatedJoinRequest = await _dbContext.CustomTournamentUserAssignments
            .Include(a => a.Tournament)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.TournamentId == tournamentId && a.UserId == joinRequest.UserId);

        if (populatedJoinRequest == null)
        {
            _logger.LogWarning($"Join request not found for user {joinRequest.UserId} in tournament {tournamentId}");
            return;
        }

        var admins = await _dbContext.CustomTournamentUserAssignments
            .Where(a => a.TournamentId == tournamentId && a.Role == UserTournamentRole.Admin)
            .Include(a => a.User)
            .Select(a => a.User)
            .ToListAsync();

        if (!admins.Any())
        {
            _logger.LogWarning($"No admins found for Tournament ID {tournamentId}. Cannot notify about join request.");
            return;
        }

        _logger.LogInformation($"Sending join request notifications to {admins.Count} admins for Tournament ID {tournamentId}");

        await ProcessLocalizedNotificationsAsync(
            admins,
            "Notifications.JoinRequest.Title",
            "Notifications.JoinRequest.Message",
            new Dictionary<string, string>
            {
                { "USER_NAME", populatedJoinRequest.UserName ?? populatedJoinRequest.User.Email ?? "User" },
                { "TOURNAMENT_NAME", populatedJoinRequest.Tournament.Name }
            },
            "/tournaments/participants",
            user => (user.ReceiveEmailTournamentInvitation, user.ReceivePushTournamentInvitation)
        );
    }

    public async Task NotifyUserJoinRequestApprovedAsync(CustomTournamentUserAssignment assignment)
    {
        var fullAssignment = await _dbContext.CustomTournamentUserAssignments
            .Include(a => a.Tournament)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.TournamentId == assignment.TournamentId && a.UserId == assignment.UserId);

        if (fullAssignment?.User == null)
        {
            _logger.LogWarning($"User not found for UserId {assignment.UserId}. Cannot send join approval notification.");
            return;
        }

        _logger.LogInformation($"Sending join approval notification to user {fullAssignment.User.Id} for Tournament ID {fullAssignment.TournamentId}");

        await ProcessLocalizedNotificationsAsync(
            new List<ApplicationUser> { fullAssignment.User },
            "Notifications.JoinApproved.Title",
            "Notifications.JoinApproved.Message",
            new Dictionary<string, string> { { "TOURNAMENT_NAME", fullAssignment.Tournament.Name } },
            "/my-bets",
            u => (u.ReceiveEmailTournamentInvitation, u.ReceivePushTournamentInvitation)
        );
    }

    public async Task NotifySuperAdminsAboutSupportMessageAsync(SupportMessage message)
    {
        // Step 1: Get all users with the "SuperAdmin" role
        var superAdminRole = "SuperAdmin";
        var superAdmins = await (from user in _dbContext.Users
                                 join userRole in _dbContext.UserRoles on user.Id equals userRole.UserId
                                 join role in _dbContext.Roles on userRole.RoleId equals role.Id
                                 where role.Name == superAdminRole
                                 select user)
                                 .Distinct()
                                 .ToListAsync();

        if (!superAdmins.Any())
        {
            _logger.LogWarning("No SuperAdmin users found to notify about support message.");
            return;
        }

        _logger.LogInformation($"Sending support notification to {superAdmins.Count} SuperAdmins.");

        var subjectPreview = message.Subject.Length > 40
            ? message.Subject.Substring(0, 40) + "..."
            : message.Subject;

        await ProcessLocalizedNotificationsAsync(
            superAdmins,
            "Notifications.SupportMessage.Title",
            "Notifications.SupportMessage.Message",
            new Dictionary<string, string>
            {
                { "EMAIL", message.Email },
                { "SUBJECT", subjectPreview }
            },
            "/admin/support-messages",
            u => (true, false)
        );
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId)
    {
        var notificationRecipient = await _dbContext.NotificationRecipients
            .FirstOrDefaultAsync(nr => nr.NotificationId == notificationId && nr.UserId == userId);

        if (notificationRecipient == null)
        {
            _logger.LogWarning($"Notification {notificationId} not found for user {userId}");
            return false;
        }

        notificationRecipient.IsRead = true;
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
    {
        try
        {
            var notificationRecipient = await _dbContext.NotificationRecipients
                .FirstOrDefaultAsync(nr => nr.NotificationId == notificationId && nr.UserId == userId);

            if (notificationRecipient == null)
            {
                _logger.LogWarning($"Notification {notificationId} not found for user {userId}");
                return false;
            }

            _dbContext.NotificationRecipients.Remove(notificationRecipient);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Notification {notificationId} deleted for user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting notification {notificationId} for user {userId}");
            return false;
        }
    }

    public async Task ProcessNotificationsAsync(
        List<ApplicationUser> recipients,
        string title,
        string message,
        string route,
        Func<ApplicationUser, (bool emailConsent, bool pushConsent)> getConsent)
    {
        if (recipients == null || !recipients.Any())
        {
            _logger.LogWarning("ProcessNotificationsAsync was called with an empty recipient list.");
            return;
        }

        await ProcessNotificationGroupAsync(recipients, title, message, route, getConsent, "en");
    }

    private async Task ProcessLocalizedNotificationsAsync(
        List<ApplicationUser> recipients,
        string titleKey,
        string messageKey,
        IReadOnlyDictionary<string, string> placeholders,
        string route,
        Func<ApplicationUser, (bool emailConsent, bool pushConsent)> getConsent)
    {
        if (recipients == null || !recipients.Any())
        {
            _logger.LogWarning("ProcessLocalizedNotificationsAsync was called with an empty recipient list.");
            return;
        }

        var languageByUserId = await GetRecipientLanguagesAsync(recipients);

        foreach (var languageGroup in recipients.GroupBy(user => languageByUserId.GetValueOrDefault(user.Id, "en")))
        {
            var language = languageGroup.Key;
            var title = _localizationService.Translate(titleKey, language, placeholders);
            var message = _localizationService.Translate(messageKey, language, placeholders);

            await ProcessNotificationGroupAsync(
                languageGroup.ToList(),
                title,
                message,
                route,
                getConsent,
                language);
        }
    }

    private async Task ProcessNotificationGroupAsync(
        List<ApplicationUser> recipients,
        string title,
        string message,
        string route,
        Func<ApplicationUser, (bool emailConsent, bool pushConsent)> getConsent,
        string language)
    {
        _logger.LogInformation($"Processing notifications for {recipients.Count} users in language {language}.");

        // Step 1: Create a single Notification entry
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Route = route,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(); // Ensure Notification ID is generated

        var notificationRecipients = new List<NotificationRecipient>();

        // Step 2: Process each recipient
        foreach (var user in recipients)
        {
            var (emailConsent, pushConsent) = getConsent(user);

            var recipient = new NotificationRecipient
            {
                UserId = user.Id,
                NotificationId = notification.Id, // Use the generated ID
                IsRead = false,
                SentEmail = false,
                SentPush = false
            };

            notificationRecipients.Add(recipient);

            // Step 3: Send Emails (if consent is given)
            if (emailConsent)
            {
                try
                {
                    await _emailService.SendNotificationEmailAsync(user, title, message, route, language);
                    recipient.SentEmail = true;
                    _logger.LogInformation($"Notification email sent to {user.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send email to {user.Email}");
                }
            }

            // Step 4: Send Push Notifications (if consent is given)
            if (pushConsent)
            {
                try
                {
                    recipient.SentPush = await _pushNotificationService.SendPushAsync(user.Id, title, message, route);
                    _logger.LogInformation($"Push notification processed for UserId: {user.Id}. Sent: {recipient.SentPush}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send push notification to UserId: {user.Id}");
                }
            }
        }

        // Step 5: Bulk Insert Recipients for Efficiency
        _dbContext.NotificationRecipients.AddRange(notificationRecipients);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Successfully processed notifications for {recipients.Count} users.");
    }

    private async Task<Dictionary<string, string>> GetRecipientLanguagesAsync(List<ApplicationUser> recipients)
    {
        var userIds = recipients
            .Select(user => user.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        return await _dbContext.Users
            .Where(user => userIds.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                Language = user.Language != null ? user.Language.ShortName : "en"
            })
            .ToDictionaryAsync(user => user.Id, user => user.Language ?? "en");
    }

    public async Task<NotificationSettingsDto?> GetNotificationSettingsAsync(string userId)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        return new NotificationSettingsDto
        {
            ReceiveEmailMatchClosed = user.ReceiveEmailMatchClosed,
            ReceivePushMatchClosed = user.ReceivePushMatchClosed,
            ReceiveEmailDailyUpdates = user.ReceiveEmailDailyUpdates,
            ReceivePushDailyUpdates = user.ReceivePushDailyUpdates,
            ReceiveEmailTournamentInvitation = user.ReceiveEmailTournamentInvitation,
            ReceivePushTournamentInvitation = user.ReceivePushTournamentInvitation,
            ReceiveEmailPendingBets = user.ReceiveEmailPendingBets,
            ReceivePushPendingBets = user.ReceivePushPendingBets,
            ReceiveEmailNewGames = user.ReceiveEmailNewGames,
            ReceivePushNewGames = user.ReceivePushNewGames,
            ReceiveEmailSpecialOffers = user.ReceiveEmailSpecialOffers,
            ReceivePushSpecialOffers = user.ReceivePushSpecialOffers
        };
    }

    public async Task<bool> UpdateNotificationSettingsAsync(string userId, NotificationSettingsDto settingsDto)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return false;

        user.ReceiveEmailMatchClosed = settingsDto.ReceiveEmailMatchClosed;
        user.ReceivePushMatchClosed = settingsDto.ReceivePushMatchClosed;
        user.ReceiveEmailDailyUpdates = settingsDto.ReceiveEmailDailyUpdates;
        user.ReceivePushDailyUpdates = settingsDto.ReceivePushDailyUpdates;
        user.ReceiveEmailTournamentInvitation = settingsDto.ReceiveEmailTournamentInvitation;
        user.ReceivePushTournamentInvitation = settingsDto.ReceivePushTournamentInvitation;
        user.ReceiveEmailPendingBets = settingsDto.ReceiveEmailPendingBets;
        user.ReceivePushPendingBets = settingsDto.ReceivePushPendingBets;
        user.ReceiveEmailNewGames = settingsDto.ReceiveEmailNewGames;
        user.ReceivePushNewGames = settingsDto.ReceivePushNewGames;
        user.ReceiveEmailSpecialOffers = settingsDto.ReceiveEmailSpecialOffers;
        user.ReceivePushSpecialOffers = settingsDto.ReceivePushSpecialOffers;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }
}
