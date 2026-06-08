using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Services.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Services;

public class BetServiceTests
{
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

            var userBet = Assert.Single(stats.UserBets!);
            Assert.Equal("Accepted Player", userBet.Username);
        }
    }

    private static Bet CreateClosedBet(int matchId, string userId, int homeGoals, int awayGoals)
    {
        return new Bet
        {
            MatchId = matchId,
            UserId = userId,
            BaseAmount = 1,
            HomeGoals = homeGoals,
            AwayGoals = awayGoals,
            Status = Bet.BetStatus.Closed,
            Result = Bet.BetResult.Won,
            BasePayout = 1,
            Calculated = true
        };
    }
}
