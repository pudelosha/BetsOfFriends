using Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface IPredefinedTournamentService
    {
        Task<bool> CreatePredefinedTournamentAsync(PredefinedTournamentDto tournamentDto);
        Task<bool> UpdatePredefinedTournamentAsync(PredefinedTournamentDto tournamentDto);
        Task<List<PredefinedTournamentListDto>> GetAllPredefinedTournamentsAsync();
        Task<List<PredefinedTournamentListDto>> GetActivePredefinedTournamentsAsync();
        Task<PredefinedTournamentDto?> GetPredefinedTournamentByIdAsync(int tournamentId);
        Task<bool> DeletePredefinedTournamentByIdAsync(int tournamentId);
        Task<bool> UpdatePredefinedTournamentStatusAsync(int tournamentId, bool isActive);
        Task<List<string>> GetTournamentStagesAsync(int tournamentId);
    }
}
