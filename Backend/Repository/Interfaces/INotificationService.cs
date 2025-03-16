using Backend.DTOs;
using Backend.Model.Entities;

namespace Backend.Repository.Interfaces
{
    public interface INotificationService
    {
        Task NotifyMatchClosureAsync(CustomMatch match);
        Task ProcessNotificationsAsync(
            List<ApplicationUser> recipients,
            string title,
            string message,
            Func<ApplicationUser, (bool emailConsent, bool pushConsent)> getConsent);

        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, int limit);
        Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId);
    }
}
