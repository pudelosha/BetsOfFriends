using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Services.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Services;

public class BetServiceTests
{
    [Fact]
    public async Task UpdateBetAsync_WithFutureTimedMatch_UpdatesBet()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("future-bet@example.com");
        var seeded = await SeedBetAsync(
            host,
            user,
            DateTime.UtcNow.AddHours(2),
            CustomMatch.MatchStatus.Timed,
            Bet.BetStatus.ToPlace);

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBetService>();

        var updated = await service.UpdateBetAsync(seeded.BetId, user.Id, new BetUpdateDto
        {
            BaseAmount = 1,
            HomeGoals = 2,
            AwayGoals = 1,
            QualifiedTeam = null
        });

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedBet = await dbContext.Bets.AsNoTracking().SingleAsync(b => b.BetId == seeded.BetId);

        Assert.True(updated);
        Assert.Equal(2, savedBet.HomeGoals);
        Assert.Equal(1, savedBet.AwayGoals);
        Assert.Equal(Bet.BetStatus.Placed, savedBet.Status);
    }

    [Fact]
    public async Task UpdateBetAsync_WithStartedTimedMatch_DoesNotUpdateBet()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("started-bet@example.com");
        var seeded = await SeedBetAsync(
            host,
            user,
            DateTime.UtcNow.AddMinutes(-1),
            CustomMatch.MatchStatus.Timed,
            Bet.BetStatus.ToPlace);

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBetService>();

        var updated = await service.UpdateBetAsync(seeded.BetId, user.Id, new BetUpdateDto
        {
            BaseAmount = 1,
            HomeGoals = 2,
            AwayGoals = 1
        });

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedBet = await dbContext.Bets.AsNoTracking().SingleAsync(b => b.BetId == seeded.BetId);

        Assert.False(updated);
        Assert.Null(savedBet.HomeGoals);
        Assert.Null(savedBet.AwayGoals);
        Assert.Equal(Bet.BetStatus.ToPlace, savedBet.Status);
    }

    [Fact]
    public async Task UpdateBetAsync_WithFutureInPlayMatch_DoesNotUpdateBet()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("in-play-bet@example.com");
        var seeded = await SeedBetAsync(
            host,
            user,
            DateTime.UtcNow.AddHours(2),
            CustomMatch.MatchStatus.In_Play,
            Bet.BetStatus.Placed,
            homeGoals: 1,
            awayGoals: 1);

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBetService>();

        var updated = await service.UpdateBetAsync(seeded.BetId, user.Id, new BetUpdateDto
        {
            BaseAmount = 1,
            HomeGoals = 3,
            AwayGoals = 0
        });

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedBet = await dbContext.Bets.AsNoTracking().SingleAsync(b => b.BetId == seeded.BetId);

        Assert.False(updated);
        Assert.Equal(1, savedBet.HomeGoals);
        Assert.Equal(1, savedBet.AwayGoals);
        Assert.Equal(Bet.BetStatus.Placed, savedBet.Status);
    }

    [Fact]
    public async Task GetBetsByStatusAndStageAsync_ForOpenStatuses_ExcludesStartedMatches()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("open-list-bets@example.com");
        var future = await SeedBetAsync(
            host,
            user,
            DateTime.UtcNow.AddHours(2),
            CustomMatch.MatchStatus.Timed,
            Bet.BetStatus.ToPlace,
            stageName: "Round 1",
            teamPrefix: "Future");
        var past = await SeedAdditionalBetAsync(
            host,
            user,
            future.TournamentId,
            DateTime.UtcNow.AddMinutes(-1),
            CustomMatch.MatchStatus.Timed,
            Bet.BetStatus.ToPlace,
            stageName: "Round 1",
            teamPrefix: "Past");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBetService>();

        var bets = await service.GetBetsByStatusAndStageAsync(future.TournamentId, user.Id, "ToPlace", "Round 1");

        var bet = Assert.Single(bets);
        Assert.Equal(future.BetId, bet.BetId);
        Assert.DoesNotContain(bets, b => b.BetId == past.BetId);
    }

    [Fact]
    public async Task GetUpcomingBetsAsync_ExcludesStartedMatches()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("upcoming-bets@example.com");
        var future = await SeedBetAsync(
            host,
            user,
            DateTime.UtcNow.AddHours(2),
            CustomMatch.MatchStatus.Timed,
            Bet.BetStatus.ToPlace,
            teamPrefix: "Future");
        await SeedAdditionalBetAsync(
            host,
            user,
            future.TournamentId,
            DateTime.UtcNow.AddMinutes(-1),
            CustomMatch.MatchStatus.Timed,
            Bet.BetStatus.ToPlace,
            teamPrefix: "Past");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBetService>();

        var upcomingBets = await service.GetUpcomingBetsAsync(future.TournamentId, user.Id);

        var bet = Assert.Single(upcomingBets);
        Assert.Equal(future.MatchId, bet.MatchId);
    }

    [Fact]
    public async Task GetInProgressBetsAsync_ReturnsCurrentUserBetsWithLiveScore()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("in-progress-home@example.com");
        var otherUser = await host.CreateUserAsync("other-in-progress-home@example.com");

        int tournamentId;
        int inProgressMatchId;

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tournament = new CustomTournament
            {
                Name = "In Progress Home Cup",
                CreatedByUserId = user.Id,
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
                TeamName = "Germany",
                EloRating = 1000
            };
            var awayTeam = new CustomTeam
            {
                TournamentId = tournamentId,
                TeamName = "England",
                EloRating = 1000
            };

            dbContext.CustomMatchStages.Add(stage);
            dbContext.CustomTeams.AddRange(homeTeam, awayTeam);
            await dbContext.SaveChangesAsync();

            var inProgressMatch = CreateMatch(
                tournamentId,
                stage.StageId,
                homeTeam.TeamId,
                awayTeam.TeamId,
                DateTime.UtcNow.AddMinutes(-20),
                CustomMatch.MatchStatus.In_Play,
                homeScoreLive: 1,
                awayScoreLive: 0);

            var futureMatch = CreateMatch(
                tournamentId,
                stage.StageId,
                homeTeam.TeamId,
                awayTeam.TeamId,
                DateTime.UtcNow.AddHours(2));

            dbContext.CustomMatches.AddRange(inProgressMatch, futureMatch);
            await dbContext.SaveChangesAsync();
            inProgressMatchId = inProgressMatch.MatchId;

            dbContext.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
            {
                TournamentId = tournamentId,
                UserId = user.Id,
                UserAdminName = "Player",
                UserName = "Player",
                Status = AssignmentStatus.Accepted,
                Role = UserTournamentRole.Player
            });

            dbContext.Bets.AddRange(
                CreateBet(inProgressMatch.MatchId, user.Id, 2, 1, Bet.BetStatus.Closed),
                CreateBet(inProgressMatch.MatchId, otherUser.Id, 0, 0, Bet.BetStatus.Closed),
                CreateBet(futureMatch.MatchId, user.Id, 1, 1, Bet.BetStatus.Placed));

            await dbContext.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBetService>();

            var bets = await service.GetInProgressBetsAsync(tournamentId, user.Id);
            var unauthorizedBets = await service.GetInProgressBetsAsync(tournamentId, otherUser.Id);

            var bet = Assert.Single(bets);
            Assert.Equal(inProgressMatchId, bet.MatchId);
            Assert.Equal("Germany", bet.TeamHome);
            Assert.Equal("England", bet.TeamAway);
            Assert.Equal(1, bet.ActualHomeGoals);
            Assert.Equal(0, bet.ActualAwayGoals);
            Assert.Equal(2, bet.PlayerHomeGoals);
            Assert.Equal(1, bet.PlayerAwayGoals);
            Assert.Empty(unauthorizedBets);
        }
    }

    [Fact]
    public async Task RecalculateBetsForMatchAsync_AppliesNonSubmittedPenaltyOnlyToMissingBets()
    {
        using var host = new BackendTestHost();
        var submittedUser = await host.CreateUserAsync("submitted-penalty@example.com");
        var missingUser = await host.CreateUserAsync("missing-penalty@example.com");

        int matchId;

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tournament = new CustomTournament
            {
                Name = "Penalty Cup",
                CreatedByUserId = submittedUser.Id,
                IsActive = true,
                AllowNonSubmittedBetsPenalty = true,
                NonSubmittedBetPenalty = 5
            };

            dbContext.CustomTournaments.Add(tournament);
            await dbContext.SaveChangesAsync();

            var stage = new CustomMatchStage
            {
                TournamentId = tournament.TournamentId,
                StageName = "Final",
                Order = 1
            };
            var homeTeam = new CustomTeam
            {
                TournamentId = tournament.TournamentId,
                TeamName = "Home",
                EloRating = 1000
            };
            var awayTeam = new CustomTeam
            {
                TournamentId = tournament.TournamentId,
                TeamName = "Away",
                EloRating = 1000
            };

            dbContext.CustomMatchStages.Add(stage);
            dbContext.CustomTeams.AddRange(homeTeam, awayTeam);
            await dbContext.SaveChangesAsync();

            var match = CreateMatch(
                tournament.TournamentId,
                stage.StageId,
                homeTeam.TeamId,
                awayTeam.TeamId,
                DateTime.UtcNow.AddHours(-2),
                CustomMatch.MatchStatus.Finished);
            match.HomeScore = 2;
            match.AwayScore = 0;
            match.HomeWinOdds = 1.71m;

            dbContext.CustomMatches.Add(match);
            await dbContext.SaveChangesAsync();
            matchId = match.MatchId;

            dbContext.Bets.AddRange(
                new Bet
                {
                    MatchId = matchId,
                    UserId = submittedUser.Id,
                    BaseAmount = 1,
                    HomeGoals = 2,
                    AwayGoals = 0,
                    Status = Bet.BetStatus.Placed,
                    Result = Bet.BetResult.Pending,
                    Calculated = false
                },
                new Bet
                {
                    MatchId = matchId,
                    UserId = missingUser.Id,
                    BaseAmount = 1,
                    HomeGoals = null,
                    AwayGoals = null,
                    Status = Bet.BetStatus.ToPlace,
                    Result = Bet.BetResult.Pending,
                    Calculated = false
                });

            await dbContext.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBetService>();

            await service.RecalculateBetsForMatchAsync(matchId);
        }

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var submittedBet = await dbContext.Bets
                .AsNoTracking()
                .SingleAsync(bet => bet.MatchId == matchId && bet.UserId == submittedUser.Id);
            var missingBet = await dbContext.Bets
                .AsNoTracking()
                .SingleAsync(bet => bet.MatchId == matchId && bet.UserId == missingUser.Id);

            Assert.Equal(1.71m, submittedBet.BasePayout);
            Assert.Equal(-5m, missingBet.BasePayout);
        }
    }

    [Fact]
    public async Task GetMissingBetsSummaryAsync_ForTournamentAdmin_ReturnsMissingBetsForUpcoming48Hours()
    {
        using var host = new BackendTestHost();
        var admin = await host.CreateUserAsync("missing-admin@example.com");
        var player = await host.CreateUserAsync("missing-player@example.com");

        int tournamentId;
        int missingMatchId;

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var nowUtc = DateTime.UtcNow;

            var tournament = new CustomTournament
            {
                Name = "Missing Bets Cup",
                CreatedByUserId = admin.Id,
                IsActive = true
            };

            dbContext.CustomTournaments.Add(tournament);
            await dbContext.SaveChangesAsync();
            tournamentId = tournament.TournamentId;

            var stage = new CustomMatchStage
            {
                TournamentId = tournamentId,
                StageName = "Group A",
                Order = 1
            };
            var homeTeam = new CustomTeam
            {
                TournamentId = tournamentId,
                TeamName = "England",
                EloRating = 1000
            };
            var awayTeam = new CustomTeam
            {
                TournamentId = tournamentId,
                TeamName = "Germany",
                EloRating = 1000
            };

            dbContext.CustomMatchStages.Add(stage);
            dbContext.CustomTeams.AddRange(homeTeam, awayTeam);
            await dbContext.SaveChangesAsync();

            var missingMatch = CreateMatch(tournamentId, stage.StageId, homeTeam.TeamId, awayTeam.TeamId, nowUtc.AddHours(2));
            var completedMatch = CreateMatch(tournamentId, stage.StageId, homeTeam.TeamId, awayTeam.TeamId, nowUtc.AddHours(4));
            var outsideWindowMatch = CreateMatch(tournamentId, stage.StageId, homeTeam.TeamId, awayTeam.TeamId, nowUtc.AddHours(72));

            dbContext.CustomMatches.AddRange(missingMatch, completedMatch, outsideWindowMatch);
            await dbContext.SaveChangesAsync();
            missingMatchId = missingMatch.MatchId;

            dbContext.CustomTournamentUserAssignments.AddRange(
                new CustomTournamentUserAssignment
                {
                    TournamentId = tournamentId,
                    UserId = admin.Id,
                    UserAdminName = "Admin",
                    UserName = "Admin",
                    Status = AssignmentStatus.Accepted,
                    Role = UserTournamentRole.Admin
                },
                new CustomTournamentUserAssignment
                {
                    TournamentId = tournamentId,
                    UserId = player.Id,
                    UserAdminName = "Johny",
                    UserName = "Johny",
                    Status = AssignmentStatus.Accepted,
                    Role = UserTournamentRole.Player
                });

            dbContext.Bets.AddRange(
                CreateBet(missingMatch.MatchId, admin.Id, 1, 1, Bet.BetStatus.Placed),
                new Bet
                {
                    MatchId = missingMatch.MatchId,
                    UserId = player.Id,
                    BaseAmount = 1,
                    Status = Bet.BetStatus.ToPlace,
                    Result = Bet.BetResult.Pending
                },
                CreateBet(completedMatch.MatchId, admin.Id, 2, 1, Bet.BetStatus.Placed),
                CreateBet(completedMatch.MatchId, player.Id, 1, 0, Bet.BetStatus.Placed),
                CreateBet(outsideWindowMatch.MatchId, admin.Id, 0, 0, Bet.BetStatus.Placed),
                new Bet
                {
                    MatchId = outsideWindowMatch.MatchId,
                    UserId = player.Id,
                    BaseAmount = 1,
                    Status = Bet.BetStatus.ToPlace,
                    Result = Bet.BetResult.Pending
                });

            await dbContext.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBetService>();

            var adminSummary = await service.GetMissingBetsSummaryAsync(tournamentId, admin.Id);
            var playerSummary = await service.GetMissingBetsSummaryAsync(tournamentId, player.Id);

            Assert.NotNull(adminSummary);
            Assert.True(adminSummary.CanView);

            var match = Assert.Single(adminSummary.Matches);
            Assert.Equal(missingMatchId, match.MatchId);
            Assert.Equal("England", match.HomeTeam);
            Assert.Equal("Germany", match.AwayTeam);

            var participant = Assert.Single(match.Participants);
            Assert.Equal(player.Id, participant.UserId);
            Assert.Equal("Johny", participant.UserName);

            Assert.NotNull(playerSummary);
            Assert.False(playerSummary.CanView);
            Assert.Empty(playerSummary.Matches);
        }
    }

    [Fact]
    public async Task GetBetStatisticsAsync_UsesOnlyAcceptedParticipantsForComparison()
    {
        using var host = new BackendTestHost();
        var acceptedUser = await host.CreateUserAsync("accepted-bets@example.com");
        var invitedUser = await host.CreateUserAsync("invited-bets@example.com");
        var deletedUser = await host.CreateUserAsync("deleted-bets@example.com");

        int matchId;

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tournament = new CustomTournament
            {
                Name = "Bets Overview Cup",
                CreatedByUserId = acceptedUser.Id,
                IsActive = true
            };

            dbContext.CustomTournaments.Add(tournament);
            await dbContext.SaveChangesAsync();

            var stage = new CustomMatchStage
            {
                TournamentId = tournament.TournamentId,
                StageName = "Final",
                Order = 1
            };
            var homeTeam = new CustomTeam
            {
                TournamentId = tournament.TournamentId,
                TeamName = "Home",
                EloRating = 1000
            };
            var awayTeam = new CustomTeam
            {
                TournamentId = tournament.TournamentId,
                TeamName = "Away",
                EloRating = 1000
            };

            dbContext.CustomMatchStages.Add(stage);
            dbContext.CustomTeams.AddRange(homeTeam, awayTeam);
            await dbContext.SaveChangesAsync();

            var match = new CustomMatch
            {
                TournamentId = tournament.TournamentId,
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
            matchId = match.MatchId;

            dbContext.CustomTournamentUserAssignments.AddRange(
                new CustomTournamentUserAssignment
                {
                    TournamentId = tournament.TournamentId,
                    UserId = acceptedUser.Id,
                    UserAdminName = "Accepted Admin",
                    UserName = "Accepted Player",
                    Status = AssignmentStatus.Accepted,
                    Role = UserTournamentRole.Admin
                },
                new CustomTournamentUserAssignment
                {
                    TournamentId = tournament.TournamentId,
                    UserId = invitedUser.Id,
                    UserAdminName = "Invited Admin",
                    UserName = "Invited Player",
                    Status = AssignmentStatus.Invited,
                    Role = UserTournamentRole.Player
                });

            dbContext.Bets.AddRange(
                CreateClosedBet(matchId, acceptedUser.Id, homeGoals: 2, awayGoals: 1),
                CreateClosedBet(matchId, invitedUser.Id, homeGoals: 1, awayGoals: 2),
                CreateClosedBet(matchId, deletedUser.Id, homeGoals: 1, awayGoals: 1));

            await dbContext.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBetService>();

            var stats = await service.GetBetStatisticsAsync(matchId, acceptedUser.Id);

            Assert.NotNull(stats);
            Assert.Equal(100, stats.Percent1);
            Assert.Equal(0, stats.PercentX);
            Assert.Equal(0, stats.Percent2);
            Assert.Equal(1, stats.PlacedBetsCount);
            Assert.Equal(1, stats.ParticipantsCount);
            Assert.Equal(2.0m, stats.AverageHomeGoals);
            Assert.Equal(1.0m, stats.AverageAwayGoals);

            var userBet = Assert.Single(stats.UserBets!);
            Assert.Equal("Accepted Player", userBet.Username);
        }
    }

    [Fact]
    public async Task GetBetStatisticsAsync_WhenMatchIsInPlay_ShowsPlacedUserBets()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("in-play-bets@example.com");

        int matchId;

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tournament = new CustomTournament
            {
                Name = "In Play Bets Cup",
                CreatedByUserId = user.Id,
                IsActive = true
            };
            dbContext.CustomTournaments.Add(tournament);
            await dbContext.SaveChangesAsync();

            var stage = new CustomMatchStage
            {
                TournamentId = tournament.TournamentId,
                StageName = "Final",
                Order = 1
            };
            var homeTeam = new CustomTeam
            {
                TournamentId = tournament.TournamentId,
                TeamName = "Home",
                EloRating = 1000
            };
            var awayTeam = new CustomTeam
            {
                TournamentId = tournament.TournamentId,
                TeamName = "Away",
                EloRating = 1000
            };
            dbContext.CustomMatchStages.Add(stage);
            dbContext.CustomTeams.AddRange(homeTeam, awayTeam);
            await dbContext.SaveChangesAsync();

            var match = new CustomMatch
            {
                TournamentId = tournament.TournamentId,
                StageId = stage.StageId,
                HomeTeamId = homeTeam.TeamId,
                AwayTeamId = awayTeam.TeamId,
                MatchStart = DateTime.UtcNow.AddMinutes(-5),
                Status = CustomMatch.MatchStatus.In_Play,
                Type = CustomMatch.MatchType.Regular90Min,
                HomeWinOdds = 2,
                DrawOdds = 3,
                AwayWinOdds = 4
            };
            dbContext.CustomMatches.Add(match);
            await dbContext.SaveChangesAsync();
            matchId = match.MatchId;

            dbContext.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
            {
                TournamentId = tournament.TournamentId,
                UserId = user.Id,
                UserAdminName = "In Play Admin",
                UserName = "In Play Player",
                Status = AssignmentStatus.Accepted,
                Role = UserTournamentRole.Player
            });
            dbContext.Bets.Add(CreateBet(matchId, user.Id, homeGoals: 1, awayGoals: 1, Bet.BetStatus.Placed));
            await dbContext.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBetService>();

            var stats = await service.GetBetStatisticsAsync(matchId, user.Id);

            Assert.NotNull(stats);
            Assert.Equal("In_Play", stats.MatchStatus);
            var userBet = Assert.Single(stats.UserBets!);
            Assert.Equal("In Play Player", userBet.Username);
            Assert.Equal("1-1", userBet.BetScore);
        }
    }

    private static Bet CreateClosedBet(int matchId, string userId, int homeGoals, int awayGoals)
        => CreateBet(matchId, userId, homeGoals, awayGoals, Bet.BetStatus.Closed);

    private static Bet CreateBet(int matchId, string userId, int homeGoals, int awayGoals, Bet.BetStatus status)
    {
        return new Bet
        {
            MatchId = matchId,
            UserId = userId,
            BaseAmount = 1,
            HomeGoals = homeGoals,
            AwayGoals = awayGoals,
            Status = status,
            Result = Bet.BetResult.Won,
            BasePayout = 1,
            Calculated = true
        };
    }

    private static CustomMatch CreateMatch(
        int tournamentId,
        int stageId,
        int homeTeamId,
        int awayTeamId,
        DateTime matchStartUtc,
        CustomMatch.MatchStatus matchStatus = CustomMatch.MatchStatus.Timed,
        int? homeScoreLive = null,
        int? awayScoreLive = null)
    {
        return new CustomMatch
        {
            TournamentId = tournamentId,
            StageId = stageId,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            MatchStart = DateTime.SpecifyKind(matchStartUtc, DateTimeKind.Utc),
            Status = matchStatus,
            Type = CustomMatch.MatchType.Regular90Min,
            HomeScoreLive = homeScoreLive,
            AwayScoreLive = awayScoreLive,
            HomeWinOdds = 2,
            DrawOdds = 3,
            AwayWinOdds = 4,
            IsVisible = true
        };
    }

    private static async Task<SeededBet> SeedBetAsync(
        BackendTestHost host,
        ApplicationUser user,
        DateTime matchStartUtc,
        CustomMatch.MatchStatus matchStatus,
        Bet.BetStatus betStatus,
        string stageName = "Final",
        string teamPrefix = "Seeded",
        int? homeGoals = null,
        int? awayGoals = null)
    {
        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tournament = new CustomTournament
        {
            Name = $"{teamPrefix} Cup",
            CreatedByUserId = user.Id,
            IsActive = true
        };

        dbContext.CustomTournaments.Add(tournament);
        await dbContext.SaveChangesAsync();

        var stage = new CustomMatchStage
        {
            TournamentId = tournament.TournamentId,
            StageName = stageName,
            Order = 1
        };
        var homeTeam = new CustomTeam
        {
            TournamentId = tournament.TournamentId,
            TeamName = $"{teamPrefix} Home",
            EloRating = 1000
        };
        var awayTeam = new CustomTeam
        {
            TournamentId = tournament.TournamentId,
            TeamName = $"{teamPrefix} Away",
            EloRating = 1000
        };

        dbContext.CustomMatchStages.Add(stage);
        dbContext.CustomTeams.AddRange(homeTeam, awayTeam);
        await dbContext.SaveChangesAsync();

        var match = new CustomMatch
        {
            TournamentId = tournament.TournamentId,
            StageId = stage.StageId,
            HomeTeamId = homeTeam.TeamId,
            AwayTeamId = awayTeam.TeamId,
            MatchStart = DateTime.SpecifyKind(matchStartUtc, DateTimeKind.Utc),
            Status = matchStatus,
            Type = CustomMatch.MatchType.Regular90Min,
            HomeWinOdds = 2,
            DrawOdds = 3,
            AwayWinOdds = 4,
            IsVisible = true
        };

        dbContext.CustomMatches.Add(match);
        await dbContext.SaveChangesAsync();

        dbContext.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
        {
            TournamentId = tournament.TournamentId,
            UserId = user.Id,
            UserAdminName = user.Email!,
            UserName = user.Email!.Split('@')[0],
            Status = AssignmentStatus.Accepted,
            Role = UserTournamentRole.Player
        });

        var bet = new Bet
        {
            MatchId = match.MatchId,
            UserId = user.Id,
            BaseAmount = 1,
            HomeGoals = homeGoals,
            AwayGoals = awayGoals,
            Status = betStatus,
            Result = Bet.BetResult.Pending,
            Calculated = false
        };

        dbContext.Bets.Add(bet);
        await dbContext.SaveChangesAsync();

        return new SeededBet(tournament.TournamentId, match.MatchId, bet.BetId);
    }

    private static async Task<SeededBet> SeedAdditionalBetAsync(
        BackendTestHost host,
        ApplicationUser user,
        int tournamentId,
        DateTime matchStartUtc,
        CustomMatch.MatchStatus matchStatus,
        Bet.BetStatus betStatus,
        string stageName = "Final",
        string teamPrefix = "Seeded",
        int? homeGoals = null,
        int? awayGoals = null)
    {
        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stage = await dbContext.CustomMatchStages
            .FirstOrDefaultAsync(s => s.TournamentId == tournamentId && s.StageName == stageName);

        if (stage == null)
        {
            stage = new CustomMatchStage
            {
                TournamentId = tournamentId,
                StageName = stageName,
                Order = 1
            };
            dbContext.CustomMatchStages.Add(stage);
            await dbContext.SaveChangesAsync();
        }

        var homeTeam = new CustomTeam
        {
            TournamentId = tournamentId,
            TeamName = $"{teamPrefix} Home",
            EloRating = 1000
        };
        var awayTeam = new CustomTeam
        {
            TournamentId = tournamentId,
            TeamName = $"{teamPrefix} Away",
            EloRating = 1000
        };

        dbContext.CustomTeams.AddRange(homeTeam, awayTeam);
        await dbContext.SaveChangesAsync();

        var match = new CustomMatch
        {
            TournamentId = tournamentId,
            StageId = stage.StageId,
            HomeTeamId = homeTeam.TeamId,
            AwayTeamId = awayTeam.TeamId,
            MatchStart = DateTime.SpecifyKind(matchStartUtc, DateTimeKind.Utc),
            Status = matchStatus,
            Type = CustomMatch.MatchType.Regular90Min,
            HomeWinOdds = 2,
            DrawOdds = 3,
            AwayWinOdds = 4,
            IsVisible = true
        };

        dbContext.CustomMatches.Add(match);
        await dbContext.SaveChangesAsync();

        var bet = new Bet
        {
            MatchId = match.MatchId,
            UserId = user.Id,
            BaseAmount = 1,
            HomeGoals = homeGoals,
            AwayGoals = awayGoals,
            Status = betStatus,
            Result = Bet.BetResult.Pending,
            Calculated = false
        };

        dbContext.Bets.Add(bet);
        await dbContext.SaveChangesAsync();

        return new SeededBet(tournamentId, match.MatchId, bet.BetId);
    }

    private sealed record SeededBet(int TournamentId, int MatchId, int BetId);
}
