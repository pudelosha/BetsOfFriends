using Backend.DTOs;
using Backend.Model.Entities;
using System.Security.Claims;

namespace Backend.Repository.Interfaces
{
    public interface IUserService
    {
        string GetUserIdFromClaims(ClaimsPrincipal user);
        Task<ApplicationUser?> FindUserByEmailAsync(string email);
        Task<ApplicationUser?> FindUserByIdAsync(string userId);
        Task<UserProfileDto> GetUserProfileAsync(string userId);
        Task<bool> UpdateUserProfileAsync(string userId, UserProfileDto profile);
        Task<bool> SendPasswordResetEmailAsync(string email);
        Task<ResetPasswordResultDto> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<bool> ChangeUserEmailAsync(string userId, string newEmail, string password);
        Task<bool> UpdateUserPasswordAsync(string userId, string currentPassword, string newPassword);
        Task<bool> DeleteUserAccountAsync(string userId, string password);
        Task<List<ApplicationUserDto>> GetAllUsersAsync();
        Task<ActionResultDto> SuspendUserAsync(string targetUserId, string performedByUserId);
        Task<ActionResultDto> UnsuspendUserAsync(string targetUserId, string adminUserId);
        Task<ActionResultDto> DeleteUserAsync(string targetUserId, string performedByUserId);
    }
}
