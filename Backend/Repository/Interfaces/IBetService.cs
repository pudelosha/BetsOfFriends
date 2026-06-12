using Backend.DTOs;

namespace Backend.Services.Interfaces
{
    public interface IBetService
    {
        Task CreateBetsForTournamentAsync(int tournamentId);
        Task<bool> UpdateBetAsync(int betId, string userId, BetUpdateDto betUpdateDto);
        Task<List<BetDto>> GetBetsByStatusAndStageAsync(int tournamentId, string userId, string status, string stage);
        Task GenerateBetsForNewMatchAsync(int matchId, int tournamentId);
        Task<BetStatsDto?> GetBetStatisticsAsync(int matchId, string userId);
        Task<PendingBetReminderSummaryDto?> GetPendingBetReminderParticipantsAsync(int matchId, string userId);
        Task<SendPendingBetReminderResultDto> SendPendingBetReminderAsync(int matchId, string userId, SendPendingBetReminderRequestDto request);
        Task RecalculateBetsForMatchAsync(int matchId);
        Task<bool> RecalculateBetsForTournamentAsync(int tournamentId);
        Task<List<UpcomingBetDto>> GetUpcomingBetsAsync(int tournamentId, string userId, int? limit = null);
        Task<List<BetDto>> GetInProgressBetsAsync(int tournamentId, string userId, int? limit = null);
        Task<MissingBetsSummaryDto?> GetMissingBetsSummaryAsync(int tournamentId, string userId, int matchLimit = 5, int hoursAhead = 48);
        Task MarkBetsAsCompletedForMatchAsync(int matchId);
    }
}
