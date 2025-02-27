using Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface ICustomTournamentService
    {
        Task<bool> CreateCustomTournamentAsync(CustomTournamentDto tournamentDto);
        Task<List<CustomTournamentListDto>> GetAllCustomTournamentsAsync();
        Task<bool> UpdateCustomTournamentStatusAsync(int tournamentId, bool isActive);



    }
}
