using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Services;

public class SupportServiceTests
{
    [Fact]
    public async Task HandleSupportMessageAsync_KeepsMessageSavedWhenSuperAdminNotificationFails()
    {
        using var host = new BackendTestHost(services =>
        {
            services.AddScoped<ISupportService, SupportService>();
            services.AddScoped<INotificationService, ThrowingSupportNotificationService>();
        });

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISupportService>();

            await service.HandleSupportMessageAsync(new SupportMessageDto
            {
                Email = "support-user@example.com",
                Subject = "Cannot use support",
                Message = "The support form should not fail after saving.",
                Language = "en"
            });
        }

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var message = await dbContext.SupportMessages.SingleAsync();

            Assert.Equal("support-user@example.com", message.Email);
            Assert.Equal("Cannot use support", message.Subject);
            Assert.Equal("The support form should not fail after saving.", message.Message);
            Assert.Equal(1, message.LanguageId);
        }
    }

    private sealed class ThrowingSupportNotificationService : INotificationService
    {
        public Task NotifyMatchClosureAsync(CustomMatch match) => Task.CompletedTask;

        public Task NotifyUserAcceptedTournamentInviteAsync(CustomTournamentUserAssignment assignment) => Task.CompletedTask;

        public Task NotifyAdminsJoinRequestAsync(CustomTournamentUserAssignment joinRequest) => Task.CompletedTask;

        public Task NotifyUserJoinRequestApprovedAsync(CustomTournamentUserAssignment assignment) => Task.CompletedTask;

        public Task NotifyMatchStartingSoonAsync(CustomMatch match, TimeSpan threshold) => Task.CompletedTask;

        public Task<List<string>> NotifyManualPendingBetReminderAsync(CustomMatch match, List<ApplicationUser> recipients)
            => Task.FromResult(new List<string>());

        public Task NotifyDailyTournamentUpdatesAsync(DateTime nowUtc) => Task.CompletedTask;

        public Task NotifyNewGamesToBetAsync(CustomMatch match) => Task.CompletedTask;

        public Task NotifyTournamentInvitationsAsync(int tournamentId, IEnumerable<string> userEmails) => Task.CompletedTask;

        public Task NotifySuperAdminsAboutSupportMessageAsync(SupportMessage message)
            => throw new InvalidOperationException("Support notification failed after save.");

        public Task ProcessNotificationsAsync(
            List<ApplicationUser> recipients,
            string title,
            string message,
            string route,
            Func<ApplicationUser, (bool emailConsent, bool pushConsent)> getConsent) => Task.CompletedTask;

        public Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int? limit = null)
            => Task.FromResult(new List<NotificationDto>());

        public Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId)
            => Task.FromResult(false);

        public Task<bool> DeleteNotificationAsync(int notificationId, string userId)
            => Task.FromResult(false);

        public Task<NotificationSettingsDto?> GetNotificationSettingsAsync(string userId)
            => Task.FromResult<NotificationSettingsDto?>(null);

        public Task<bool> UpdateNotificationSettingsAsync(string userId, NotificationSettingsDto settingsDto)
            => Task.FromResult(true);
    }
}
