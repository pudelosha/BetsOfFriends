namespace Backend.Repository.Interfaces
{
    public interface IBetService
    {
        Task CreateBetsForTournamentAsync(int tournamentId);

    }
}
