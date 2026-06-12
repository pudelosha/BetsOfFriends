using System.Security.Claims;
using Backend.Controllers;
using Backend.DTOs;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Controllers;

public class CustomTournamentsControllerTests
{
    [Fact]
    public async Task UpdateCustomTournament_WhenAccountSetupEmailFails_ReturnsOk()
    {
        var tournamentService = new FakeCustomTournamentService
        {
            UpdateResult = TournamentUpdateResultDto.SuccessResult(
                42,
                "Summer Cup",
                new HashSet<string> { "new-player@example.com" },
                new HashSet<string>())
        };
        var userService = new FakeUserService();
        userService.UsersByEmail["new-player@example.com"] = new ApplicationUser
        {
            Id = "new-player-id",
            Email = "new-player@example.com",
            UserName = "new-player@example.com"
        };
        var emailService = new FakeEmailService { ThrowAccountSetupEmail = true };
        var betService = new FakeBetService();
        var notificationService = new FakeNotificationService();

        var controller = CreateController(
            tournamentService,
            userService,
            emailService,
            betService,
            notificationService);

        var result = await controller.UpdateCustomTournament(CreateTournamentDto());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Tournament updated successfully.", ok.Value);
        Assert.Equal(new[] { 42 }, betService.CreatedBetTournamentIds);
        Assert.Equal(1, emailService.AccountSetupAttempts);
    }

    [Fact]
    public async Task UpdateCustomTournament_WhenExistingUserNotificationFails_ReturnsOk()
    {
        var tournamentService = new FakeCustomTournamentService
        {
            UpdateResult = TournamentUpdateResultDto.SuccessResult(
                42,
                "Summer Cup",
                new HashSet<string>(),
                new HashSet<string> { "existing-player@example.com" })
        };
        var notificationService = new FakeNotificationService { ThrowTournamentInvitation = true };
        var betService = new FakeBetService();

        var controller = CreateController(
            tournamentService,
            new FakeUserService(),
            new FakeEmailService(),
            betService,
            notificationService);

        var result = await controller.UpdateCustomTournament(CreateTournamentDto());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Tournament updated successfully.", ok.Value);
        Assert.Equal(new[] { 42 }, betService.CreatedBetTournamentIds);
        Assert.Equal(1, notificationService.TournamentInvitationAttempts);
    }

    private static CustomTournamentsController CreateController(
        ICustomTournamentService tournamentService,
        IUserService userService,
        IEmailService emailService,
        IBetService betService,
        INotificationService notificationService)
    {
        var controller = new CustomTournamentsController(
            tournamentService,
            NullLogger<CustomTournamentsController>.Instance,
            userService,
            emailService,
            betService,
            notificationService,
            new FakeTournamentSelectionService());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "admin-user-id") },
                    "TestAuth"))
            }
        };

        return controller;
    }

    private static CustomTournamentDto CreateTournamentDto()
    {
        return new CustomTournamentDto
        {
            TournamentId = 42,
            TournamentName = "Summer Cup",
            IsActive = true,
            IncludeHomeAdvantage = false,
            TournamentVisibility = "Private",
            UpdateMethod = "Manual",
            CreatedBy = "admin-user-id",
            Teams = new List<CustomTeamDto>(),
            Matches = new List<CustomMatchDto>(),
            Users = new List<CustomUserDto>(),
            Stages = new List<CustomStageDto>()
        };
    }

    private sealed class FakeCustomTournamentService : ICustomTournamentService
    {
        public TournamentUpdateResultDto UpdateResult { get; set; } =
            TournamentUpdateResultDto.SuccessResult(42, "Summer Cup", new HashSet<string>(), new HashSet<string>());

        public Task<TournamentUpdateResultDto> UpdateCustomTournamentAsync(CustomTournamentDto tournamentDto, string userId)
        {
            return Task.FromResult(UpdateResult);
        }

        public Task<TournamentCreationResultDto> CreateCustomTournamentAsync(CustomTournamentDto tournamentDto) => throw new NotImplementedException();
        public Task<List<CustomTournamentListDto>> GetAllCustomTournamentsAsync(string userId) => throw new NotImplementedException();
        public Task<bool?> UpdateCustomTournamentStatusAsync(int tournamentId, string userId, bool isActive) => throw new NotImplementedException();
        public Task<bool?> DeleteCustomTournamentByIdAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<CustomTournamentDto?> GetCustomTournamentByIdAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<List<UserActiveTournamentDto>> GetUserActiveTournamentsAsync(string userId) => throw new NotImplementedException();
        public Task<bool> QuitTournamentAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<bool?> ToggleTournamentVisibilityAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<bool> RecalculateTournamentBetsAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<List<TournamentSummaryDto>?> GetTournamentSummaryAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<List<TournamentPlayerResultDto>> GetTournamentPlayerResultAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<List<TournamentInviteDto>> GetPendingTournamentInvitesAsync(string userId) => throw new NotImplementedException();
        public Task<List<string>> GetTournamentStagesAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<string?> GetFirstStageWithPendingBetsAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<string?> GetFirstStageWithUpcomingMatchesAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<List<UserBettingStatsDto>> GetUserBettingStatsAsync(string userId, int tournamentId, string statsUserId) => throw new NotImplementedException();
        public Task<List<MatchInsightDto>> GetMatchInsightsAsync(string userId, int tournamentId) => throw new NotImplementedException();
        public Task<CustomTournamentExtraPredictionsDto?> GetCustomTournamentExtraPredictionsAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<ActionResultDto> UpsertCustomTournamentExtraPredictionAsync(int tournamentId, string userId, CustomTournamentExtraPredictionUpdateDto request) => throw new NotImplementedException();
        public Task<bool> IsTournamentNameTakenAsync(string name, string visibility, string userId) => throw new NotImplementedException();
        public Task<List<PublicTournamentDto>> GetPublicActiveTournamentsAsync(string userId) => throw new NotImplementedException();
        public Task<List<TournamentParticipantDto>?> GetTournamentParticipantsAsync(int tournamentId, string userId, string status) => throw new NotImplementedException();
        public Task<ActionResultDto> ExcludeParticipantAsync(int tournamentId, string requesterUserId, string targetUserEmail) => throw new NotImplementedException();
        public Task<ActionResultDto> AcceptParticipantAsync(int tournamentId, string requesterUserId, string targetUserEmail) => throw new NotImplementedException();
        public Task<ActionResultDto> ResendInviteAsync(int tournamentId, string requesterUserId, string targetUserEmail) => throw new NotImplementedException();
        public Task<TournamentInvitationResponseDto> UpdateTournamentAssignmentAsync(int tournamentId, string userId, string newNickname) => throw new NotImplementedException();
        public Task<TournamentAssignmentDto?> GetAssignmentDetailsAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<TournamentInvitationResponseDto> AcceptTournamentInvitationAsync(int tournamentId, string userId, string nickname) => throw new NotImplementedException();
        public Task<TournamentInvitationResponseDto> RequestToJoinTournamentAsync(string userId, int tournamentId, string nickname, string message) => throw new NotImplementedException();
        public Task<CustomTournamentDto?> CheckForPendingUpdatesAsync(int tournamentId, string userId) => throw new NotImplementedException();
        public Task<SelectedTournamentDetailsDto?> GetSelectedTournamentDetailsAsync(int tournamentId, string userId) => throw new NotImplementedException();
    }

    private sealed class FakeUserService : IUserService
    {
        public Dictionary<string, ApplicationUser> UsersByEmail { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string GetUserIdFromClaims(ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin-user-id";
        }

        public Task<ApplicationUser?> FindUserByEmailAsync(string email)
        {
            UsersByEmail.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public Task<ApplicationUser?> FindUserByIdAsync(string userId) => throw new NotImplementedException();
        public Task<UserProfileDto> GetUserProfileAsync(string userId) => throw new NotImplementedException();
        public Task<bool> UpdateUserProfileAsync(string userId, UserProfileDto profile) => throw new NotImplementedException();
        public Task<bool> SendPasswordResetEmailAsync(string email) => throw new NotImplementedException();
        public Task<ResetPasswordResultDto> ResetPasswordAsync(ResetPasswordRequestDto request) => throw new NotImplementedException();
        public Task<bool> ChangeUserEmailAsync(string userId, string newEmail, string password) => throw new NotImplementedException();
        public Task<bool> UpdateUserPasswordAsync(string userId, string currentPassword, string newPassword) => throw new NotImplementedException();
        public Task<bool> DeleteUserAccountAsync(string userId, string password) => throw new NotImplementedException();
        public Task<List<ApplicationUserDto>> GetAllUsersAsync() => throw new NotImplementedException();
        public Task<ActionResultDto> SuspendUserAsync(string targetUserId, string performedByUserId) => throw new NotImplementedException();
        public Task<ActionResultDto> UnsuspendUserAsync(string targetUserId, string adminUserId) => throw new NotImplementedException();
        public Task<ActionResultDto> DeleteUserAsync(string targetUserId, string performedByUserId) => throw new NotImplementedException();
    }

    private sealed class FakeEmailService : IEmailService
    {
        public bool ThrowAccountSetupEmail { get; set; }
        public int AccountSetupAttempts { get; private set; }

        public Task SendAccountSetupEmailAsync(ApplicationUser user, string tournamentName)
        {
            AccountSetupAttempts++;
            if (ThrowAccountSetupEmail)
            {
                throw new InvalidOperationException("SMTP delivery failed.");
            }

            return Task.CompletedTask;
        }

        public Task SendTournamentInvitationEmailAsync(string email, string tournamentName, int tournamentId) => throw new NotImplementedException();
        public Task SendConfirmationEmailAsync(ApplicationUser user) => throw new NotImplementedException();
        public Task SendPasswordResetEmailAsync(ApplicationUser user) => throw new NotImplementedException();
        public Task SendNotificationEmailAsync(
            ApplicationUser user,
            string title,
            string message,
            string route,
            string language,
            bool includeNotificationConsentText = true) => throw new NotImplementedException();
    }

    private sealed class FakeBetService : IBetService
    {
        private readonly List<int> _createdBetTournamentIds = new();
        public int[] CreatedBetTournamentIds => _createdBetTournamentIds.ToArray();

        public Task CreateBetsForTournamentAsync(int tournamentId)
        {
            _createdBetTournamentIds.Add(tournamentId);
            return Task.CompletedTask;
        }

        public Task<bool> UpdateBetAsync(int betId, string userId, BetUpdateDto betUpdateDto) => throw new NotImplementedException();
        public Task<List<BetDto>> GetBetsByStatusAndStageAsync(int tournamentId, string userId, string status, string stage) => throw new NotImplementedException();
        public Task GenerateBetsForNewMatchAsync(int matchId, int tournamentId) => throw new NotImplementedException();
        public Task<BetStatsDto?> GetBetStatisticsAsync(int matchId, string userId) => throw new NotImplementedException();
        public Task<PendingBetReminderSummaryDto?> GetPendingBetReminderParticipantsAsync(int matchId, string userId) => throw new NotImplementedException();
        public Task<SendPendingBetReminderResultDto> SendPendingBetReminderAsync(int matchId, string userId, SendPendingBetReminderRequestDto request) => throw new NotImplementedException();
        public Task RecalculateBetsForMatchAsync(int matchId) => throw new NotImplementedException();
        public Task<bool> RecalculateBetsForTournamentAsync(int tournamentId) => throw new NotImplementedException();
        public Task<List<UpcomingBetDto>> GetUpcomingBetsAsync(int tournamentId, string userId, int? limit = null) => throw new NotImplementedException();
        public Task<List<BetDto>> GetInProgressBetsAsync(int tournamentId, string userId, int? limit = null) => throw new NotImplementedException();
        public Task<MissingBetsSummaryDto?> GetMissingBetsSummaryAsync(int tournamentId, string userId, int matchLimit = 5, int hoursAhead = 48) => throw new NotImplementedException();
        public Task MarkBetsAsCompletedForMatchAsync(int matchId) => throw new NotImplementedException();
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public bool ThrowTournamentInvitation { get; set; }
        public int TournamentInvitationAttempts { get; private set; }

        public Task NotifyTournamentInvitationsAsync(int tournamentId, IEnumerable<string> userEmails)
        {
            TournamentInvitationAttempts++;
            if (ThrowTournamentInvitation)
            {
                throw new InvalidOperationException("Notification delivery failed.");
            }

            return Task.CompletedTask;
        }

        public Task NotifyMatchClosureAsync(CustomMatch match) => throw new NotImplementedException();
        public Task NotifyUserAcceptedTournamentInviteAsync(CustomTournamentUserAssignment assignment) => throw new NotImplementedException();
        public Task NotifyAdminsJoinRequestAsync(CustomTournamentUserAssignment joinRequest) => throw new NotImplementedException();
        public Task NotifyUserJoinRequestApprovedAsync(CustomTournamentUserAssignment assignment) => throw new NotImplementedException();
        public Task NotifyMatchStartingSoonAsync(CustomMatch match, TimeSpan threshold) => throw new NotImplementedException();
        public Task<List<string>> NotifyManualPendingBetReminderAsync(CustomMatch match, List<ApplicationUser> recipients) => throw new NotImplementedException();
        public Task NotifyDailyTournamentUpdatesAsync(DateTime nowUtc) => throw new NotImplementedException();
        public Task NotifyNewGamesToBetAsync(CustomMatch match) => throw new NotImplementedException();
        public Task NotifySuperAdminsAboutSupportMessageAsync(SupportMessage message) => throw new NotImplementedException();
        public Task ProcessNotificationsAsync(List<ApplicationUser> recipients, string title, string message, string route, Func<ApplicationUser, (bool emailConsent, bool pushConsent)> getConsent) => throw new NotImplementedException();
        public Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int? limit = null) => throw new NotImplementedException();
        public Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId) => throw new NotImplementedException();
        public Task<bool> DeleteNotificationAsync(int notificationId, string userId) => throw new NotImplementedException();
        public Task<NotificationSettingsDto?> GetNotificationSettingsAsync(string userId) => throw new NotImplementedException();
        public Task<bool> UpdateNotificationSettingsAsync(string userId, NotificationSettingsDto settingsDto) => throw new NotImplementedException();
    }

    private sealed class FakeTournamentSelectionService : ITournamentSelectionService
    {
        public Task<bool> SetSelectedTournamentAsync(string userId, int tournamentId) => throw new NotImplementedException();
        public Task<int?> GetSelectedTournamentAsync(string userId) => throw new NotImplementedException();
    }
}
