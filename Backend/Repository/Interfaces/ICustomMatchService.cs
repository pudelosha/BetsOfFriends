using Backend.DTOs.Backend.DTOs;
using Backend.Model.Entities;
using static Backend.Model.Entities.CustomMatch;

namespace Backend.Repository.Interfaces
{
    public interface ICustomMatchService
    {
        Task<bool> UpdateMatchResultAsync(MatchResultUpdateDto matchUpdateDto, string userId);
        Task<List<MatchDto>> GetMatchesByStatusAndStageAsync(int tournamentId, string userId, string status, string stage);
        Task<List<MatchDto>> GetStartedMatchesAsync(int tournamentId, string userId);
    }
}
