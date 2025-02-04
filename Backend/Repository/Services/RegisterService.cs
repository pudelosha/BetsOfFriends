using Backend.DTOs;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Text;

namespace Backend.Repository.Services
{
    public class RegisterService : IRegisterService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegisterService> _logger;

        public RegisterService(UserManager<ApplicationUser> userManager, IEmailService emailService, IConfiguration configuration, ILogger<RegisterService> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RegisterResultDto> RegisterUserAsync(string userName, string email, string password)
        {
            _logger.LogInformation($"Attempting to register user with email: {email}");

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                _logger.LogWarning($"User registration failed: Email {email} is already in use.");
                return new RegisterResultDto
                {
                    Success = false,
                    Message = "Email is already in use.",
                    Errors = new List<IdentityError> { new IdentityError { Description = "Email is already in use." } }
                };
            }

            var user = new ApplicationUser { UserName = userName, Email = email };
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errorMessages = result.Errors.Select(e => e.Description);
                _logger.LogWarning($"User registration failed for email: {email}. Errors: {string.Join(", ", errorMessages)}");

                return new RegisterResultDto
                {
                    Success = false,
                    Message = "Registration failed due to validation errors.",
                    Errors = result.Errors
                };
            }

            _logger.LogInformation($"User registered successfully: {email}");

            var confirmationUrl = await GenerateEmailConfirmationLinkAsync(user);
            _logger.LogInformation($"Sending confirmation email to {email}");

            await _emailService.SendEmailAsync(user.Email, "Confirm Your Account",
                $"Click <a href='{confirmationUrl}'>here</a> to confirm your account.");

            return new RegisterResultDto
            {
                Success = true,
                Message = "Registration successful. Please check your email to confirm your account."
            };
        }

        public async Task<RegisterResultDto> ConfirmEmailAsync(string userId, string token)
        {
            _logger.LogInformation($"Attempting email confirmation for user ID: {userId}");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"Email confirmation failed: User not found with ID {userId}");
                return new RegisterResultDto { Success = false, Message = "Invalid user ID." };
            }

            var decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(token));

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning($"Email confirmation failed for user ID {userId}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return new RegisterResultDto { Success = false, Errors = result.Errors };
            }

            _logger.LogInformation($"Email confirmed successfully for user ID: {userId}");

            return new RegisterResultDto { Success = true, Message = "Email confirmed successfully." };
        }

        public async Task<RegisterResultDto> ResendConfirmationEmailAsync(string email)
        {
            _logger.LogInformation($"Resending confirmation email to {email}");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning($"Resend confirmation email failed: User not found for email {email}");
                return new RegisterResultDto { Success = false, Message = "User not found." };
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                _logger.LogWarning($"Resend confirmation email failed: Email already confirmed for user {email}");
                return new RegisterResultDto { Success = false, Message = "Email already confirmed." };
            }

            var confirmationUrl = await GenerateEmailConfirmationLinkAsync(user);

            _logger.LogInformation($"Sending new confirmation email to {email}");

            await _emailService.SendEmailAsync(user.Email, "Confirm Your Account",
                $"Click <a href='{confirmationUrl}'>here</a> to confirm your account.");

            return new RegisterResultDto { Success = true, Message = "Confirmation email sent successfully." };
        }

        /// <summary>
        /// Generates an email confirmation link for a user.
        /// </summary>
        private async Task<string> GenerateEmailConfirmationLinkAsync(ApplicationUser user)
        {
            _logger.LogInformation($"Generating email confirmation token for user: {user.Email}");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Encode the token safely in Base64 (avoids double encoding issues)
            var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

            // Determine the correct backend URL based on environment
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            var frontendBaseUrl = environment == "Development"
                ? _configuration["App:ClientBaseUrlDev"]
                : _configuration["App:ClientBaseUrlProd"];

            var confirmationLink = $"{frontendBaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

            _logger.LogInformation($"Generated confirmation link for user {user.Email}: {confirmationLink}");

            return confirmationLink;
        }
    }
}
