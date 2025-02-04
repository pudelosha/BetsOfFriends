using Backend.DTOs;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly IRegisterService _registerService;
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(IRegisterService registerService, ILogger<RegisterController> logger)
        {
            _registerService = registerService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user and sends a confirmation email.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserName))
                    return BadRequest(new RegisterResultDto { Success = false, Message = "Username is required." });

                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest(new RegisterResultDto { Success = false, Message = "Email is required." });

                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                    return BadRequest(new RegisterResultDto { Success = false, Message = "Password must be at least 8 characters long." });

                if (!request.Consent)
                    return BadRequest(new RegisterResultDto { Success = false, Message = "You must accept terms and conditions." });

                _logger.LogInformation($"Registering user: {request.Email}");

                var result = await _registerService.RegisterUserAsync(request.UserName, request.Email, request.Password);

                if (!result.Success)
                {
                    _logger.LogWarning($"Registration failed for {request.Email}: {result.Message}");
                    return BadRequest(result);
                }

                _logger.LogInformation($"User registered successfully: {request.Email}");

                return Ok(new RegisterResultDto { Success = true, Message = "Registration successful! Please check your email to confirm your account." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during registration for {request.Email}");
                return StatusCode(500, new RegisterResultDto { Success = false, Message = "An unexpected error occurred. Please try again later." });
            }
        }

        /// <summary>
        /// Confirms the user email after clicking the link.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            try
            {
                var result = await _registerService.ConfirmEmailAsync(userId, token);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(new RegisterResultDto { Success = true, Message = "Email confirmed successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during email confirmation.");
                return StatusCode(500, new RegisterResultDto { Success = false, Message = "An unexpected error occurred. Please try again later." });
            }
        }

        /// <summary>
        /// Resends the confirmation email if the user did not receive it.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationRequestDto request)
        {
            try
            {
                var result = await _registerService.ResendConfirmationEmailAsync(request.Email);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(new RegisterResultDto { Success = true, Message = "Confirmation email sent successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while resending confirmation email.");
                return StatusCode(500, new RegisterResultDto { Success = false, Message = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}
