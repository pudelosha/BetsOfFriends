using Backend.DTOs;
using System.Security.Claims;

namespace Backend.Repository.Interfaces
{
    public interface IUserService
    {
        string GetUserIdFromClaims(ClaimsPrincipal user);
        Task<UserProfileDto> GetUserProfileAsync(string userId);
        Task<bool> UpdateUserProfileAsync(string userId, UserProfileDto profile);
        Task<bool> SendPasswordResetEmailAsync(string email);
        Task<ResetPasswordResultDto> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<bool> ChangeUserEmailAsync(string userId, string newEmail, string password);
        Task<bool> UpdateUserPasswordAsync(string userId, string currentPassword, string newPassword);
    }
}
