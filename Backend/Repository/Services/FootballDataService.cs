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

            var matches = new List<PredefinedMatchDto>();
            var teams = new Dictionary<string, PredefinedTeamDto>(StringComparer.OrdinalIgnoreCase);
            var stageMap = new Dictionary<int, string>(); // matchday -> stage name

            const string placeholderTeamName = "TBD";

            // Ensure placeholder is always added
            teams[placeholderTeamName] = new PredefinedTeamDto
            {
                TeamName = placeholderTeamName,
                RecordStatus = "New"
            };

            foreach (var match in matchesJson.EnumerateArray())
            {
                var homeTeam = match.GetProperty("homeTeam").GetProperty("name").GetString() ?? placeholderTeamName;
                var awayTeam = match.GetProperty("awayTeam").GetProperty("name").GetString() ?? placeholderTeamName;
                var utcDate = match.GetProperty("utcDate").GetDateTime();
                var matchday = match.TryGetProperty("matchday", out var mdProp) && mdProp.ValueKind == JsonValueKind.Number
                    ? mdProp.GetInt32()
                    : 0;

                var stageName = matchday > 0 ? $"Matchday {matchday}" : match.GetProperty("stage").GetString() ?? "Unknown Stage";

                // Register unique teams if not already in dictionary
                if (!teams.ContainsKey(homeTeam))
                {
                    teams[homeTeam] = new PredefinedTeamDto
                    {
                        TeamName = homeTeam,
                        RecordStatus = "New"
                    };
                }

                if (!teams.ContainsKey(awayTeam))
                {
                    teams[awayTeam] = new PredefinedTeamDto
                    {
                        TeamName = awayTeam,
                        RecordStatus = "New"
                    };
                }

                // Track stage
                if (!stageMap.ContainsKey(matchday))
                    stageMap[matchday] = stageName;

                matches.Add(new PredefinedMatchDto
                {
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    MatchStart = utcDate,
                    StageName = stageName,
                    MatchType = "Regular90Min",
                    RecordStatus = "New",
                    HomeWinOdds = 1,
                    DrawOdds = 1,
                    AwayWinOdds = 1
                });
            }

            var tournamentName = root.GetProperty("competition").GetProperty("name").GetString() ?? "Unnamed Tournament";

            var stages = stageMap
                .OrderBy(kv => kv.Key)
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
                IsActive = true,
                TournamentVisibility = "Private",
                UpdateMethod = "Auto",
                CreatedAt = DateTime.UtcNow,
                Teams = teams.Values.ToList(),
                Matches = matches,
                Stages = stages
            };
        }
    }
}
