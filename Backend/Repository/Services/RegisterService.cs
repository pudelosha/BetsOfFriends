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

        public RegisterService(UserManager<ApplicationUser> userManager, IEmailService emailService, IConfiguration configuration)
        {
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<RegisterResultDto> RegisterUserAsync(string userName, string email, string password)
        {
            var user = new ApplicationUser { UserName = userName, Email = email };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return new RegisterResultDto { Success = false, Errors = result.Errors };
            }

            var confirmationUrl = await GenerateEmailConfirmationLinkAsync(user);

            await _emailService.SendEmailAsync(user.Email, "Confirm Your Account",
                $"Click <a href='{confirmationUrl}'>here</a> to confirm your account.");

            return new RegisterResultDto { Success = true };
        }

        public async Task<RegisterResultDto> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new RegisterResultDto { Success = false, Message = "Invalid user ID." };

            var decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(token));

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            return result.Succeeded
                ? new RegisterResultDto { Success = true, Message = "Email confirmed successfully." }
                : new RegisterResultDto { Success = false, Errors = result.Errors };
        }

        public async Task<RegisterResultDto> ResendConfirmationEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new RegisterResultDto { Success = false, Message = "User not found." };

            if (await _userManager.IsEmailConfirmedAsync(user))
                return new RegisterResultDto { Success = false, Message = "Email already confirmed." };

            var confirmationUrl = await GenerateEmailConfirmationLinkAsync(user);

            await _emailService.SendEmailAsync(user.Email, "Confirm Your Account",
                $"Click <a href='{confirmationUrl}'>here</a> to confirm your account.");

            return new RegisterResultDto { Success = true, Message = "Confirmation email sent successfully." };
        }

        /// <summary>
        /// Generates an email confirmation link for a user.
        /// </summary>
        private async Task<string> GenerateEmailConfirmationLinkAsync(ApplicationUser user)
        {
            // Generate the email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Encode the token safely in Base64 (avoids double encoding issues)
            var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

            // Determine the correct backend URL based on environment
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            var backendBaseUrl = environment == "Development"
                ? _configuration["App:BackendBaseUrlDev"]
                : _configuration["App:BackendBaseUrlProd"];

            // Generate the final confirmation URL
            return $"{backendBaseUrl}/api/register/confirm-email?userId={user.Id}&token={encodedToken}";
        }
    }
}
