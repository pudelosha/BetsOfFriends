using Backend.DTOs;
using Backend.Model.Entities;

namespace Backend.Repository.Interfaces
{
    public interface IRegisterService
    {
        Task<RegisterResultDto> RegisterUserAsync(string email, string password);
        Task<ApplicationUser?> RegisterInvitedUserAsync(string email);
        Task<RegisterResultDto> ConfirmEmailAsync(string userId, string token);
        Task<RegisterResultDto> ResendConfirmationEmailAsync(string email);
    }
}
