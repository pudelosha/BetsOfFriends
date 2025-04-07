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
        Task NotifySuperAdminsAboutSupportMessageAsync(SupportMessage message);


        Task ProcessNotificationsAsync(
            List<ApplicationUser> recipients,
            string title,
            string message,
            string route,
            Func<ApplicationUser, (bool emailConsent, bool pushConsent)> getConsent);

        Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int? limit = null);
        Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId);
        Task<bool> DeleteNotificationAsync(int notificationId, string userId);
    }
}
