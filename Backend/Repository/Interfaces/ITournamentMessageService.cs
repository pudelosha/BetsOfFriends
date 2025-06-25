using Backend.DTOs;
using Backend.Model.Entities;

namespace Backend.Repository.Interfaces
{
    public interface ITournamentMessageService
    {
        Task<List<TournamentMessageDto>> GetLatestMessagesAsync(int tournamentId, string userId, int count);
        Task<CreateMessageResultDto> CreateMessageAsync(int tournamentId, string userId, string content);
    }
}
