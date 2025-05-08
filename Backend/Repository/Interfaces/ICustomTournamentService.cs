using Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface ICustomTournamentService
    {
        Task<TournamentCreationResultDto> CreateCustomTournamentAsync(CustomTournamentDto tournamentDto);
        Task<List<CustomTournamentListDto>> GetAllCustomTournamentsAsync(string userId);
        Task<bool?> UpdateCustomTournamentStatusAsync(int tournamentId, string userId, bool isActive);
        Task<bool?> DeleteCustomTournamentByIdAsync(int tournamentId, string userId);
        Task<TournamentUpdateResultDto> UpdateCustomTournamentAsync(CustomTournamentDto tournamentDto, string userId);
        Task<CustomTournamentDto?> GetCustomTournamentByIdAsync(int tournamentId, string userId);
        Task<List<UserActiveTournamentDto>> GetUserActiveTournamentsAsync(string userId);
        Task<bool> QuitTournamentAsync(int tournamentId, string userId);
        Task<bool?> ToggleTournamentVisibilityAsync(int tournamentId, string userId);
        Task<bool> RecalculateTournamentBetsAsync(int tournamentId, string userId);
        Task<List<TournamentSummaryDto>?> GetTournamentSummaryAsync(int tournamentId, string userId);
        Task<List<TournamentPlayerResultDto>> GetTournamentPlayerResultAsync(int tournamentId, string userId);
        Task<List<TournamentInviteDto>> GetPendingTournamentInvitesAsync(string userId);
        Task<List<string>> GetTournamentStagesAsync(int tournamentId, string userId);
        Task<string?> GetFirstStageWithPendingBetsAsync(int tournamentId, string userId);
        Task<string?> GetFirstStageWithUpcomingMatchesAsync(int tournamentId, string userId);
        Task<List<UserBettingStatsDto>> GetUserBettingStatsAsync(string userId, int tournamentId, string statsUserId);
        Task<bool> IsTournamentNameTakenAsync(string name, string visibility, string userId);
        Task<List<PublicTournamentDto>> GetPublicActiveTournamentsAsync(string userId);
        Task<List<TournamentParticipantDto>?> GetTournamentParticipantsAsync(int tournamentId, string userId, string status);
        Task<ActionResultDto> ExcludeParticipantAsync(int tournamentId, string requesterUserId, string targetUserEmail);
        Task<ActionResultDto> AcceptParticipantAsync(int tournamentId, string requesterUserId, string targetUserEmail);
        Task<ActionResultDto> ResendInviteAsync(int tournamentId, string requesterUserId, string targetUserEmail);
        Task<TournamentInvitationResponseDto> UpdateTournamentAssignmentAsync(int tournamentId, string userId, string newNickname);
        Task<TournamentAssignmentDto?> GetAssignmentDetailsAsync(int tournamentId, string userId);
        Task<TournamentInvitationResponseDto> AcceptTournamentInvitationAsync(int tournamentId, string userId, string nickname);
        Task<TournamentInvitationResponseDto> RequestToJoinTournamentAsync(string userId, int tournamentId, string nickname, string message);
        Task<CustomTournamentDto?> CheckForPendingUpdatesAsync(int tournamentId, string userId);
        Task<SelectedTournamentDetailsDto?> GetSelectedTournamentDetailsAsync(int tournamentId, string userId);
    }
}
