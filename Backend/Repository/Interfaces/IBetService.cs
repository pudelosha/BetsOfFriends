using Backend.DTOs;

namespace Backend.Services.Interfaces
{
    public interface IBetService
    {
        Task CreateBetsForTournamentAsync(int tournamentId);
        Task<bool> UpdateBetAsync(int betId, string userId, BetUpdateDto betUpdateDto);
        Task<List<BetDto>> GetBetsByStatusAsync(int tournamentId, string userId, string status);
        Task<bool> CalculateBetsAsync(int tournamentId);
    }
}
