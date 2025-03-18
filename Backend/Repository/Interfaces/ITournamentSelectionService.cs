namespace Backend.Repository.Interfaces
{
    public interface ITournamentSelectionService
    {
        Task<bool> SetSelectedTournamentAsync(string userId, int tournamentId);
        Task<int?> GetSelectedTournamentAsync(string userId);
    }
}
