using Backend.DTOs;
using Backend.Model.Entities;

namespace Backend.Repository.Interfaces
{
    public interface INotificationService
    {
        Task NotifyMatchClosureAsync(CustomMatch match);
        Task NotifyUserAcceptedTournamentInviteAsync(CustomTournamentUserAssignment assignment);
        Task NotifyAdminsJoinRequestAsync(CustomTournamentUserAssignment joinRequest);
        Task NotifyUserJoinRequestApprovedAsync(CustomTournamentUserAssignment assignment);
        Task NotifyMatchStartingSoonAsync(CustomMatch match, TimeSpan threshold);
        Task<List<string>> NotifyManualPendingBetReminderAsync(CustomMatch match, List<ApplicationUser> recipients);
        Task NotifyAdminsAboutMissingBetsAsync(DateTime nowUtc);
        Task NotifyDailyTournamentUpdatesAsync(DateTime nowUtc);
        Task NotifyNewGamesToBetAsync(CustomMatch match);
        Task NotifyTournamentInvitationsAsync(int tournamentId, IEnumerable<string> userEmails);
        Task NotifySuperAdminsAboutSupportMessageAsync(SupportMessage message);


        Task ProcessNotificationsAsync(
            List<ApplicationUser> recipients,
            string title,
            string message,
            string route,
            Func<ApplicationUser, (bool emailConsent, bool pushConsent)> getConsent);

        Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int? limit = null);
        Task<List<NotificationMessageRecipientDto>> GetTournamentMessageRecipientsAsync(int tournamentId, string userId);
        Task<bool> SendTournamentUserMessageAsync(SendTournamentUserMessageDto request, string senderUserId);
        Task<bool> ReplyToUserMessageAsync(ReplyToUserMessageDto request, string senderUserId);
        Task<int> SendAdminBroadcastMessageAsync(SendAdminBroadcastMessageDto request, string senderUserId);
        Task<bool> SendAdminUserMessageAsync(SendAdminUserMessageDto request, string senderUserId);
        Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId);
        Task<bool> DeleteNotificationAsync(int notificationId, string userId);
        Task<int> DeleteAllNotificationsAsync(string userId);
        Task<NotificationSettingsDto?> GetNotificationSettingsAsync(string userId);
        Task<bool> UpdateNotificationSettingsAsync(string userId, NotificationSettingsDto settingsDto);

    }
}
