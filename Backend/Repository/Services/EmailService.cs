using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Repository.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILocalizationService _localizationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public EmailService(IConfiguration configuration, 
        ILogger<EmailService> logger, 
        IEmailTemplateService emailTemplateService,
        ILocalizationService localizationService,
        UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _logger = logger;
        _emailTemplateService = emailTemplateService;
        _localizationService = localizationService;
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
        var language = await GetUserLanguageByEmailAsync(email);

        var plainPlaceholders = new Dictionary<string, string>
        {
            { "TOURNAMENT_NAME", tournamentName }
        };
        var htmlPlaceholders = new Dictionary<string, string>
        {
            { "TOURNAMENT_NAME", WebUtility.HtmlEncode(tournamentName) }
        };

        var subject = _localizationService.Translate("Email.TournamentInvite.Title", language, plainPlaceholders);
        var emailBody = await BuildFramedEmailAsync(
            language,
            "Email.TournamentInvite.Title",
            "Email.TournamentInvite.Body",
            "Email.Action.AcceptInvitation",
            inviteLink,
            "Email.IgnoreUnexpected",
            htmlPlaceholders,
            includeActionLinkFallback: true);

        await SendEmailAsync(email, subject, emailBody);
    }

    public async Task SendAccountSetupEmailAsync(ApplicationUser user, string tournamentName)
    {
        // Step 1: Generate token and encode it
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = IdentityTokenUrlDecoder.Encode(token);

        // Step 2: Construct setup link
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        var frontendBaseUrl = environment == "Development"
            ? _configuration["App:ClientBaseUrlDev"]
            : _configuration["App:ClientBaseUrlProd"];

        var setupLink = $"{frontendBaseUrl}/setup-account?userId={user.Id}&token={encodedToken}";
        var language = await GetUserLanguageAsync(user);

        var plainPlaceholders = new Dictionary<string, string>
        {
            { "TOURNAMENT_NAME", tournamentName }
        };
        var htmlPlaceholders = new Dictionary<string, string>
        {
            { "TOURNAMENT_NAME", WebUtility.HtmlEncode(tournamentName) }
        };

        var subject = _localizationService.Translate("Email.AccountSetup.Title", language, plainPlaceholders);
        var emailBody = await BuildFramedEmailAsync(
            language,
            "Email.AccountSetup.Title",
            "Email.AccountSetup.Body",
            "Email.Action.SetupAccount",
            setupLink,
            "Email.AccountSetupSecondary",
            htmlPlaceholders,
            includeActionLinkFallback: true);

        await SendEmailAsync(user.Email, subject, emailBody);
    }

    public async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        _logger.LogInformation($"Generating email confirmation token for user: {user.Email}");

        // Generate and encode the token
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = IdentityTokenUrlDecoder.Encode(token);

        // Build confirmation URL
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        var frontendBaseUrl = environment == "Development"
            ? _configuration["App:ClientBaseUrlDev"]
            : _configuration["App:ClientBaseUrlProd"];

        var confirmationLink = $"{frontendBaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

        _logger.LogInformation($"Generated confirmation link for user {user.Email}: {confirmationLink}");

        var language = await GetUserLanguageAsync(user);
        var subject = _localizationService.Translate("Email.Confirm.Title", language);
        string emailBody = await BuildFramedEmailAsync(
            language,
            "Email.Confirm.Title",
            "Email.Confirm.Body",
            "Email.Action.ConfirmAccount",
            confirmationLink,
            "Email.ConfirmSecondary",
            includeActionLinkFallback: true);

        await SendEmailAsync(user.Email, subject, emailBody);
    }

    public async Task SendPasswordResetEmailAsync(ApplicationUser user)
    {
        _logger.LogInformation($"Generating password reset token for user: {user.Email}");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = IdentityTokenUrlDecoder.Encode(token);

        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        var frontendBaseUrl = environment == "Development"
            ? _configuration["App:ClientBaseUrlDev"]
            : _configuration["App:ClientBaseUrlProd"];

        var resetLink = $"{frontendBaseUrl}/reset-password?userId={user.Id}&token={encodedToken}";

        var language = await GetUserLanguageAsync(user);
        var subject = _localizationService.Translate("Email.PasswordReset.Title", language);
        string emailBody = await BuildFramedEmailAsync(
            language,
            "Email.PasswordReset.Title",
            "Email.PasswordReset.Body",
            "Email.Action.ResetPassword",
            resetLink,
            "Email.PasswordResetSecondary",
            includeActionLinkFallback: true);

        await SendEmailAsync(user.Email, subject, emailBody);

        _logger.LogInformation($"Password reset email sent to {user.Email}");
    }

    public async Task SendNotificationEmailAsync(ApplicationUser user, string title, string message, string route, string language)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("Notification email skipped because user {UserId} has no email address.", user.Id);
            return;
        }

        var notificationLink = BuildFrontendUrl(route);
        var placeholders = new Dictionary<string, string>
        {
            { "GREETING", _localizationService.Translate("Email.Greeting", language) },
            { "TITLE", WebUtility.HtmlEncode(title) },
            { "BODY_HTML", WebUtility.HtmlEncode(message).Replace("\n", "<br />") },
            { "ACTION_LINK", WebUtility.HtmlEncode(notificationLink) },
            { "ACTION_TEXT", WebUtility.HtmlEncode(_localizationService.Translate("Email.Action.OpenApp", language)) },
            { "SECONDARY_TEXT", WebUtility.HtmlEncode(_localizationService.Translate("Email.NotificationConsent", language)) },
            { "SIGNATURE", _localizationService.Translate("Email.Signature", language) }
        };

        var emailBody = await _emailTemplateService.GetEmailTemplateAsync("EmailFrame", placeholders);
        await SendEmailAsync(user.Email, title, emailBody);
    }

    private async Task<string> BuildFramedEmailAsync(
        string language,
        string titleKey,
        string bodyKey,
        string actionTextKey,
        string actionLink,
        string secondaryTextKey,
        IReadOnlyDictionary<string, string>? placeholders = null,
        bool includeActionLinkFallback = false)
    {
        var title = _localizationService.Translate(titleKey, language, placeholders);
        var bodyHtml = _localizationService.Translate(bodyKey, language, placeholders);
        var actionLinkFallbackHtml = includeActionLinkFallback
            ? _localizationService.Translate(
                "Email.ActionLinkFallback",
                language,
                new Dictionary<string, string>
                {
                    { "ACTION_LINK", WebUtility.HtmlEncode(actionLink) }
                })
            : string.Empty;

        var framePlaceholders = new Dictionary<string, string>
        {
            { "GREETING", _localizationService.Translate("Email.Greeting", language) },
            { "TITLE", title },
            { "BODY_HTML", bodyHtml },
            { "ACTION_LINK", WebUtility.HtmlEncode(actionLink) },
            { "ACTION_TEXT", WebUtility.HtmlEncode(_localizationService.Translate(actionTextKey, language)) },
            { "ACTION_LINK_FALLBACK_HTML", actionLinkFallbackHtml },
            { "SECONDARY_TEXT", WebUtility.HtmlEncode(_localizationService.Translate(secondaryTextKey, language)) },
            { "SIGNATURE", _localizationService.Translate("Email.Signature", language) }
        };

        return await _emailTemplateService.GetEmailTemplateAsync("EmailFrame", framePlaceholders);
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
        return BuildFrontendUrl("/my-tournaments");
    }

    private async Task<string> GetUserLanguageAsync(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.Language?.ShortName))
        {
            return user.Language.ShortName;
        }

        return await _userManager.Users
            .Where(u => u.Id == user.Id)
            .Select(u => u.Language != null ? u.Language.ShortName : "en")
            .FirstOrDefaultAsync() ?? "en";
    }

    private async Task<string> GetUserLanguageByEmailAsync(string email)
    {
        return await _userManager.Users
            .Where(u => u.Email == email)
            .Select(u => u.Language != null ? u.Language.ShortName : "en")
            .FirstOrDefaultAsync() ?? "en";
    }

    private string BuildFrontendUrl(string route)
    {
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        var frontendBaseUrl = environment == "Development"
            ? _configuration["App:ClientBaseUrlDev"]
            : _configuration["App:ClientBaseUrlProd"];

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            frontendBaseUrl = "https://app.betsoffriends.com";
        }

        if (string.IsNullOrWhiteSpace(route))
        {
            return frontendBaseUrl.TrimEnd('/');
        }

        return $"{frontendBaseUrl.TrimEnd('/')}/{route.TrimStart('/')}";
    }
}
