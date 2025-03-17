using Backend.DTOs.Backend.DTOs;
using Backend.Model.Entities;
using static Backend.Model.Entities.CustomMatch;

namespace Backend.Repository.Interfaces
{
    public interface IMatchService
    {
        Task<bool> UpdateMatchResultAsync(MatchResultUpdateDto matchUpdateDto, string userId);
        Task AutoUpdateMatchStatusAsync();
        Task<List<MatchDto>> GetMatchesByStatusAndStageAsync(int tournamentId, string userId, string status, string stage);
    }
}
