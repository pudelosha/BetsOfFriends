using Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface IUserService
    {
        Task<bool> SendPasswordResetEmailAsync(string email);
        Task<ResetPasswordResultDto> ResetPasswordAsync(ResetPasswordRequestDto request);
    }
}
