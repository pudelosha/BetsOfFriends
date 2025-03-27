using Backend.DTOs.Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface IPredefinedMatchService
    {
        Task<List<MatchDto>> GetMatchesByStatusAndStageAsync(int tournamentId, string status, string stage);
        Task<bool> UpdateMatchResultAsync(MatchResultUpdateDto matchUpdateDto);
        Task<List<MatchDto>> GetStartedMatchesAsync();
    }
}
