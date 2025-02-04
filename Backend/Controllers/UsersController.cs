using Backend.DTOs;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Sends an email to reset a password.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            bool emailSent = await _userService.SendPasswordResetEmailAsync(request.Email);

            return Ok(new { message = "If your email exists in our system, a reset link has been sent." });
        }

        /// <summary>
        /// Resets a user's password using a valid reset token.
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResetPasswordResultDto { Success = false, Message = "Invalid request data." });

            _logger.LogInformation($"Password reset attempt for user: {request.UserId}");

            var result = await _userService.ResetPasswordAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Get user profile details
        /// </summary>
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt to user profile.");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var profile = await _userService.GetUserProfileAsync(userId);
                if (profile == null)
                {
                    _logger.LogWarning($"User profile not found for UserId: {userId}");
                    return NotFound(new { message = "User not found" });
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user profile");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        /// <summary>
        /// Update user profile details
        /// </summary>
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileDto profile)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized profile update attempt.");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var success = await _userService.UpdateUserProfileAsync(userId, profile);
                if (!success)
                {
                    _logger.LogWarning($"Failed to update profile for UserId: {userId}");
                    return BadRequest(new { message = "Failed to update profile" });
                }

                return Ok(new { message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        /// <summary>
        /// Change user email upon providing valid password
        /// </summary>
        [Authorize]
        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequestDto request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "User not authenticated" });

                var success = await _userService.ChangeUserEmailAsync(userId, request.NewEmail, request.Password);
                return success ? Ok(new { message = "Email updated successfully" }) : BadRequest(new { message = "Failed to update email. Check your password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user email");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        [Authorize]
        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequestDto request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var success = await _userService.UpdateUserPasswordAsync(userId, request.CurrentPassword, request.NewPassword);
                return success
                    ? Ok(new { message = "Password updated successfully." })
                    : BadRequest(new { message = "Failed to update password. Check your current password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [Authorize]
        [HttpPost("delete-account")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequestDto request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var success = await _userService.DeleteUserAccountAsync(userId, request.Password);
                return success
                    ? Ok(new { message = "Account deleted successfully." })
                    : BadRequest(new { message = "Failed to delete account. Check your password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user account");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }
    }
}
