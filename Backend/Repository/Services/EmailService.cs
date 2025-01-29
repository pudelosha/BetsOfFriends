using Backend.Repository.Interfaces;
using System.Net;
using System.Net.Mail;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
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
}
