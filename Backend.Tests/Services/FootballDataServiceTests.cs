using Backend.DTOs;
using Backend.Repository.Services;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Services;

public class FootballDataServiceTests
{
    [Fact]
    public async Task ConvertToPredefinedTournamentDtoAsync_UsesRegularTimeScoreAndWinnerForQualifier()
    {
        var service = new FootballDataService(
            new TestHttpClientFactory(),
            Options.Create(new FootballDataConfig
            {
                ApiBaseUrl = "https://football-data.test",
                AuthToken = "test-token"
            }));

        var json = """
            {
              "filters": { "season": 2026 },
              "competition": { "id": 2000, "name": "FIFA World Cup" },
              "matches": [
                {
                  "id": 537418,
                  "utcDate": "2026-06-29T19:00:00Z",
                  "status": "FINISHED",
                  "matchday": null,
                  "stage": "LAST_32",
                  "season": {
                    "id": 2026,
                    "startDate": "2026-06-11",
                    "endDate": "2026-07-19"
                  },
                  "homeTeam": {
                    "id": 1,
                    "name": "Home FC",
                    "crest": "https://crests.example/home.svg"
                  },
                  "awayTeam": {
                    "id": 2,
                    "name": "Away FC",
                    "crest": "https://crests.example/away.svg"
                  },
                  "score": {
                    "winner": "HOME_TEAM",
                    "duration": "EXTRA_TIME",
                    "regularTime": {
                      "home": 0,
                      "away": 0
                    },
                    "fullTime": {
                      "home": 1,
                      "away": 0
                    }
                  }
                }
              ]
            }
            """;

        var tournament = await service.ConvertToPredefinedTournamentDtoAsync(json);

        var match = Assert.Single(tournament.Matches);
        Assert.Equal(0, match.ScoreHome);
        Assert.Equal(0, match.ScoreAway);
        Assert.Equal("Home", match.QualifiedTeam);
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}
