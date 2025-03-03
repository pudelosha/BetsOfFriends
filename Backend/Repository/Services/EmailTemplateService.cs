using Backend.Repository.Interfaces;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IConfiguration _configuration;

    public EmailTemplateService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> GetEmailTemplateAsync(string templateName, Dictionary<string, string> placeholders)
    {
        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Emails", $"{templateName}.html");

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Email template '{templateName}' not found at {templatePath}");
        }

        string template = await File.ReadAllTextAsync(templatePath);

        foreach (var placeholder in placeholders)
        {
            template = template.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
        }

        return template;
    }
}
