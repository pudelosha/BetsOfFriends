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


        //get user profile
        //change user password
        //get users
        //delete user profile
        //update user profile
        //change user email
    }
}
