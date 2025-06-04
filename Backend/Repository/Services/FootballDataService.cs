using Backend.DTOs;
using Backend.Repository.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Backend.Repository.Services
{
    public class FootballDataService : IFootballDataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _authToken;

        public FootballDataService(IHttpClientFactory factory, IOptions<FootballDataConfig> config)
        {
            _httpClient = factory.CreateClient();
            _baseUrl = config.Value.ApiBaseUrl;
            _authToken = config.Value.AuthToken;
        }

        public async Task<string> GetCompetitionMatchesAsync(int competitionCode, int seasonCode)
        {
            var url = $"{_baseUrl}/competitions/{competitionCode}/matches?season={seasonCode}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Auth-Token", _authToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<PredefinedTournamentDto> ConvertToPredefinedTournamentDtoAsync(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var matchesJson = root.GetProperty("matches");
            var competitionJson = root.GetProperty("competition");

            int? season = root.TryGetProperty("filters", out var filtersJson) &&
                          filtersJson.TryGetProperty("season", out var seasonProp) &&
                          seasonProp.ValueKind == JsonValueKind.Number
                          ? seasonProp.GetInt32()
                          : null;

            int? seasonId = null;
            DateTime? seasonStart = null;
            DateTime? seasonEnd = null;

            if (matchesJson.GetArrayLength() > 0 &&
                matchesJson[0].TryGetProperty("season", out var seasonJson))
            {
                if (seasonJson.TryGetProperty("id", out var seasonIdProp) && seasonIdProp.ValueKind == JsonValueKind.Number)
                    seasonId = seasonIdProp.GetInt32();

                if (seasonJson.TryGetProperty("startDate", out var startDateProp) &&
                    DateTime.TryParse(startDateProp.GetString(), out var parsedStart))
                    seasonStart = DateTime.SpecifyKind(parsedStart, DateTimeKind.Utc);

                if (seasonJson.TryGetProperty("endDate", out var endDateProp) &&
                    DateTime.TryParse(endDateProp.GetString(), out var parsedEnd))
                    seasonEnd = DateTime.SpecifyKind(parsedEnd, DateTimeKind.Utc);
            }

            var matches = new List<PredefinedMatchDto>();
            var teams = new Dictionary<string, PredefinedTeamDto>(StringComparer.OrdinalIgnoreCase);
            var stageMap = new List<KeyValuePair<string, string>>();

            const string placeholderTeamName = "TBD";

            teams[placeholderTeamName] = new PredefinedTeamDto
            {
                TeamName = placeholderTeamName,
                ExternalTeamId = null,
                RecordStatus = "New"
            };

            foreach (var match in matchesJson.EnumerateArray())
            {
                var externalMatchId = match.GetProperty("id").GetInt32();
                var utcDate = match.GetProperty("utcDate").GetDateTime();
                var matchStatus = match.TryGetProperty("status", out var statusProp)
                    ? statusProp.GetString()
                    : null;

                var matchday = match.TryGetProperty("matchday", out var mdProp) && mdProp.ValueKind == JsonValueKind.Number
                    ? mdProp.GetInt32()
                    : 0;

                var stageCode = match.TryGetProperty("stage", out var stgProp) && stgProp.ValueKind == JsonValueKind.String
                    ? stgProp.GetString()
                    : "UNKNOWN";

                var stageName = stageCode != null && matchday > 0
                    ? $"{ToTitleCase(stageCode.Replace("_", " "))} {matchday}"
                    : (stageCode ?? "Unknown Stage");

                var homeTeamJson = match.GetProperty("homeTeam");
                var awayTeamJson = match.GetProperty("awayTeam");

                var homeTeamName = homeTeamJson.GetProperty("name").GetString() ?? placeholderTeamName;
                var awayTeamName = awayTeamJson.GetProperty("name").GetString() ?? placeholderTeamName;

                var homeTeamId = homeTeamJson.TryGetProperty("id", out var homeIdProp) && homeIdProp.ValueKind == JsonValueKind.Number
                    ? homeIdProp.GetInt32()
                    : (int?)null;

                var awayTeamId = awayTeamJson.TryGetProperty("id", out var awayIdProp) && awayIdProp.ValueKind == JsonValueKind.Number
                    ? awayIdProp.GetInt32()
                    : (int?)null;

                if (!teams.ContainsKey(homeTeamName))
                {
                    teams[homeTeamName] = new PredefinedTeamDto
                    {
                        TeamName = homeTeamName,
                        ExternalTeamId = homeTeamId,
                        RecordStatus = "New"
                    };
                }

                if (!teams.ContainsKey(awayTeamName))
                {
                    teams[awayTeamName] = new PredefinedTeamDto
                    {
                        TeamName = awayTeamName,
                        ExternalTeamId = awayTeamId,
                        RecordStatus = "New"
                    };
                }

                var stageKey = $"{stageCode}_{matchday}";
                if (!stageMap.Any(kv => kv.Key == stageKey))
                    stageMap.Add(new KeyValuePair<string, string>(stageKey, stageName));

                // Score extraction logic (with fallback)
                int? scoreHome = null;
                int? scoreAway = null;
                string? qualified = null;

                if (match.TryGetProperty("score", out var scoreProp))
                {
                    bool hasValidRegular = false;

                    if (scoreProp.TryGetProperty("regularTime", out var regularScore) &&
                        regularScore.TryGetProperty("home", out var homeReg) &&
                        regularScore.TryGetProperty("away", out var awayReg) &&
                        homeReg.ValueKind == JsonValueKind.Number &&
                        awayReg.ValueKind == JsonValueKind.Number)
                    {
                        var regHome = homeReg.GetInt32();
                        var regAway = awayReg.GetInt32();

                        if (regHome != 0 || regAway != 0)
                        {
                            scoreHome = regHome;
                            scoreAway = regAway;
                            hasValidRegular = true;
                        }
                    }

                    if (!hasValidRegular &&
                        scoreProp.TryGetProperty("fullTime", out var fullScore) &&
                        fullScore.TryGetProperty("home", out var homeFull) &&
                        fullScore.TryGetProperty("away", out var awayFull) &&
                        homeFull.ValueKind == JsonValueKind.Number &&
                        awayFull.ValueKind == JsonValueKind.Number)
                    {
                        scoreHome = homeFull.GetInt32();
                        scoreAway = awayFull.GetInt32();
                    }

                    if (scoreProp.TryGetProperty("winner", out var winnerProp))
                    {
                        var winnerStr = winnerProp.GetString()?.ToUpperInvariant();
                        qualified = winnerStr switch
                        {
                            "HOME_TEAM" => "Home",
                            "AWAY_TEAM" => "Away",
                            _ => null
                        };
                    }
                }

                matches.Add(new PredefinedMatchDto
                {
                    ExternalMatchId = externalMatchId,
                    MatchStart = utcDate,
                    MatchStatus = MapMatchStatus(matchStatus, match),
                    ScoreHome = scoreHome,
                    ScoreAway = scoreAway,
                    QualifiedTeam = qualified,

                    HomeTeam = homeTeamName,
                    AwayTeam = awayTeamName,
                    StageName = stageName,

                    MatchType = "Regular90Min",
                    RecordStatus = "New",
                    HomeWinOdds = 1,
                    DrawOdds = 1,
                    AwayWinOdds = 1
                });
            }

            var tournamentName = competitionJson.GetProperty("name").GetString() ?? "Unnamed Tournament";
            var tournamentId = competitionJson.TryGetProperty("id", out var compIdProp) && compIdProp.ValueKind == JsonValueKind.Number
                ? compIdProp.GetInt32()
                : (int?)null;

            var stages = stageMap
                .Select((kv, index) => new PredefinedStageDto
                {
                    StageName = kv.Value,
                    Order = index + 1,
                    RecordStatus = "New"
                })
                .ToList();

            return new PredefinedTournamentDto
            {
                TournamentName = tournamentName,
                PublicTournamentName = tournamentName,
                ExternalTournamentId = tournamentId,
                Season = season,
                SeasonId = seasonId,
                TournamentStart = seasonStart,
                TournamentEnd = seasonEnd,
                IsActive = true,
                TournamentVisibility = "Private",
                UpdateMethod = "Auto",
                CreatedAt = DateTime.UtcNow,
                Teams = teams.Values.ToList(),
                Matches = matches,
                Stages = stages
            };
        }

        // Helpers
        private static string ToTitleCase(string input)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
        }

        private static string MapMatchStatus(string? rawStatus, JsonElement match)
        {
            var status = rawStatus?.ToUpperInvariant();

            // Check if fullTime scores are available
            bool hasFullTimeScores = false;
            if (match.TryGetProperty("score", out var scoreProp) &&
                scoreProp.TryGetProperty("fullTime", out var fullTimeProp) &&
                fullTimeProp.TryGetProperty("home", out var homeScore) &&
                fullTimeProp.TryGetProperty("away", out var awayScore) &&
                homeScore.ValueKind == JsonValueKind.Number &&
                awayScore.ValueKind == JsonValueKind.Number)
            {
                hasFullTimeScores = true;
            }

            return status switch
            {
                "SCHEDULED" => "Timed",
                "TIMED" => hasFullTimeScores ? "Finished" : "Timed",
                "IN_PLAY" => "In_Play",
                "PAUSED" => "In_Play",
                "FINISHED" => "Finished",
                "POSTPONED" => "Canceled",
                "SUSPENDED" => "Canceled",
                "CANCELLED" => "Canceled",
                "AWARDED" => "Finished",
                _ => hasFullTimeScores ? "Finished" : "Timed"
            };
        }
    }
}
