using Backend.Model.Entities;

namespace Backend.Repository.Interfaces
{
    public interface IEmailService
    {
        Task SendConfirmationEmailAsync(ApplicationUser user);
        Task SendAccountSetupEmailAsync(ApplicationUser user, string tournamentName);
        Task SendTournamentInvitationEmailAsync(string email, string tournamentName, int tournamentId);
        Task SendPasswordResetEmailAsync(ApplicationUser user);
    }
}
