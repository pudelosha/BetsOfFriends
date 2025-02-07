using Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface IPredefinedTournamentService
    {
        Task<bool> CreatePredefinedTournamentAsync(PredefinedTournamentDto tournamentDto);
        Task<bool> UpdatePredefinedTournamentAsync(PredefinedTournamentDto tournamentDto);
        Task<List<PredefinedTournamentListDto>> GetAllPredefinedTournamentsAsync();
    }
}
