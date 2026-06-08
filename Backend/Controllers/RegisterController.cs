using Backend.DTOs;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("[controller]")]
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
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto? request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new RegisterResultDto { Success = false, Message = "Invalid request data." });

                var email = request.Email?.Trim();
                var language = string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim();

                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new RegisterResultDto { Success = false, Message = "Email is required." });

                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                    return BadRequest(new RegisterResultDto { Success = false, Message = "Password must be at least 8 characters long." });

                if (!request.Consent)
                    return BadRequest(new RegisterResultDto { Success = false, Message = "You must accept terms and conditions." });

                _logger.LogInformation($"Registering user: {email}");

                var result = await _registerService.RegisterUserAsync(email, request.Password, language);

                if (!result.Success)
                {
                    _logger.LogWarning($"Registration failed for {email}: {result.Message}");
                    return BadRequest(result);
                }

                _logger.LogInformation($"User registered successfully: {email}");

                return Ok(new RegisterResultDto
                {
                    Success = true,
                    Message = result.Message ?? "Registration successful! Please check your email to confirm your account."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}", request?.Email);
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
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationRequestDto? request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest(new RegisterResultDto { Success = false, Message = "Email is required." });

                var result = await _registerService.ResendConfirmationEmailAsync(request.Email.Trim());

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

        /// <summary>
        /// Completes the account setup for invited users by setting a new password.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("setup-account")]
        public async Task<IActionResult> SetupAccount([FromBody] SetupAccountRequestDto? request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new RegisterResultDto { Success = false, Message = "Invalid request data." });

                if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest(new RegisterResultDto { Success = false, Message = "Invalid request data." });

                if (request.Password.Length < 8)
                    return BadRequest(new RegisterResultDto { Success = false, Message = "Password must be at least 8 characters long." });

                _logger.LogInformation($"Processing account setup for user: {request.UserId}");

                var language = string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim();
                var result = await _registerService.SetupAccountAsync(request.UserId, request.Token, request.Password, language);

                if (!result.Success)
                {
                    _logger.LogWarning($"Account setup failed for user {request.UserId}: {result.Message}");
                    return BadRequest(result);
                }

                _logger.LogInformation($"Account setup successful for user {request.UserId}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during account setup for user {UserId}", request?.UserId);
                return StatusCode(500, new RegisterResultDto
                {
                    Success = false,
                    Message = "An unexpected error occurred. Please try again later."
                });
            }
        }
    }
}
