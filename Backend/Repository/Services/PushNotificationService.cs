using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace Backend.Repository.Services
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(
            AppDbContext dbContext,
            IConfiguration configuration,
            ILogger<PushNotificationService> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(GetPublicKey()) &&
            !string.IsNullOrWhiteSpace(GetPrivateKey()) &&
            !string.IsNullOrWhiteSpace(GetSubject());

        public string? GetPublicKey() => _configuration["PushNotifications:VapidPublicKey"];

        public async Task UpsertSubscriptionAsync(string userId, PushSubscriptionDto subscription)
        {
            if (string.IsNullOrWhiteSpace(subscription.Endpoint) ||
                string.IsNullOrWhiteSpace(subscription.Keys?.P256dh) ||
                string.IsNullOrWhiteSpace(subscription.Keys?.Auth))
            {
                throw new ArgumentException("Push subscription endpoint and keys are required.");
            }

            var endpointHash = ComputeEndpointHash(subscription.Endpoint);
            var existing = await _dbContext.UserPushSubscriptions
                .FirstOrDefaultAsync(s => s.EndpointHash == endpointHash);

            if (existing == null)
            {
                existing = new UserPushSubscription
                {
                    Endpoint = subscription.Endpoint,
                    EndpointHash = endpointHash
                };

                _dbContext.UserPushSubscriptions.Add(existing);
            }

            existing.UserId = userId;
            existing.Endpoint = subscription.Endpoint;
            existing.P256dh = subscription.Keys.P256dh;
            existing.Auth = subscription.Keys.Auth;
            existing.ExpirationTime = subscription.ExpirationTime;
            existing.UserAgent = subscription.UserAgent;
            existing.IsActive = true;
            existing.LastSeenAt = DateTime.UtcNow;
            existing.RevokedAt = null;

            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveSubscriptionAsync(string userId, PushSubscriptionDeleteDto subscription)
        {
            if (string.IsNullOrWhiteSpace(subscription.Endpoint))
            {
                return;
            }

            var endpointHash = ComputeEndpointHash(subscription.Endpoint);
            var existing = await _dbContext.UserPushSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.EndpointHash == endpointHash);

            if (existing == null)
            {
                return;
            }

            existing.IsActive = false;
            existing.RevokedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> SendPushAsync(string userId, string title, string message, string route)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("Push notification skipped because VAPID configuration is missing.");
                return false;
            }

            var subscriptions = await _dbContext.UserPushSubscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            if (!subscriptions.Any())
            {
                return false;
            }

            var payload = JsonSerializer.Serialize(new
            {
                title,
                body = message,
                route,
                icon = "/assets/icon/favicon.png",
                badge = "/assets/icon/favicon.png"
            });

            var client = new WebPushClient();
            var vapidDetails = new VapidDetails(GetSubject(), GetPublicKey(), GetPrivateKey());
            var sent = false;

            foreach (var subscription in subscriptions)
            {
                try
                {
                    await client.SendNotificationAsync(
                        new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth),
                        payload,
                        vapidDetails);

                    subscription.LastSentAt = DateTime.UtcNow;
                    sent = true;
                }
                catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
                {
                    subscription.IsActive = false;
                    subscription.RevokedAt = DateTime.UtcNow;
                    _logger.LogInformation("Removed expired push subscription for user {UserId}.", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send push notification to user {UserId}.", userId);
                }
            }

            await _dbContext.SaveChangesAsync();
            return sent;
        }

        private string? GetPrivateKey() => _configuration["PushNotifications:VapidPrivateKey"];

        private string GetSubject() =>
            _configuration["PushNotifications:VapidSubject"] ?? "mailto:noreply@betsoffriends.com";

        private static string ComputeEndpointHash(string endpoint)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(endpoint));
            return Convert.ToHexString(bytes);
        }
    }
}
