using Backend.DTOs;
using Backend.Model.Entities;

namespace Backend.Services.Interfaces
{
    public interface IBetService
    {
        Task CreateBetsForTournamentAsync(int tournamentId);
        Task<bool> UpdateBetAsync(int betId, string userId, BetUpdateDto betUpdateDto);
        Task<List<BetDto>> GetBetsByStatusAsync(int tournamentId, string userId, Bet.BetStatus status);
        Task<bool> CalculateBetsAsync(int tournamentId);
        Task AutoUpdateBetStatusAsync();
        Task GenerateBetsForNewMatchAsync(int matchId, int tournamentId);
        Task<BetStatsDto?> GetBetStatisticsAsync(int matchId);
    }
}
