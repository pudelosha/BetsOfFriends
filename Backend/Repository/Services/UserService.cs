using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ILocationService _locationService;
        private readonly AppDbContext _dbContext;

        public UserService(UserManager<ApplicationUser> userManager, IEmailService emailService, IConfiguration configuration, ILogger<UserService> logger, IEmailTemplateService emailTemplateService, ILocationService locationService, AppDbContext dbContext)
        {
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _locationService = locationService;
            _logger = logger;
            _emailTemplateService = emailTemplateService;
            _dbContext = dbContext;
        }

        public string GetUserIdFromClaims(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public async Task<ApplicationUser?> FindUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<ApplicationUser?> FindUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);

        }

        public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
        {
            _logger.LogInformation($"Fetching profile for UserId: {userId}");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return null;
            }

            var location = await _locationService.GetLocationByIdAsync(user.LocationId);

            return new UserProfileDto
            {
                Email = user.Email,
                MemberSince = user.MemberSince,
                Nickname = user.Nickname,
                Language = "English",       //TODO fixed
                DarkMode = false,           //TODO fixed
                Location = location != null
                    ? new LocationDto
                    {
                        CountryId = location.CountryId,
                        Name = location.Name
                    }
                    : null
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

            user.Nickname = profile.Nickname;
            //user.Language = profile.Language;
            //user.DarkMode = profile.DarkMode;
            user.LocationId = profile.Location?.CountryId;

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

            // Step 1: Prepare placeholders for the email template
            var placeholders = new Dictionary<string, string>
            {
                { "RESET_LINK", resetLink }
            };

            // Step 2: Retrieve the email template
            string emailBody = await _emailTemplateService.GetEmailTemplateAsync("PasswordReset", placeholders);

            // Step 3: Send the email
            await _emailService.SendEmailAsync(user.Email, "Reset Your Password", emailBody);

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

        public async Task<bool> ChangeUserEmailAsync(string userId, string newEmail, string password)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Validate password before allowing email change
            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                _logger.LogWarning($"Failed email change attempt due to incorrect password for user {userId}");
                return false;
            }

            user.Email = newEmail;
            user.NormalizedEmail = newEmail.ToUpper();

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> UpdateUserPasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"Password update failed: User {userId} not found.");
                return false;
            }

            if (!await _userManager.CheckPasswordAsync(user, currentPassword))
            {
                _logger.LogWarning($"Failed password update attempt: Incorrect current password for user {userId}");
                return false;
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                _logger.LogWarning($"Failed password update for user {userId}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return false;
            }

            _logger.LogInformation($"Password successfully updated for user {userId}");
            return true;
        }

        public async Task<bool> DeleteUserAccountAsync(string userId, string password)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"Account deletion failed: User {userId} not found.");
                return false;
            }

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                _logger.LogWarning($"Failed account deletion attempt: Incorrect password for user {userId}");
                return false;
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning($"Failed to delete user {userId}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return false;
            }

            _logger.LogInformation($"User {userId} successfully deleted.");
            return true;
        }

        public async Task<List<ApplicationUserDto>> GetAllUsersAsync()
        {
            var users = _userManager.Users.ToList();

            var result = new List<ApplicationUserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "Player";

                var dto = new ApplicationUserDto
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    UserEmail = user.Email,
                    UserRole = role,
                    UserStatus = GetUserStatus(user),
                    TournamentAdminCount = await _dbContext.CustomTournaments.CountAsync(t => t.CreatedByUserId == user.Id),
                    TournamentParticipantCount = await _dbContext.CustomTournamentUserAssignments.CountAsync(p => p.UserId == user.Id),
                    MemberSince = user.MemberSince // Or user.CreatedAt / user.RegisteredAt
                };

                result.Add(dto);
            }

            return result;
        }

        public async Task<ActionResultDto> SuspendUserAsync(string targetUserId, string adminUserId)
        {
            var user = await _userManager.FindByIdAsync(targetUserId);
            if (user == null)
                return ActionResultDto.ErrorResult("User not found.");

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? ActionResultDto.SuccessResult("User suspended successfully.")
                : ActionResultDto.ErrorResult("Failed to suspend user.");
        }

        public async Task<ActionResultDto> UnsuspendUserAsync(string targetUserId, string adminUserId)
        {
            var user = await _userManager.FindByIdAsync(targetUserId);
            if (user == null)
                return ActionResultDto.ErrorResult("User not found.");

            user.LockoutEnd = null;
            user.LockoutEnabled = false;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? ActionResultDto.SuccessResult("User unsuspended successfully.")
                : ActionResultDto.ErrorResult("Failed to unsuspend user.");
        }

        public async Task<ActionResultDto> DeleteUserAsync(string targetUserId, string adminUserId)
        {
            var user = await _userManager.FindByIdAsync(targetUserId);
            if (user == null)
                return ActionResultDto.ErrorResult("User not found.");

            // Prevent deletion of Super Admins
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin"))
                return ActionResultDto.ErrorResult("You cannot delete a Super Admin.");

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded
                ? ActionResultDto.SuccessResult("User deleted successfully.")
                : ActionResultDto.ErrorResult("Failed to delete user.");
        }
        private string GetUserStatus(ApplicationUser user)
        {
            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
                return "Suspended";

            if (!user.EmailConfirmed)
                return "Inactive";

            return "Active";
        }
    }
}
