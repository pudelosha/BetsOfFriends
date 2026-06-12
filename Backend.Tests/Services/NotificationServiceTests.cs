using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Services;

public class NotificationServiceTests
{
    [Fact]
    public async Task ProcessNotificationsAsync_CreatesNotificationRecipientsAndHonorsEmailAndPushConsent()
    {
        using var host = new BackendTestHost();
        var firstUser = await host.CreateUserAsync("notify-one@example.com");
        var secondUser = await host.CreateUserAsync("notify-two@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await service.ProcessNotificationsAsync(
            new List<ApplicationUser> { firstUser, secondUser },
            "Test title",
            "Test message",
            "/test-route",
            user => (emailConsent: true, pushConsent: user.Id == firstUser.Id));

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notification = await dbContext.Notifications.SingleAsync();
        var recipients = await dbContext.NotificationRecipients
            .OrderBy(recipient => recipient.UserId)
            .ToListAsync();

        Assert.Equal("Test title", notification.Title);
        Assert.Equal("Test message", notification.Message);
        Assert.Equal("/test-route", notification.Route);
        Assert.Equal(2, recipients.Count);
        Assert.All(recipients, recipient => Assert.True(recipient.SentEmail));
        Assert.Single(recipients, recipient => recipient.UserId == firstUser.Id && recipient.SentPush);
        Assert.Single(host.PushNotifications.SentPushNotifications);
        Assert.Equal(2, host.Emails.NotificationEmails.Count);
    }

    [Fact]
    public async Task GetMarkAndDeleteNotificationMethods_WorkForTheCurrentUserOnly()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("notification-list@example.com");
        var otherUser = await host.CreateUserAsync("other-notification-list@example.com");

        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var older = new Notification
        {
            Title = "Older",
            Message = "Older message",
            Route = "/older",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var newer = new Notification
        {
            Title = "Newer",
            Message = "Newer message",
            Route = "/newer",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notifications.AddRange(older, newer);
        await dbContext.SaveChangesAsync();

        dbContext.NotificationRecipients.AddRange(
            new NotificationRecipient { UserId = user.Id, NotificationId = older.Id },
            new NotificationRecipient { UserId = user.Id, NotificationId = newer.Id },
            new NotificationRecipient { UserId = otherUser.Id, NotificationId = newer.Id });
        await dbContext.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var notifications = await service.GetUserNotificationsAsync(user.Id, limit: 1);
        var marked = await service.MarkNotificationAsReadAsync(newer.Id, user.Id);
        var markOtherUsersNotification = await service.MarkNotificationAsReadAsync(older.Id, otherUser.Id);
        var deleted = await service.DeleteNotificationAsync(older.Id, user.Id);

        Assert.Single(notifications);
        Assert.Equal("Newer", notifications[0].Title);
        Assert.True(marked);
        Assert.False(markOtherUsersNotification);
        Assert.True(deleted);
        Assert.False(await dbContext.NotificationRecipients.AnyAsync(recipient =>
            recipient.NotificationId == older.Id && recipient.UserId == user.Id));
        Assert.True(await dbContext.NotificationRecipients.AnyAsync(recipient =>
            recipient.NotificationId == newer.Id && recipient.UserId == otherUser.Id));
    }

    [Fact]
    public async Task UpdateNotificationSettingsAsync_PersistsAllConsentFlags()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("settings@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var settings = new NotificationSettingsDto
        {
            ReceiveEmailMatchClosed = true,
            ReceivePushMatchClosed = true,
            ReceiveEmailDailyUpdates = true,
            ReceivePushDailyUpdates = false,
            ReceiveEmailTournamentInvitation = true,
            ReceivePushTournamentInvitation = true,
            ReceiveEmailPendingBets = true,
            ReceivePushPendingBets = false,
            ReceiveEmailNewGames = true,
            ReceivePushNewGames = true,
            ReceiveEmailSpecialOffers = false,
            ReceivePushSpecialOffers = true
        };

        var updated = await service.UpdateNotificationSettingsAsync(user.Id, settings);
        var saved = await service.GetNotificationSettingsAsync(user.Id);

        Assert.True(updated);
        Assert.NotNull(saved);
        Assert.True(saved!.ReceiveEmailMatchClosed);
        Assert.True(saved.ReceivePushMatchClosed);
        Assert.True(saved.ReceiveEmailDailyUpdates);
        Assert.False(saved.ReceivePushDailyUpdates);
        Assert.True(saved.ReceiveEmailTournamentInvitation);
        Assert.True(saved.ReceivePushTournamentInvitation);
        Assert.True(saved.ReceiveEmailPendingBets);
        Assert.False(saved.ReceivePushPendingBets);
        Assert.True(saved.ReceiveEmailNewGames);
        Assert.True(saved.ReceivePushNewGames);
        Assert.False(saved.ReceiveEmailSpecialOffers);
        Assert.True(saved.ReceivePushSpecialOffers);
    }

    [Fact]
    public async Task NotifyMatchClosureAsync_SendsResultRecalculationNotificationToAcceptedParticipantsWithConsent()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("closed-match@example.com");

        using var scope = host.CreateScope();
        var seed = await SeedTournamentMatchAsync(
            scope.ServiceProvider,
            user.Id,
            configuredUser =>
            {
                configuredUser.ReceiveEmailMatchClosed = true;
                configuredUser.ReceivePushMatchClosed = true;
            },
            status: CustomMatch.MatchStatus.Finished);
        seed.Match.HomeScore = 2;
        seed.Match.AwayScore = 1;
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await service.NotifyMatchClosureAsync(seed.Match);

        var email = Assert.Single(host.Emails.NotificationEmails);
        var push = Assert.Single(host.PushNotifications.SentPushNotifications);
        Assert.Equal("Tournament results updated", email.Title);
        Assert.Contains("Home FC vs Away FC", email.Message);
        Assert.Contains("2-1", email.Message);
        Assert.Equal("/results", email.Route);
        Assert.Equal(email.Title, push.Title);
    }

    [Fact]
    public async Task NotifyDailyTournamentUpdatesAsync_SendsOnlyOncePerUserPerDay()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("daily@example.com");
        var nowUtc = DateTime.UtcNow;

        using var scope = host.CreateScope();
        await SeedTournamentMatchAsync(
            scope.ServiceProvider,
            user.Id,
            configuredUser => configuredUser.ReceiveEmailDailyUpdates = true,
            matchStartUtc: nowUtc.AddHours(2));
        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await service.NotifyDailyTournamentUpdatesAsync(nowUtc);
        await service.NotifyDailyTournamentUpdatesAsync(nowUtc.AddMinutes(10));

        var email = Assert.Single(host.Emails.NotificationEmails);
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Daily tournament update", email.Title);
        Assert.Contains("1 tournament match", email.Message);
        Assert.Single(await dbContext.NotificationRecipients.ToListAsync());
    }

    [Fact]
    public async Task NotifyTournamentInvitationsAsync_NotifiesRegisteredEmailsWithInvitationConsent()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("invite-target@example.com", languageId: 2);

        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var trackedUser = await dbContext.Users.FindAsync(user.Id);
        trackedUser!.ReceiveEmailTournamentInvitation = true;
        trackedUser.ReceivePushTournamentInvitation = true;

        var tournament = new CustomTournament
        {
            Name = "Spring Cup",
            CreatedByUserId = trackedUser.Id,
            CreatedByUser = trackedUser,
            IsActive = true
        };
        dbContext.CustomTournaments.Add(tournament);
        await dbContext.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await service.NotifyTournamentInvitationsAsync(
            tournament.TournamentId,
            new[] { " INVITE-TARGET@example.com ", "invite-target@example.com", "missing@example.com" });

        var email = Assert.Single(host.Emails.NotificationEmails);
        var push = Assert.Single(host.PushNotifications.SentPushNotifications);
        Assert.Equal("Zaproszenie do turnieju", email.Title);
        Assert.Contains("Spring Cup", email.Message);
        Assert.Equal("/my-tournaments", email.Route);
        Assert.Equal(email.Title, push.Title);
    }

    [Fact]
    public async Task NotifyUserAcceptedTournamentInviteAsync_NotifiesTournamentAdmins()
    {
        using var host = new BackendTestHost();
        var admin = await host.CreateUserAsync("invite-admin@example.com");
        var invitedUser = await host.CreateUserAsync("invite-accepted-user@example.com");

        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var trackedAdmin = await dbContext.Users.FindAsync(admin.Id);
        var trackedInvitedUser = await dbContext.Users.FindAsync(invitedUser.Id);
        trackedAdmin!.ReceiveEmailTournamentInvitation = true;
        trackedAdmin.ReceivePushTournamentInvitation = true;

        var tournament = new CustomTournament
        {
            Name = "Admin Notify Cup",
            CreatedByUserId = trackedAdmin.Id,
            CreatedByUser = trackedAdmin,
            IsActive = true
        };
        dbContext.CustomTournaments.Add(tournament);
        await dbContext.SaveChangesAsync();

        dbContext.CustomTournamentUserAssignments.AddRange(
            new CustomTournamentUserAssignment
            {
                TournamentId = tournament.TournamentId,
                Tournament = tournament,
                UserId = trackedAdmin.Id,
                User = trackedAdmin,
                UserAdminName = "Admin",
                UserName = "Admin",
                Role = UserTournamentRole.Admin,
                Status = AssignmentStatus.Accepted
            },
            new CustomTournamentUserAssignment
            {
                TournamentId = tournament.TournamentId,
                Tournament = tournament,
                UserId = trackedInvitedUser!.Id,
                User = trackedInvitedUser,
                UserAdminName = "Invited User",
                UserName = "Invited",
                Role = UserTournamentRole.Player,
                Status = AssignmentStatus.Accepted
            });
        await dbContext.SaveChangesAsync();

        var acceptedAssignment = await dbContext.CustomTournamentUserAssignments
            .SingleAsync(assignment => assignment.TournamentId == tournament.TournamentId && assignment.UserId == trackedInvitedUser.Id);
        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await service.NotifyUserAcceptedTournamentInviteAsync(acceptedAssignment);

        var notificationRecipient = await dbContext.NotificationRecipients
            .Include(recipient => recipient.Notification)
            .SingleAsync();
        var email = Assert.Single(host.Emails.NotificationEmails);
        var push = Assert.Single(host.PushNotifications.SentPushNotifications);

        Assert.Equal(trackedAdmin.Id, notificationRecipient.UserId);
        Assert.Equal("User joined: Invited (invite-accepted-user@example.com)", notificationRecipient.Notification.Title);
        Assert.Contains("has accepted the tournament invite", notificationRecipient.Notification.Message);
        Assert.Equal("/tournaments/participants", notificationRecipient.Notification.Route);
        Assert.Equal(notificationRecipient.Notification.Title, email.Title);
        Assert.Equal(email.Title, push.Title);
    }

    [Fact]
    public async Task NotifyMatchStartingSoonAsync_RemindsOnlyAcceptedUsersWithToPlaceBets()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("pending-bet@example.com");

        using var scope = host.CreateScope();
        var seed = await SeedTournamentMatchAsync(
            scope.ServiceProvider,
            user.Id,
            configuredUser =>
            {
                configuredUser.ReceiveEmailPendingBets = true;
                configuredUser.ReceivePushPendingBets = true;
            },
            matchStartUtc: DateTime.UtcNow.AddMinutes(55));
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Bets.Add(new Bet
        {
            MatchId = seed.Match.MatchId,
            Match = seed.Match,
            UserId = seed.User.Id,
            User = seed.User,
            Status = Bet.BetStatus.ToPlace,
            Result = Bet.BetResult.Pending
        });
        await dbContext.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await service.NotifyMatchStartingSoonAsync(seed.Match, TimeSpan.FromHours(1));

        var email = Assert.Single(host.Emails.NotificationEmails);
        var push = Assert.Single(host.PushNotifications.SentPushNotifications);
        Assert.Equal("Reminder: match starts in 1 hour", email.Title);
        Assert.Contains("Home FC vs Away FC", email.Message);
        Assert.Contains("tab=to-place", email.Route);
        Assert.Equal(email.Title, push.Title);
    }

    [Fact]
    public async Task NotifyNewGamesToBetAsync_SendsNewGameNotificationWithBetRoute()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("new-game@example.com");

        using var scope = host.CreateScope();
        var seed = await SeedTournamentMatchAsync(
            scope.ServiceProvider,
            user.Id,
            configuredUser =>
            {
                configuredUser.ReceiveEmailNewGames = true;
                configuredUser.ReceivePushNewGames = true;
            });
        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await service.NotifyNewGamesToBetAsync(seed.Match);

        var email = Assert.Single(host.Emails.NotificationEmails);
        var push = Assert.Single(host.PushNotifications.SentPushNotifications);
        Assert.Equal("New match available", email.Title);
        Assert.Equal("Home FC vs Away FC is now open for betting.", email.Message);
        Assert.Contains("/my-bets?tab=to-place", email.Route);
        Assert.Contains($"tournamentId={seed.Tournament.TournamentId}", email.Route);
        Assert.Equal(email.Title, push.Title);
    }

    private static async Task<TournamentMatchSeed> SeedTournamentMatchAsync(
        IServiceProvider services,
        string userId,
        Action<ApplicationUser>? configureUser = null,
        DateTime? matchStartUtc = null,
        CustomMatch.MatchStatus status = CustomMatch.MatchStatus.Timed)
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        var user = await dbContext.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"Test user {userId} was not found.");

        configureUser?.Invoke(user);

        var tournament = new CustomTournament
        {
            Name = "Test Cup",
            CreatedByUserId = user.Id,
            CreatedByUser = user,
            IsActive = true
        };
        dbContext.CustomTournaments.Add(tournament);
        await dbContext.SaveChangesAsync();

        var assignment = new CustomTournamentUserAssignment
        {
            UserId = user.Id,
            User = user,
            TournamentId = tournament.TournamentId,
            Tournament = tournament,
            Role = UserTournamentRole.Player,
            UserAdminName = "Test User",
            UserName = "Test User",
            Status = AssignmentStatus.Accepted
        };
        var stage = new CustomMatchStage
        {
            TournamentId = tournament.TournamentId,
            Tournament = tournament,
            StageName = "Final",
            Order = 1
        };
        var homeTeam = new CustomTeam
        {
            TournamentId = tournament.TournamentId,
            Tournament = tournament,
            TeamName = "Home FC",
            EloRating = 1600
        };
        var awayTeam = new CustomTeam
        {
            TournamentId = tournament.TournamentId,
            Tournament = tournament,
            TeamName = "Away FC",
            EloRating = 1500
        };

        dbContext.CustomTournamentUserAssignments.Add(assignment);
        dbContext.CustomMatchStages.Add(stage);
        dbContext.CustomTeams.AddRange(homeTeam, awayTeam);
        await dbContext.SaveChangesAsync();

        var match = new CustomMatch
        {
            TournamentId = tournament.TournamentId,
            Tournament = tournament,
            StageId = stage.StageId,
            Stage = stage,
            HomeTeamId = homeTeam.TeamId,
            HomeTeam = homeTeam,
            AwayTeamId = awayTeam.TeamId,
            AwayTeam = awayTeam,
            MatchStart = matchStartUtc ?? DateTime.UtcNow.AddDays(1),
            Status = status,
            HomeWinOdds = 2.1m,
            DrawOdds = 3.3m,
            AwayWinOdds = 3.4m
        };

        dbContext.CustomMatches.Add(match);
        await dbContext.SaveChangesAsync();

        return new TournamentMatchSeed(tournament, match, user);
    }

    private sealed record TournamentMatchSeed(CustomTournament Tournament, CustomMatch Match, ApplicationUser User);
}
