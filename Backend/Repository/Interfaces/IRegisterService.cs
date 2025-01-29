using Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface IRegisterService
    {
        Task<RegisterResultDto> RegisterUserAsync(string userName, string email, string password);
        Task<RegisterResultDto> ConfirmEmailAsync(string userId, string token);
        Task<RegisterResultDto> ResendConfirmationEmailAsync(string email);
    }
}
