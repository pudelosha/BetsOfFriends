using Backend.DTOs;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text;

namespace Backend.Repository.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;

        public UserService(UserManager<ApplicationUser> userManager, IEmailService emailService, IConfiguration configuration, ILogger<UserService> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public string GetUserIdFromClaims(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public async Task<UserProfileDto> GetUserProfileAsync(string userId)
        {
            _logger.LogInformation($"Fetching profile for UserId: {userId}");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return null;
            }

            return new UserProfileDto
            {
                Username = user.UserName,
                Email = user.Email,
                MemberSince = user.MemberSince,
                Language = "English", //TODO user.Language,
                DarkMode = false //     user.DarkMode
            };
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, UserProfileDto profile)
        {
            _logger.LogInformation($"Updating profile for UserId: {userId}");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User not found for update: {userId}");
                return false;
            }

            user.UserName = profile.Username;
            //user.Language = profile.Language;
            //user.DarkMode = profile.DarkMode;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError($"Failed to update profile for UserId: {userId}");
                return false;
            }

            _logger.LogInformation($"User profile updated successfully for UserId: {userId}");
            return true;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            _logger.LogInformation($"Generating password reset token for email: {email}");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return false;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Encode the token safely in Base64 (avoids double encoding issues)
            var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            var frontendBaseUrl = environment == "Development"
                ? _configuration["App:ClientBaseUrlDev"]
                : _configuration["App:ClientBaseUrlProd"];

            var resetLink = $"{frontendBaseUrl}/reset-password?userId={user.Id}&token={encodedToken}";

            await _emailService.SendEmailAsync(user.Email, "Reset Your Password",
                $"Click <a href='{resetLink}'>here</a> to reset your password.");

            _logger.LogInformation($"Generated password reset email for user {user.Email}");

            return true;
        }

        public async Task<ResetPasswordResultDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            _logger.LogInformation($"Processing password reset for UserID: {request.UserId}");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning($"User not found: {request.UserId}");
                return new ResetPasswordResultDto { Success = false, Message = "Invalid user ID." };
            }

            var decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(request.Token));

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            if (!result.Succeeded)
            {
                _logger.LogError($"Password reset failed for UserID: {request.UserId}");
                return new ResetPasswordResultDto { Success = false, Errors = result.Errors };
            }

            _logger.LogInformation($"Password successfully reset for UserID: {request.UserId}");
            return new ResetPasswordResultDto { Success = true, Message = "Password updated successfully." };
        }
    }
}
