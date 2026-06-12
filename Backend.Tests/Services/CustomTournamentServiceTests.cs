using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Services;

public class CustomTournamentServiceTests
{
    [Fact]
    public async Task AcceptTournamentInvitationAsync_ReturnsSuccessWhenAdminNotificationFailsAfterAccept()
    {
        using var host = new BackendTestHost(services =>
        {
            services.AddScoped<INotificationService, ThrowingAcceptedInviteNotificationService>();
        });

        var creator = await host.CreateUserAsync("creator-accept@example.com");
        var invitedUser = await host.CreateUserAsync("invited-accept@example.com");

        int tournamentId;

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tournament = new CustomTournament
            {
                Name = "Invite Accept Cup",
                CreatedByUserId = creator.Id,
                IsActive = true
            };

            dbContext.CustomTournaments.Add(tournament);
            await dbContext.SaveChangesAsync();
            tournamentId = tournament.TournamentId;

            dbContext.CustomTournamentUserAssignments.AddRange(
                new CustomTournamentUserAssignment
                {
                    TournamentId = tournamentId,
                    UserId = creator.Id,
                    UserAdminName = "Creator",
                    UserName = "Creator",
                    Status = AssignmentStatus.Accepted,
                    Role = UserTournamentRole.Admin
                },
                new CustomTournamentUserAssignment
                {
                    TournamentId = tournamentId,
                    UserId = invitedUser.Id,
                    UserAdminName = "Invited User",
                    Status = AssignmentStatus.Invited,
                    Role = UserTournamentRole.Player
                });

            await dbContext.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ICustomTournamentService>();

            var response = await service.AcceptTournamentInvitationAsync(tournamentId, invitedUser.Id, "Invited");

            Assert.True(response.Success);
            Assert.Equal("You have successfully joined the tournament as Invited.", response.Message);
        }

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var assignment = await dbContext.CustomTournamentUserAssignments
                .SingleAsync(a => a.TournamentId == tournamentId && a.UserId == invitedUser.Id);

            Assert.Equal(AssignmentStatus.Accepted, assignment.Status);
            Assert.Equal("Invited", assignment.UserName);
            Assert.True(assignment.IsSelected);
        }
    }

    [Fact]
    public async Task AcceptTournamentInvitationAsync_NotifiesTournamentAdminWhenInviteIsAccepted()
    {
        using var host = new BackendTestHost();

        var creator = await host.CreateUserAsync("creator-accept-notify@example.com");
        var invitedUser = await host.CreateUserAsync("invited-accept-notify@example.com");

        int tournamentId;

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tournament = new CustomTournament
            {
                Name = "Invite Notify Cup",
                CreatedByUserId = creator.Id,
                IsActive = true
            };

            dbContext.CustomTournaments.Add(tournament);
            await dbContext.SaveChangesAsync();
            tournamentId = tournament.TournamentId;

            dbContext.CustomTournamentUserAssignments.AddRange(
                new CustomTournamentUserAssignment
                {
                    TournamentId = tournamentId,
                    UserId = creator.Id,
                    UserAdminName = "Creator",
                    UserName = "Creator",
                    Status = AssignmentStatus.Accepted,
                    Role = UserTournamentRole.Admin
                },
                new CustomTournamentUserAssignment
                {
                    TournamentId = tournamentId,
                    UserId = invitedUser.Id,
                    UserAdminName = "Invited User",
                    Status = AssignmentStatus.Invited,
                    Role = UserTournamentRole.Player
                });

            await dbContext.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ICustomTournamentService>();

            var response = await service.AcceptTournamentInvitationAsync(tournamentId, invitedUser.Id, "Invited");

            Assert.True(response.Success);
        }

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var recipient = await dbContext.NotificationRecipients
                .Include(notificationRecipient => notificationRecipient.Notification)
                .SingleAsync();

            Assert.Equal(creator.Id, recipient.UserId);
            Assert.Equal("User joined: Invited (invited-accept-notify@example.com)", recipient.Notification.Title);
            Assert.Equal("/tournaments/participants", recipient.Notification.Route);
        }
    }

    [Fact]
    public async Task RequestToJoinTournamentAsync_ReturnsSuccessWhenAdminNotificationFailsAfterRequestIsSaved()
    {
        using var host = new BackendTestHost(services =>
        {
            services.AddScoped<INotificationService, ThrowingJoinRequestNotificationService>();
        });

        var creator = await host.CreateUserAsync("creator-join@example.com");
        var requester = await host.CreateUserAsync("requester-join@example.com");

        int tournamentId;

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tournament = new CustomTournament
            {
                Name = "Public Join Cup",
                PublicName = "Public Join Cup",
                CreatedByUserId = creator.Id,
                IsActive = true,
                Visibility = CustomTournament.TournamentVisibility.Public
            };

            dbContext.CustomTournaments.Add(tournament);
            await dbContext.SaveChangesAsync();
            tournamentId = tournament.TournamentId;

            dbContext.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
            {
                TournamentId = tournamentId,
                UserId = creator.Id,
                UserAdminName = "Creator",
                UserName = "Creator",
                Status = AssignmentStatus.Accepted,
                Role = UserTournamentRole.Admin
            });

            await dbContext.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ICustomTournamentService>();

            var response = await service.RequestToJoinTournamentAsync(requester.Id, tournamentId, "Requester", string.Empty);

            Assert.True(response.Success);
            Assert.Equal("You have successfully requested to join the tournament as 'Requester'.", response.Message);
        }

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var assignment = await dbContext.CustomTournamentUserAssignments
                .SingleAsync(a => a.TournamentId == tournamentId && a.UserId == requester.Id);

            Assert.Equal(AssignmentStatus.Requested, assignment.Status);
            Assert.Equal("Requester", assignment.UserName);
            Assert.False(assignment.IsSelected);
        }
    }

    [Fact]
    public async Task GetTournamentSummaryAsync_ReturnsResultsOnlyForAcceptedParticipants()
    {
        using var host = new BackendTestHost();
        var acceptedUser = await host.CreateUserAsync("accepted-results@example.com");
        var invitedUser = await host.CreateUserAsync("invited-results@example.com");
        var deletedUser = await host.CreateUserAsync("deleted-results@example.com");

        int tournamentId;

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tournament = new CustomTournament
            {
                Name = "Results Cup",
                CreatedByUserId = acceptedUser.Id,
                IsActive = true
            };

            dbContext.CustomTournaments.Add(tournament);
            await dbContext.SaveChangesAsync();
            tournamentId = tournament.TournamentId;

            var stage = new CustomMatchStage
            {
                TournamentId = tournamentId,
                StageName = "Final",
                Order = 1
            };
            var homeTeam = new CustomTeam
            {
                TournamentId = tournamentId,
                TeamName = "Home",
                EloRating = 1000
            };
            var awayTeam = new CustomTeam
            {
                TournamentId = tournamentId,
                TeamName = "Away",
                EloRating = 1000
            };

            dbContext.CustomMatchStages.Add(stage);
            dbContext.CustomTeams.AddRange(homeTeam, awayTeam);
            await dbContext.SaveChangesAsync();

            var match = new CustomMatch
            {
                TournamentId = tournamentId,
                StageId = stage.StageId,
                HomeTeamId = homeTeam.TeamId,
                AwayTeamId = awayTeam.TeamId,
                MatchStart = DateTime.UtcNow.AddDays(-1),
                Status = CustomMatch.MatchStatus.Finished,
                Type = CustomMatch.MatchType.Regular90Min,
                HomeScore = 2,
                AwayScore = 1,
                HomeWinOdds = 2,
                DrawOdds = 3,
                AwayWinOdds = 4
            };

            dbContext.CustomMatches.Add(match);
            await dbContext.SaveChangesAsync();

            dbContext.CustomTournamentUserAssignments.AddRange(
                new CustomTournamentUserAssignment
                {
                    TournamentId = tournamentId,
                    UserId = acceptedUser.Id,
                    UserAdminName = "Accepted Admin",
                    UserName = "Accepted Player",
                    Status = AssignmentStatus.Accepted,
                    Role = UserTournamentRole.Admin
                },
                new CustomTournamentUserAssignment
                {
                    TournamentId = tournamentId,
                    UserId = invitedUser.Id,
                    UserAdminName = "Invited Admin",
                    UserName = "Invited Player",
                    Status = AssignmentStatus.Invited,
                    Role = UserTournamentRole.Player
                });

            dbContext.Bets.AddRange(
                CreateClosedBet(match.MatchId, acceptedUser.Id, 10),
                CreateClosedBet(match.MatchId, invitedUser.Id, 20),
                CreateClosedBet(match.MatchId, deletedUser.Id, 30));

            await dbContext.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ICustomTournamentService>();

            var summary = await service.GetTournamentSummaryAsync(tournamentId, acceptedUser.Id);

            var result = Assert.Single(summary!);
            Assert.Equal(acceptedUser.Id, result.UserId);
            Assert.Equal("Accepted Player", result.UserName);
            Assert.Equal(10, result.TotalPayout);
        }
    }

    [Fact]
    public async Task GetFirstStageWithPendingBetsAsync_IgnoresStartedPendingBets()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("pending-stage@example.com");

        int tournamentId;

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tournament = new CustomTournament
            {
                Name = "Pending Stage Cup",
                CreatedByUserId = user.Id,
                IsActive = true
            };

            dbContext.CustomTournaments.Add(tournament);
            await dbContext.SaveChangesAsync();
            tournamentId = tournament.TournamentId;

            var pastStage = new CustomMatchStage
            {
                TournamentId = tournamentId,
                StageName = "Past Stage",
                Order = 1
            };
            var futureStage = new CustomMatchStage
            {
                TournamentId = tournamentId,
                StageName = "Future Stage",
                Order = 2
            };
            var homeTeam = new CustomTeam
            {
                TournamentId = tournamentId,
                TeamName = "Home",
                EloRating = 1000
            };
            var awayTeam = new CustomTeam
            {
                TournamentId = tournamentId,
                TeamName = "Away",
                EloRating = 1000
            };

            dbContext.CustomMatchStages.AddRange(pastStage, futureStage);
            dbContext.CustomTeams.AddRange(homeTeam, awayTeam);
            await dbContext.SaveChangesAsync();

            var pastMatch = new CustomMatch
            {
                TournamentId = tournamentId,
                StageId = pastStage.StageId,
                HomeTeamId = homeTeam.TeamId,
                AwayTeamId = awayTeam.TeamId,
                MatchStart = DateTime.UtcNow.AddMinutes(-1),
                Status = CustomMatch.MatchStatus.Timed,
                Type = CustomMatch.MatchType.Regular90Min,
                HomeWinOdds = 2,
                DrawOdds = 3,
                AwayWinOdds = 4
            };
            var futureMatch = new CustomMatch
            {
                TournamentId = tournamentId,
                StageId = futureStage.StageId,
                HomeTeamId = homeTeam.TeamId,
                AwayTeamId = awayTeam.TeamId,
                MatchStart = DateTime.UtcNow.AddHours(2),
                Status = CustomMatch.MatchStatus.Timed,
                Type = CustomMatch.MatchType.Regular90Min,
                HomeWinOdds = 2,
                DrawOdds = 3,
                AwayWinOdds = 4
            };

            dbContext.CustomMatches.AddRange(pastMatch, futureMatch);
            await dbContext.SaveChangesAsync();

            dbContext.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
            {
                TournamentId = tournamentId,
                UserId = user.Id,
                UserAdminName = "Pending Player",
                UserName = "Pending Player",
                Status = AssignmentStatus.Accepted,
                Role = UserTournamentRole.Player
            });
            dbContext.Bets.AddRange(
                CreatePendingBet(pastMatch.MatchId, user.Id),
                CreatePendingBet(futureMatch.MatchId, user.Id));

            await dbContext.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ICustomTournamentService>();

            var stage = await service.GetFirstStageWithPendingBetsAsync(tournamentId, user.Id);

            Assert.Equal("Future Stage", stage);
        }
    }

    [Fact]
    public async Task UpsertCustomTournamentExtraPredictionAsync_WithFutureFirstMatch_SavesPrediction()
    {
        using var host = new BackendTestHost();
        var creator = await host.CreateUserAsync("extra-creator@example.com");
        var player = await host.CreateUserAsync("extra-player@example.com");
        var seed = await SeedExtraPredictionTournamentAsync(host, creator, player, DateTime.UtcNow.AddHours(2));

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ICustomTournamentService>();

            var result = await service.UpsertCustomTournamentExtraPredictionAsync(
                seed.TournamentId,
                player.Id,
                new CustomTournamentExtraPredictionUpdateDto
                {
                    WinnerTeamId = seed.WinnerTeamId,
                    SecondPlaceTeamId = seed.SecondPlaceTeamId,
                    ThirdPlaceTeamId = seed.ThirdPlaceTeamId,
                    TopScorerTeamId = seed.TopScorerTeamId,
                    TopScorerName = "Test Scorer"
                });

            Assert.True(result.Success);
        }

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var prediction = await dbContext.CustomTournamentExtraPredictions
                .SingleAsync(p => p.TournamentId == seed.TournamentId && p.UserId == player.Id);

            Assert.Equal(seed.WinnerTeamId, prediction.WinnerTeamId);
            Assert.Equal(seed.SecondPlaceTeamId, prediction.SecondPlaceTeamId);
            Assert.Equal(seed.ThirdPlaceTeamId, prediction.ThirdPlaceTeamId);
            Assert.Equal(seed.TopScorerTeamId, prediction.TopScorerTeamId);
            Assert.Equal("Test Scorer", prediction.TopScorerName);
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ICustomTournamentService>();
            var overview = await service.GetCustomTournamentExtraPredictionsAsync(seed.TournamentId, player.Id);

            Assert.NotNull(overview);
            Assert.False(overview.IsLocked);
            Assert.Equal(4, overview.Teams.Count);

            var playerRow = overview.Predictions.Single(p => p.IsCurrentUser);
            Assert.True(playerRow.HasPrediction);
            Assert.Equal(seed.WinnerTeamId, playerRow.WinnerTeamId);
            Assert.Equal("Test Scorer", playerRow.TopScorerName);
        }
    }

    [Fact]
    public async Task UpsertCustomTournamentExtraPredictionAsync_WhenFirstMatchStarted_RejectsPrediction()
    {
        using var host = new BackendTestHost();
        var creator = await host.CreateUserAsync("extra-locked-creator@example.com");
        var player = await host.CreateUserAsync("extra-locked-player@example.com");
        var seed = await SeedExtraPredictionTournamentAsync(host, creator, player, DateTime.UtcNow.AddMinutes(-1));

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ICustomTournamentService>();

            var result = await service.UpsertCustomTournamentExtraPredictionAsync(
                seed.TournamentId,
                player.Id,
                new CustomTournamentExtraPredictionUpdateDto
                {
                    WinnerTeamId = seed.WinnerTeamId,
                    SecondPlaceTeamId = seed.SecondPlaceTeamId,
                    ThirdPlaceTeamId = seed.ThirdPlaceTeamId,
                    TopScorerTeamId = seed.TopScorerTeamId,
                    TopScorerName = "Too Late"
                });

            Assert.False(result.Success);
            Assert.Equal("The first tournament match has already started.", result.Message);
        }

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await dbContext.CustomTournamentExtraPredictions.AnyAsync(p => p.TournamentId == seed.TournamentId));
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ICustomTournamentService>();
            var overview = await service.GetCustomTournamentExtraPredictionsAsync(seed.TournamentId, player.Id);

            Assert.NotNull(overview);
            Assert.True(overview.IsLocked);
        }
    }

    private static Bet CreateClosedBet(int matchId, string userId, decimal basePayout)
    {
        return new Bet
        {
            MatchId = matchId,
            UserId = userId,
            BaseAmount = 1,
            Status = Bet.BetStatus.Closed,
            Result = Bet.BetResult.Won,
            BasePayout = basePayout,
            Calculated = true
        };
    }

    private static Bet CreatePendingBet(int matchId, string userId)
    {
        return new Bet
        {
            MatchId = matchId,
            UserId = userId,
            BaseAmount = 1,
            Status = Bet.BetStatus.ToPlace,
            Result = Bet.BetResult.Pending,
            Calculated = false
        };
    }

    private static async Task<ExtraPredictionSeed> SeedExtraPredictionTournamentAsync(
        BackendTestHost host,
        ApplicationUser creator,
        ApplicationUser player,
        DateTime matchStart)
    {
        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tournament = new CustomTournament
        {
            Name = $"Extra Prediction Cup {Guid.NewGuid():N}",
            CreatedByUserId = creator.Id,
            IsActive = true
        };

        dbContext.CustomTournaments.Add(tournament);
        await dbContext.SaveChangesAsync();

        dbContext.CustomTournamentUserAssignments.AddRange(
            new CustomTournamentUserAssignment
            {
                TournamentId = tournament.TournamentId,
                UserId = creator.Id,
                UserAdminName = "Creator",
                UserName = "Creator",
                Status = AssignmentStatus.Accepted,
                Role = UserTournamentRole.Admin
            },
            new CustomTournamentUserAssignment
            {
                TournamentId = tournament.TournamentId,
                UserId = player.Id,
                UserAdminName = "Player",
                UserName = "Player",
                Status = AssignmentStatus.Accepted,
                Role = UserTournamentRole.Player
            });

        var teams = new[]
        {
            new CustomTeam { TournamentId = tournament.TournamentId, TeamName = "Winner" },
            new CustomTeam { TournamentId = tournament.TournamentId, TeamName = "Second" },
            new CustomTeam { TournamentId = tournament.TournamentId, TeamName = "Third" },
            new CustomTeam { TournamentId = tournament.TournamentId, TeamName = "Scorer Team" }
        };

        dbContext.CustomTeams.AddRange(teams);
        await dbContext.SaveChangesAsync();

        var stage = new CustomMatchStage
        {
            TournamentId = tournament.TournamentId,
            StageName = "Group",
            Order = 1
        };

        dbContext.CustomMatchStages.Add(stage);
        await dbContext.SaveChangesAsync();

        dbContext.CustomMatches.Add(new CustomMatch
        {
            TournamentId = tournament.TournamentId,
            StageId = stage.StageId,
            HomeTeamId = teams[0].TeamId,
            AwayTeamId = teams[1].TeamId,
            MatchStart = DateTime.SpecifyKind(matchStart, DateTimeKind.Utc),
            Type = CustomMatch.MatchType.Regular90Min,
            Status = CustomMatch.MatchStatus.Timed,
            HomeWinOdds = 1.5m,
            DrawOdds = 3.5m,
            AwayWinOdds = 2.5m,
            IsVisible = true
        });

        await dbContext.SaveChangesAsync();

        return new ExtraPredictionSeed(
            tournament.TournamentId,
            teams[0].TeamId,
            teams[1].TeamId,
            teams[2].TeamId,
            teams[3].TeamId);
    }

    private sealed record ExtraPredictionSeed(
        int TournamentId,
        int WinnerTeamId,
        int SecondPlaceTeamId,
        int ThirdPlaceTeamId,
        int TopScorerTeamId);

    private sealed class ThrowingAcceptedInviteNotificationService : INotificationService
    {
        public Task NotifyMatchClosureAsync(CustomMatch match) => Task.CompletedTask;

        public Task NotifyUserAcceptedTournamentInviteAsync(CustomTournamentUserAssignment assignment)
            => throw new InvalidOperationException("Notification failed after accept.");

        public Task NotifyAdminsJoinRequestAsync(CustomTournamentUserAssignment joinRequest) => Task.CompletedTask;

        public Task NotifyUserJoinRequestApprovedAsync(CustomTournamentUserAssignment assignment) => Task.CompletedTask;

        public Task NotifyMatchStartingSoonAsync(CustomMatch match, TimeSpan threshold) => Task.CompletedTask;

        public Task NotifyDailyTournamentUpdatesAsync(DateTime nowUtc) => Task.CompletedTask;

        public Task NotifyNewGamesToBetAsync(CustomMatch match) => Task.CompletedTask;

        public Task NotifyTournamentInvitationsAsync(int tournamentId, IEnumerable<string> userEmails) => Task.CompletedTask;

        public Task NotifySuperAdminsAboutSupportMessageAsync(SupportMessage message) => Task.CompletedTask;

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

    private sealed class ThrowingJoinRequestNotificationService : INotificationService
    {
        public Task NotifyMatchClosureAsync(CustomMatch match) => Task.CompletedTask;

        public Task NotifyUserAcceptedTournamentInviteAsync(CustomTournamentUserAssignment assignment) => Task.CompletedTask;

        public Task NotifyAdminsJoinRequestAsync(CustomTournamentUserAssignment joinRequest)
            => throw new InvalidOperationException("Notification failed after join request.");

        public Task NotifyUserJoinRequestApprovedAsync(CustomTournamentUserAssignment assignment) => Task.CompletedTask;

        public Task NotifyMatchStartingSoonAsync(CustomMatch match, TimeSpan threshold) => Task.CompletedTask;

        public Task NotifyDailyTournamentUpdatesAsync(DateTime nowUtc) => Task.CompletedTask;

        public Task NotifyNewGamesToBetAsync(CustomMatch match) => Task.CompletedTask;

        public Task NotifyTournamentInvitationsAsync(int tournamentId, IEnumerable<string> userEmails) => Task.CompletedTask;

        public Task NotifySuperAdminsAboutSupportMessageAsync(SupportMessage message) => Task.CompletedTask;

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
