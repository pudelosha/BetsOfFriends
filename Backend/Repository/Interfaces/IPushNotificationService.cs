using Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface IPushNotificationService
    {
        bool IsConfigured { get; }
        string? GetPublicKey();
        Task UpsertSubscriptionAsync(string userId, PushSubscriptionDto subscription);
        Task RemoveSubscriptionAsync(string userId, PushSubscriptionDeleteDto subscription);
        Task<bool> SendPushAsync(string userId, string title, string message, string route);
    }
}
