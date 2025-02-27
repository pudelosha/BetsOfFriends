using Backend.DTOs;
using Backend.Model.Entities;

namespace Backend.Repository.Interfaces
{
    public interface IRegisterService
    {
        Task<RegisterResultDto> RegisterUserAsync(string userName, string email, string password);
        Task<ApplicationUser?> RegisterInvitedUserAsync(string email, string userName);
        Task<RegisterResultDto> ConfirmEmailAsync(string userId, string token);
        Task<RegisterResultDto> ResendConfirmationEmailAsync(string email);
    }
}
