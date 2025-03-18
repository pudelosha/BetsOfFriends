using Backend.DTOs;
using Backend.Model.Entities;

namespace Backend.Services.Interfaces
{
    public interface IBetService
    {
        Task CreateBetsForTournamentAsync(int tournamentId);
        Task<bool> UpdateBetAsync(int betId, string userId, BetUpdateDto betUpdateDto);
        Task<List<BetDto>> GetBetsByStatusAndStageAsync(int tournamentId, string userId, string status, string stage);
        Task AutoUpdateBetStatusAsync();
        Task GenerateBetsForNewMatchAsync(int matchId, int tournamentId);
        Task<BetStatsDto?> GetBetStatisticsAsync(int matchId, string userId);
        Task RecalculateBetsForMatchAsync(int matchId);
        Task<bool> RecalculateBetsForTournamentAsync(int tournamentId);
        Task<List<UpcomingBetDto>> GetUpcomingBetsAsync(int tournamentId, string userId, int? limit = null);
    }
}
