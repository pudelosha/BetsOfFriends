using System.Reflection;
using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Services.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.Services;

public class FootballDataHostedServiceTests
{
    [Fact]
    public async Task CheckTournamentChangesAsync_PropagatesQualifiedTeamAndRecalculatesQualificationBets()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("auto-qualified@example.com");
        var footballDataService = new StubFootballDataService(CreateFinishedMatchDto());

        int customMatchId;
        int betId;

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var predefinedTournament = new PredefinedTournament
            {
                TournamentName = "FIFA World Cup",
                ExternalTournamentId = 2000,
                Season = 2026,
                Update = CustomTournament.TournamentUpdate.Auto,
                IsActive = true
            };
            dbContext.PredefinedTournaments.Add(predefinedTournament);
            await dbContext.SaveChangesAsync();

            var predefinedStage = new PredefinedMatchStage
            {
                TournamentId = predefinedTournament.TournamentId,
                StageName = "LAST_32",
                Order = 1
            };
            var predefinedHome = new PredefinedTeam
            {
                PredefinedTournamentId = predefinedTournament.TournamentId,
                ExternalTeamId = 774,
                TeamName = "South Africa",
                EloRating = 1000
            };
            var predefinedAway = new PredefinedTeam
            {
                PredefinedTournamentId = predefinedTournament.TournamentId,
                ExternalTeamId = 828,
                TeamName = "Canada",
                EloRating = 1000
            };

            dbContext.PredefinedMatchStages.Add(predefinedStage);
            dbContext.PredefinedTeams.AddRange(predefinedHome, predefinedAway);
            await dbContext.SaveChangesAsync();

            var predefinedMatch = new PredefinedMatch
            {
                TournamentId = predefinedTournament.TournamentId,
                StageId = predefinedStage.StageId,
                HomeTeamId = predefinedHome.TeamId,
                AwayTeamId = predefinedAway.TeamId,
                MatchStart = DateTime.UtcNow.AddHours(-2),
                Status = CustomMatch.MatchStatus.In_Play,
                Type = CustomMatch.MatchType.ExtendedWithQualification,
                ExternalMatchId = 537417,
                HomeWinOdds = 2,
                DrawOdds = 3,
                AwayWinOdds = 4,
                HomeQualifies = 1.6m,
                AwayQualifies = 1.8m
            };
            dbContext.PredefinedMatches.Add(predefinedMatch);
            await dbContext.SaveChangesAsync();

            var customTournament = new CustomTournament
            {
                Name = "Friends World Cup",
                CreatedByUserId = user.Id,
                PredefinedTournamentId = predefinedTournament.TournamentId,
                Update = CustomTournament.TournamentUpdate.Auto,
                IsActive = true,
                AllowWhoQualifiesBets = true
            };
            dbContext.CustomTournaments.Add(customTournament);
            await dbContext.SaveChangesAsync();

            var customStage = new CustomMatchStage
            {
                TournamentId = customTournament.TournamentId,
                StageName = "LAST_32",
                Order = 1
            };
            var customHome = new CustomTeam
            {
                TournamentId = customTournament.TournamentId,
                PredefinedTeamId = predefinedHome.TeamId,
                TeamName = "South Africa",
                EloRating = 1000
            };
            var customAway = new CustomTeam
            {
                TournamentId = customTournament.TournamentId,
                PredefinedTeamId = predefinedAway.TeamId,
                TeamName = "Canada",
                EloRating = 1000
            };

            dbContext.CustomMatchStages.Add(customStage);
            dbContext.CustomTeams.AddRange(customHome, customAway);
            await dbContext.SaveChangesAsync();

            var customMatch = new CustomMatch
            {
                TournamentId = customTournament.TournamentId,
                StageId = customStage.StageId,
                PredefinedMatchId = predefinedMatch.MatchId,
                HomeTeamId = customHome.TeamId,
                AwayTeamId = customAway.TeamId,
                MatchStart = DateTime.UtcNow.AddHours(-2),
                Status = CustomMatch.MatchStatus.In_Play,
                Type = CustomMatch.MatchType.ExtendedWithQualification,
                HomeWinOdds = 2,
                DrawOdds = 3,
                AwayWinOdds = 4,
                HomeQualifies = 1.6m,
                AwayQualifies = 1.8m,
                IsVisible = true
            };
            dbContext.CustomMatches.Add(customMatch);
            await dbContext.SaveChangesAsync();
            customMatchId = customMatch.MatchId;

            var bet = new Bet
            {
                MatchId = customMatchId,
                UserId = user.Id,
                BaseAmount = 1,
                HomeGoals = 0,
                AwayGoals = 1,
                Qualified = CustomMatch.TeamQualified.Away,
                Status = Bet.BetStatus.Placed,
                Result = Bet.BetResult.Pending,
                Calculated = false
            };
            dbContext.Bets.Add(bet);
            await dbContext.SaveChangesAsync();
            betId = bet.BetId;
        }

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var betService = scope.ServiceProvider.GetRequiredService<IBetService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<FootballDataHostedService>>();
            var hostedService = new FootballDataHostedService(host.Services, logger);

            await RunTournamentCheckAsync(hostedService, dbContext, footballDataService, betService, notificationService);
            await RunTournamentCheckAsync(hostedService, dbContext, footballDataService, betService, notificationService);
        }

        using (var scope = host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customMatch = await dbContext.CustomMatches
                .AsNoTracking()
                .SingleAsync(match => match.MatchId == customMatchId);
            var bet = await dbContext.Bets
                .AsNoTracking()
                .SingleAsync(savedBet => savedBet.BetId == betId);

            Assert.Equal(CustomMatch.MatchStatus.Finished, customMatch.Status);
            Assert.Equal(0, customMatch.HomeScore);
            Assert.Equal(1, customMatch.AwayScore);
            Assert.Equal(CustomMatch.TeamQualified.Away, customMatch.Qualified);
            Assert.Equal(4m, bet.BasePayout);
            Assert.Equal(1.8m, bet.QualificationPayout);
            Assert.Equal(Bet.BetStatus.Closed, bet.Status);
            Assert.Equal(Bet.BetResult.Won, bet.Result);
        }
    }

    private static async Task RunTournamentCheckAsync(
        FootballDataHostedService hostedService,
        AppDbContext dbContext,
        IFootballDataService footballDataService,
        IBetService betService,
        INotificationService notificationService)
    {
        var method = typeof(FootballDataHostedService).GetMethod(
            "CheckTournamentChangesAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var task = (Task)method.Invoke(
            hostedService,
            new object[]
            {
                dbContext,
                footballDataService,
                betService,
                notificationService,
                CancellationToken.None
            })!;

        await task;
    }

    private static PredefinedTournamentDto CreateFinishedMatchDto()
    {
        return new PredefinedTournamentDto
        {
            TournamentName = "FIFA World Cup",
            PublicTournamentName = "FIFA World Cup",
            ExternalTournamentId = 2000,
            Season = 2026,
            IsActive = true,
            UpdateMethod = "Auto",
            TournamentVisibility = "Private",
            Teams = new List<PredefinedTeamDto>
            {
                new()
                {
                    TeamName = "South Africa",
                    ExternalTeamId = 774,
                    CrestUrl = "https://crests.football-data.org/9396.svg",
                    RecordStatus = "Uploaded"
                },
                new()
                {
                    TeamName = "Canada",
                    ExternalTeamId = 828,
                    CrestUrl = "https://crests.football-data.org/canada.svg",
                    RecordStatus = "Uploaded"
                }
            },
            Matches = new List<PredefinedMatchDto>
            {
                new()
                {
                    ExternalMatchId = 537417,
                    MatchStart = DateTime.UtcNow.AddHours(-2),
                    MatchStatus = "Finished",
                    ScoreHome = 0,
                    ScoreAway = 1,
                    QualifiedTeam = "Away",
                    HomeTeam = "South Africa",
                    AwayTeam = "Canada",
                    StageName = "LAST_32",
                    MatchType = "ExtendedWithQualification",
                    RecordStatus = "Uploaded",
                    HomeWinOdds = 2,
                    DrawOdds = 3,
                    AwayWinOdds = 4,
                    HomeQualifies = 1.6m,
                    AwayQualifies = 1.8m
                }
            }
        };
    }

    private sealed class StubFootballDataService : IFootballDataService
    {
        private readonly PredefinedTournamentDto _dto;

        public StubFootballDataService(PredefinedTournamentDto dto)
        {
            _dto = dto;
        }

        public Task<string> GetCompetitionMatchesAsync(int competitionCode, int seasonCode)
        {
            return Task.FromResult("{}");
        }

        public Task<PredefinedTournamentDto> ConvertToPredefinedTournamentDtoAsync(string json)
        {
            return Task.FromResult(_dto);
        }
    }
}
