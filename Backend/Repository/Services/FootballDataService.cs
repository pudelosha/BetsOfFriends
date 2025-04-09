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

            // Extract season (e.g. 2024) from filters
            int? season = root.TryGetProperty("filters", out var filtersJson) &&
                          filtersJson.TryGetProperty("season", out var seasonProp) &&
                          seasonProp.ValueKind == JsonValueKind.Number
                          ? seasonProp.GetInt32()
                          : null;

            // Extract seasonId, start and end date from the first match
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

            // Always include placeholder
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

                // Register unique teams
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

                // Track stage
                var stageKey = $"{stageCode}_{matchday}";
                if (!stageMap.Any(kv => kv.Key == stageKey))
                    stageMap.Add(new KeyValuePair<string, string>(stageKey, stageName));

                // Scores
                int? scoreHome = null;
                int? scoreAway = null;

                if (match.TryGetProperty("score", out var scoreProp) &&
                    scoreProp.TryGetProperty("fullTime", out var fullTimeProp))
                {
                    if (fullTimeProp.TryGetProperty("home", out var homeScoreProp) && homeScoreProp.ValueKind == JsonValueKind.Number)
                        scoreHome = homeScoreProp.GetInt32();

                    if (fullTimeProp.TryGetProperty("away", out var awayScoreProp) && awayScoreProp.ValueKind == JsonValueKind.Number)
                        scoreAway = awayScoreProp.GetInt32();
                }

                matches.Add(new PredefinedMatchDto
                {
                    ExternalMatchId = externalMatchId,
                    MatchStart = utcDate,
                    MatchStatus = MapMatchStatus(matchStatus),
                    ScoreHome = scoreHome,
                    ScoreAway = scoreAway,

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

        // Helper
        private static string ToTitleCase(string input)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
        }

        private static string MapMatchStatus(string? rawStatus)
        {
            return rawStatus?.ToUpperInvariant() switch
            {
                "SCHEDULED" => "Timed",
                "TIMED" => "Timed",
                "IN_PLAY" => "In_Play",
                "PAUSED" => "In_Play",
                "FINISHED" => "Finished",
                "POSTPONED" => "Canceled",
                "SUSPENDED" => "Canceled",
                "CANCELED" => "Canceled",
                _ => "Timed" // fallback default
            };
        }
    }
}
