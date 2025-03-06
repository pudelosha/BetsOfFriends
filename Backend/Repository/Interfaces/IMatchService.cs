using Backend.DTOs.Backend.DTOs;
using Backend.Model.Entities;
using static Backend.Model.Entities.CustomMatch;

namespace Backend.Repository.Interfaces
{
    public interface IMatchService
    {
        Task<List<MatchDto>> GetMatchesByStatusAsync(int tournamentId, string userId, MatchStatus status);
        Task<bool> UpdateMatchResultAsync(MatchResultUpdateDto matchUpdateDto, string userId);
        Task AutoUpdateMatchStatusAsync();


    }
}
