using Backend.DTOs;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Text;
using System.Transactions;

namespace Backend.Repository.Services
{
    public class RegisterService : IRegisterService
    {
        private const string DefaultLanguageCode = "en";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegisterService> _logger;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILanguageService _languageService;

        public RegisterService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<RegisterService> logger,
            IEmailTemplateService emailTemplateService,
            ILanguageService languageService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
            _emailTemplateService = emailTemplateService;
            _languageService = languageService;
        }

        public async Task<RegisterResultDto> RegisterUserAsync(string email, string password, string language)
        {
            email = NormalizeEmail(email);
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

            var languageEntity = await GetLanguageOrDefaultAsync(language);
            if (languageEntity == null)
            {
                _logger.LogError("Default language {DefaultLanguageCode} is not configured.", DefaultLanguageCode);
                return new RegisterResultDto
                {
                    Success = false,
                    Message = "Registration is temporarily unavailable. Please try again later.",
                    Errors = new List<IdentityError> { new IdentityError { Description = "Default language is not configured." } }
                };
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                LanguageId = languageEntity.LanguageId
            };
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                _logger.LogWarning($"User registration failed for email: {email}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");

                return new RegisterResultDto
                {
                    Success = false,
                    Message = "Registration failed due to validation errors.",
                    Errors = result.Errors
                };
            }

            _logger.LogInformation($"User registered successfully: {email}");

            // Ensure the "User" role exists
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                _logger.LogInformation("Creating 'User' role since it does not exist.");
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Assign the "User" role to the newly registered user
            var roleResult = await _userManager.AddToRoleAsync(user, "User");
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning($"Failed to assign 'User' role to {email}. Errors: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
            else
            {
                _logger.LogInformation($"Assigned 'User' role to {email}.");
            }

            _logger.LogInformation($"Sending confirmation email to {email}");
            try
            {
                await _emailService.SendConfirmationEmailAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {Email} was registered, but confirmation email could not be sent.", email);
                return new RegisterResultDto
                {
                    Success = true,
                    Message = "Registration successful, but the confirmation email could not be sent. Please use resend activation email from the login page."
                };
            }

            return new RegisterResultDto
            {
                Success = true,
                Message = "Registration successful. Please check your email to confirm your account."
            };
        }

        public async Task<ApplicationUser?> RegisterInvitedUserAsync(string email)
        {
            email = NormalizeEmail(email);
            _logger.LogInformation($"Registering invited user: {email}");

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                _logger.LogWarning($"User already exists: {email}. Skipping registration.");
                return existingUser; // Return existing user if they already have an account
            }

            var newUser = new ApplicationUser
            {
                Email = email,
                UserName = email,
                EmailConfirmed = false // User must confirm their email first
            };

            // Create user without password (password will be set later)
            var result = await _userManager.CreateAsync(newUser);
            if (!result.Succeeded)
            {
                _logger.LogError($"Failed to create invited user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return null;
            }

            _logger.LogInformation($"User {email} registered successfully without a password.");

            // Ensure the "User" role exists
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                _logger.LogInformation("Creating 'User' role since it does not exist.");
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Assign the "User" role to the invited user
            var roleResult = await _userManager.AddToRoleAsync(newUser, "User");
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning($"Failed to assign 'User' role to {email}. Errors: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
            else
            {
                _logger.LogInformation($"Assigned 'User' role to {email}.");
            }

            return newUser;
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

            var decodedToken = Uri.UnescapeDataString(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning($"Email confirmation failed for user ID {userId}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return new RegisterResultDto { Success = false, Message = "Invalid or expired confirmation token.", Errors = result.Errors };
            }

            _logger.LogInformation($"Email confirmed successfully for user ID: {userId}");

            return new RegisterResultDto { Success = true, Message = "Email confirmed successfully." };
        }

        public async Task<RegisterResultDto> ResendConfirmationEmailAsync(string email)
        {
            email = NormalizeEmail(email);
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

            _logger.LogInformation($"Sending new confirmation email to {email}");

            try
            {
                await _emailService.SendConfirmationEmailAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Confirmation email could not be resent to {Email}.", email);
                return new RegisterResultDto { Success = false, Message = "Could not send confirmation email. Please try again later." };
            }

            return new RegisterResultDto { Success = true, Message = "Confirmation email sent successfully." };
        }

        public async Task<RegisterResultDto> SetupAccountAsync(string userId, string token, string password, string language)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new RegisterResultDto { Success = false, Message = "User not found." };
            }

            var decodedToken = Uri.UnescapeDataString(token);
            var resetResult = await _userManager.ResetPasswordAsync(user, decodedToken, password);

            if (!resetResult.Succeeded)
            {
                return new RegisterResultDto
                {
                    Success = false,
                    Message = "Failed to set password.",
                    Errors = resetResult.Errors
                };
            }

            var languageEntity = await GetLanguageOrDefaultAsync(language);
            if (languageEntity == null)
            {
                return new RegisterResultDto
                {
                    Success = false,
                    Message = "Account setup is temporarily unavailable. Please try again later.",
                    Errors = new List<IdentityError> { new IdentityError { Description = "Default language is not configured." } }
                };
            }

            user.EmailConfirmed = true;
            user.LanguageId = languageEntity.LanguageId;

            await _userManager.UpdateAsync(user);

            return new RegisterResultDto { Success = true, Message = "Account setup completed successfully!" };
        }

        private async Task<Language?> GetLanguageOrDefaultAsync(string? language)
        {
            var languageCode = NormalizeLanguageCode(language);
            var languageEntity = await _languageService.GetByShortNameAsync(languageCode);
            if (languageEntity != null)
            {
                return languageEntity;
            }

            _logger.LogWarning("Unsupported language code '{LanguageCode}' was provided. Falling back to {DefaultLanguageCode}.", languageCode, DefaultLanguageCode);
            return await _languageService.GetByShortNameAsync(DefaultLanguageCode);
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static string NormalizeLanguageCode(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return DefaultLanguageCode;
            }

            var normalized = language.Trim().ToLowerInvariant();
            var separatorIndex = normalized.IndexOfAny(new[] { '-', '_' });
            return separatorIndex > 0 ? normalized[..separatorIndex] : normalized;
        }
    }
}
