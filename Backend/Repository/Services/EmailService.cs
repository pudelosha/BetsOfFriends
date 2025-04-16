using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mail;
using System.Text;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly UserManager<ApplicationUser> _userManager;

    public EmailService(IConfiguration configuration, 
        ILogger<EmailService> logger, 
        IEmailTemplateService emailTemplateService,
        UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _logger = logger;
        _emailTemplateService = emailTemplateService;
        _userManager = userManager;
    }

    public async Task SendTournamentInvitationEmailAsync(string email, string tournamentName, int tournamentId)
    {
        // Step 1: Construct invite link
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        var frontendBaseUrl = environment == "Development"
            ? _configuration["App:ClientBaseUrlDev"]
            : _configuration["App:ClientBaseUrlProd"];

        var inviteLink = $"{frontendBaseUrl}/my-tournaments";

        // Step 2: Prepare email content
        var placeholders = new Dictionary<string, string>
        {
            { "TOURNAMENT_NAME", tournamentName },
            { "INVITE_LINK", inviteLink }
        };

        var emailBody = await _emailTemplateService.GetEmailTemplateAsync("TournamentInvite", placeholders);

        // Step 3: Send the email
        await SendEmailAsync(email, $"You're Invited to {tournamentName}!", emailBody);
    }

    public async Task SendAccountSetupEmailAsync(ApplicationUser user, string tournamentName)
    {
        // Step 1: Generate token and encode it
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

        // Step 2: Construct setup link
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        var frontendBaseUrl = environment == "Development"
            ? _configuration["App:ClientBaseUrlDev"]
            : _configuration["App:ClientBaseUrlProd"];

        var setupLink = $"{frontendBaseUrl}/setup-account?userId={user.Id}&token={encodedToken}";

        // Step 3: Prepare email content
        var placeholders = new Dictionary<string, string>
        {
            { "TOURNAMENT_NAME", tournamentName },
            { "SETUP_LINK", setupLink }
        };

        var emailBody = await _emailTemplateService.GetEmailTemplateAsync("AccountSetup", placeholders);

        // Step 4: Send the email
        await SendEmailAsync(user.Email, $"Set Up Your Account for {tournamentName}", emailBody);
    }

    public async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        _logger.LogInformation($"Generating email confirmation token for user: {user.Email}");

        // Generate and encode the token
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

        // Build confirmation URL
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        var frontendBaseUrl = environment == "Development"
            ? _configuration["App:ClientBaseUrlDev"]
            : _configuration["App:ClientBaseUrlProd"];

        var confirmationLink = $"{frontendBaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

        _logger.LogInformation($"Generated confirmation link for user {user.Email}: {confirmationLink}");

        // Prepare and send email
        var placeholders = new Dictionary<string, string>
        {
            { "CONFIRMATION_LINK", confirmationLink }
        };

        string emailBody = await _emailTemplateService.GetEmailTemplateAsync("ConfirmEmail", placeholders);

        await SendEmailAsync(user.Email, "Confirm Your Account", emailBody);
    }

    public async Task SendPasswordResetEmailAsync(ApplicationUser user)
    {
        _logger.LogInformation($"Generating password reset token for user: {user.Email}");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        var frontendBaseUrl = environment == "Development"
            ? _configuration["App:ClientBaseUrlDev"]
            : _configuration["App:ClientBaseUrlProd"];

        var resetLink = $"{frontendBaseUrl}/reset-password?userId={user.Id}&token={encodedToken}";

        var placeholders = new Dictionary<string, string>
        {
            { "RESET_LINK", resetLink }
        };

        string emailBody = await _emailTemplateService.GetEmailTemplateAsync("PasswordReset", placeholders);
        await SendEmailAsync(user.Email, "Reset Your Password", emailBody);

        _logger.LogInformation($"Password reset email sent to {user.Email}");
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpServer = _configuration["EmailSettings:SmtpServer"];
        var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
        var fromEmail = _configuration["EmailSettings:FromEmail"];

        if (string.IsNullOrEmpty(smtpServer))
        {
            _logger.LogError("SMTP Server is not configured.");
            throw new ArgumentNullException(nameof(smtpServer), "SMTP Server is required.");
        }

        if (string.IsNullOrEmpty(smtpPortStr) || !int.TryParse(smtpPortStr, out int smtpPort))
        {
            _logger.LogError("SMTP Port is not configured or invalid.");
            throw new ArgumentNullException(nameof(smtpPort), "SMTP Port is required.");
        }

        if (string.IsNullOrEmpty(fromEmail))
        {
            _logger.LogError("FromEmail is not configured.");
            throw new ArgumentNullException(nameof(fromEmail), "FromEmail is required.");
        }

        _logger.LogInformation($"Sending email from {fromEmail} to {to} via {smtpServer}:{smtpPort}");

        using (var client = new SmtpClient(smtpServer, smtpPort))
        {
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(
                _configuration["EmailSettings:SmtpUsername"],
                _configuration["EmailSettings:SmtpPassword"]
            );
            client.EnableSsl = false;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
        }
    }
    public string GenerateTournamentInviteLink(string email, int tournamentId)
    {
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        var frontendBaseUrl = environment == "Development"
            ? _configuration["App:ClientBaseUrlDev"]
            : _configuration["App:ClientBaseUrlProd"];

        return $"{frontendBaseUrl}/my-tournaments";
    }
}
