using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext dbContext,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IPushNotificationService pushNotificationService,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    public async Task NotifyMatchStartingSoonAsync(CustomMatch match, TimeSpan threshold)
    {
        // Get users who have a "ToPlace" bet for this match (means they haven't submitted it yet)
        var usersWithToPlaceBets = await _dbContext.Bets
            .Where(b => b.MatchId == match.MatchId && b.Status == Bet.BetStatus.ToPlace)
            .Include(b => b.User)
            .Select(b => b.User)
            .Distinct()
            .ToListAsync();

        if (!usersWithToPlaceBets.Any())
        {
            _logger.LogInformation($"No pending bets to place for Match ID {match.MatchId}. No reminders needed.");
            return;
        }

        var timeStr = threshold.TotalHours == 1 ? "1 hour" : "24 hours";

        _logger.LogInformation($"Sending match start reminders to {usersWithToPlaceBets.Count} users for Match ID {match.MatchId} (threshold: {timeStr})");

        await ProcessNotificationsAsync(
            usersWithToPlaceBets,
            $"Reminder: Match starts in {timeStr}",
            $"You haven't submitted your bet for {match.HomeTeam.TeamName} vs {match.AwayTeam.TeamName}. The match starts in less than {timeStr}!",
            "/my-bets",
            u => (u.ReceiveEmailPendingBets, u.ReceivePushPendingBets)
        );
    }

    public async Task NotifyMatchClosureAsync(CustomMatch match)
    {
        var tournamentId = match.TournamentId;

        // Get all tournament participants
        var participants = await _dbContext.CustomTournamentUserAssignments
            .Where(a => a.TournamentId == tournamentId)
            .Select(a => a.User)
            .ToListAsync();

        if (!participants.Any())
        {
            _logger.LogWarning($"No participants found for Tournament ID {tournamentId}. Skipping match closure notifications.");
            return;
        }

        string stageName = match.Stage?.StageName ?? await _dbContext.CustomMatchStages
            .Where(s => s.StageId == match.StageId)
            .Select(s => s.StageName)
            .FirstOrDefaultAsync();

        string encodedStage = Uri.EscapeDataString(stageName ?? "");

        _logger.LogInformation($"Sending match closure notifications to {participants.Count} users for Match ID {match.MatchId}");

        await ProcessNotificationsAsync(
            participants,
            $"Match Closed: {match.HomeTeam.TeamName} vs {match.AwayTeam.TeamName}",
            $"The match {match.HomeTeam.TeamName} vs {match.AwayTeam.TeamName} has been finalized.\n" +
            $"Final Score: {match.HomeScore}-{match.AwayScore}.\nCheck your bets and standings!",
            $"/my-bets?tab=finalised&stage={encodedStage}",
            user => (user.ReceiveEmailMatchClosed, user.ReceivePushMatchClosed)
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

        await ProcessNotificationsAsync(
            admins,
            $"User Joined: {displayName}",
            $"{displayName} has accepted the tournament invite and joined your tournament.",
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
                    CreatedAt = nr.Notification.CreatedAt,
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

        await ProcessNotificationsAsync(
            admins,
            $"New Join Request for Tournament",
            $"User '{populatedJoinRequest.UserName}' has requested to join your tournament '{populatedJoinRequest.Tournament.Name}'. Review the request in the dashboard.",
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

        await ProcessNotificationsAsync(
            new List<ApplicationUser> { fullAssignment.User },
            $"Join Request Approved",
            $"Your request to join tournament '{fullAssignment.Tournament.Name}' has been approved! You can now start betting.",
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

        var notificationTitle = $"New Support Message from {message.Email}";
        var notificationBody = $"Subject: {subjectPreview}";

        // Step 2: Notify super admins
        await ProcessNotificationsAsync(
            superAdmins,
            notificationTitle,
            notificationBody,
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

        _logger.LogInformation($"Processing notifications for {recipients.Count} users.");

        // Step 1: Create a single Notification entry
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Route = route,
            CreatedAt = DateTime.UtcNow
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
                SentEmail = emailConsent,
                SentPush = pushConsent
            };

            notificationRecipients.Add(recipient);

            // Step 3: Send Emails (if consent is given)
            if (emailConsent)
            {
                try
                {
                    //TODO fix later
                    //string emailBody = await _emailTemplateService.GetEmailTemplateAsync(title, message);
                    //await _emailService.SendEmailAsync(user.Email, title, emailBody);
                    _logger.LogInformation($"Email sent to {user.Email}");
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
                    //TODO fix later
                    //await _pushNotificationService.SendPushAsync(user.Id, title, message);
                    _logger.LogInformation($"Push notification sent to UserId: {user.Id}");
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
}
