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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegisterService> _logger;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<RegisterService> logger,
            IEmailTemplateService emailTemplateService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
            _emailTemplateService = emailTemplateService;
        }

        public async Task<RegisterResultDto> RegisterUserAsync(string email, string password)
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

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var user = new ApplicationUser { UserName = email, Email = email };
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
                return new RegisterResultDto
                {
                    Success = false,
                    Message = "Failed to assign role to the user.",
                    Errors = roleResult.Errors
                };
            }

            _logger.LogInformation($"Assigned 'User' role to {email}.");

            // Commit the transaction before sending an email
            transaction.Complete();

            // Send confirmation email after transaction is complete
            var confirmationUrl = await GenerateEmailConfirmationLinkAsync(user);
            _logger.LogInformation($"Sending confirmation email to {email}");
            await SendConfirmationEmailAsync(user.Email, confirmationUrl);

            return new RegisterResultDto
            {
                Success = true,
                Message = "Registration successful. Please check your email to confirm your account."
            };
        }

        private async Task SendConfirmationEmailAsync(string email, string confirmationUrl)
        {
            var placeholders = new Dictionary<string, string>
        {
            { "CONFIRMATION_LINK", confirmationUrl }
        };

            string emailBody = await _emailTemplateService.GetEmailTemplateAsync("ConfirmEmail", placeholders);

            await _emailService.SendEmailAsync(email, "Confirm Your Account", emailBody);
        }

        public async Task<ApplicationUser?> RegisterInvitedUserAsync(string email)
        {
            _logger.LogInformation($"Registering invited user: {email}");

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                _logger.LogWarning($"User already exists: {email}. Skipping registration.");
                return existingUser; // Return existing user if they already have an account
            }

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

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
                return null; // Prevent further execution
            }

            _logger.LogInformation($"Assigned 'User' role to {email}.");

            // Commit the transaction before sending an email
            transaction.Complete();

            // Generate an email confirmation & password setup link
            var confirmationUrl = await GenerateAccountSetupLinkAsync(newUser);

            _logger.LogInformation($"Sending account setup email to {email}");
            await SendConfirmationEmailAsync(newUser.Email, confirmationUrl);

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

            await SendConfirmationEmailAsync(user.Email, confirmationUrl);

            return new RegisterResultDto { Success = true, Message = "Confirmation email sent successfully." };
        }

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

        private async Task<string> GenerateAccountSetupLinkAsync(ApplicationUser user)
        {
            _logger.LogInformation($"Generating account setup link for user: {user.Email}");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            var frontendBaseUrl = environment == "Development"
                ? _configuration["App:ClientBaseUrlDev"]
                : _configuration["App:ClientBaseUrlProd"];

            var setupLink = $"{frontendBaseUrl}/setup-account?userId={user.Id}&token={encodedToken}";

            _logger.LogInformation($"Generated account setup link for user {user.Email}: {setupLink}");

            return setupLink;
        }
    }
}
