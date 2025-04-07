using Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface IFootballDataService
    {
        Task<string> GetCompetitionMatchesAsync(int competitionCode, int seasonCode);
        Task<PredefinedTournamentDto> ConvertToPredefinedTournamentDtoAsync(string json);
    }
}
