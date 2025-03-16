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

        _logger.LogInformation($"Sending match closure notifications to {participants.Count} users for Match ID {match.MatchId}");

        await ProcessNotificationsAsync(
            participants,
            $"Match Closed: {match.HomeTeam.Name} vs {match.AwayTeam.Name}",
            $"The match {match.HomeTeam.Name} vs {match.AwayTeam.Name} has been finalized.\n" +
            $"Final Score: {match.HomeScore}-{match.AwayScore}.\nCheck your bets and standings!",
            user => (user.ReceiveEmailMatchClosed, user.ReceivePushMatchClosed)
        );
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, int limit)
    {
        return await _dbContext.NotificationRecipients
            .Where(nr => nr.UserId == userId)
            .OrderByDescending(nr => nr.Notification.CreatedAt)
            .Take(limit)
            .Select(nr => new NotificationDto
            {
                NotificationId = nr.NotificationId,
                Title = nr.Notification.Title,
                CreatedAt = nr.Notification.CreatedAt,
                IsRead = nr.IsRead
            })
            .ToListAsync();
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

        notificationRecipient.IsRead = true; // <-- Fixed here
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task ProcessNotificationsAsync(
        List<ApplicationUser> recipients,
        string title,
        string message,
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
