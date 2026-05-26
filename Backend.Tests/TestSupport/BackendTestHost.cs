using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Repository.Services;
using Backend.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.TestSupport;

public sealed class BackendTestHost : IDisposable
{
    public const string ValidPassword = "ValidPassword123!";

    public BackendTestHost()
    {
        Emails = new TestEmailService();
        PushNotifications = new TestPushNotificationService();
        var databaseName = $"backend-tests-{Guid.NewGuid()}";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddDataProtection();
        services.AddSingleton(BuildConfiguration());
        services.AddDbContext<AppDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 3;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IEmailService>(Emails);
        services.AddSingleton<IPushNotificationService>(PushNotifications);
        services.AddSingleton<IEmailTemplateService, TestEmailTemplateService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IRegisterService, RegisterService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserService, UserService>();

        Services = services.BuildServiceProvider();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();
    }

    public ServiceProvider Services { get; }
    public TestEmailService Emails { get; }
    public TestPushNotificationService PushNotifications { get; }

    public IServiceScope CreateScope()
    {
        return Services.CreateScope();
    }

    public async Task<ApplicationUser> CreateUserAsync(
        string email,
        string password = ValidPassword,
        bool emailConfirmed = true,
        string? roleName = "User",
        int languageId = 1)
    {
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed,
            LanguageId = languageId
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {FormatErrors(createResult)}");
        }

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create test role: {FormatErrors(roleResult)}");
                }
            }

            var roleAssignmentResult = await userManager.AddToRoleAsync(user, roleName);
            if (!roleAssignmentResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to assign test role: {FormatErrors(roleAssignmentResult)}");
            }
        }

        return user;
    }

    public void Dispose()
    {
        Services.Dispose();
    }

    private static IConfiguration BuildConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "backend-tests-jwt-signing-key-with-more-than-32-chars",
            ["Jwt:Issuer"] = "Backend.Tests",
            ["Jwt:Audience"] = "Backend.Tests",
            ["App:FrontendBaseUrl"] = "https://frontend.test",
            ["EmailSettings:FromEmail"] = "tests@betsoffriends.local"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join(", ", result.Errors.Select(error => error.Description));
    }
}

public sealed class TestEmailService : IEmailService
{
    public List<ApplicationUser> ConfirmationEmails { get; } = new();
    public List<AccountSetupEmail> AccountSetupEmails { get; } = new();
    public List<TournamentInvitationEmail> TournamentInvitationEmails { get; } = new();
    public List<ApplicationUser> PasswordResetEmails { get; } = new();
    public List<NotificationEmail> NotificationEmails { get; } = new();

    public Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        ConfirmationEmails.Add(user);
        return Task.CompletedTask;
    }

    public Task SendAccountSetupEmailAsync(ApplicationUser user, string tournamentName)
    {
        AccountSetupEmails.Add(new AccountSetupEmail(user, tournamentName));
        return Task.CompletedTask;
    }

    public Task SendTournamentInvitationEmailAsync(string email, string tournamentName, int tournamentId)
    {
        TournamentInvitationEmails.Add(new TournamentInvitationEmail(email, tournamentName, tournamentId));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(ApplicationUser user)
    {
        PasswordResetEmails.Add(user);
        return Task.CompletedTask;
    }

    public Task SendNotificationEmailAsync(ApplicationUser user, string title, string message, string route, string language)
    {
        NotificationEmails.Add(new NotificationEmail(user, title, message, route, language));
        return Task.CompletedTask;
    }
}

public sealed record AccountSetupEmail(ApplicationUser User, string TournamentName);

public sealed record TournamentInvitationEmail(string Email, string TournamentName, int TournamentId);

public sealed record NotificationEmail(ApplicationUser User, string Title, string Message, string Route, string Language);

public sealed class TestEmailTemplateService : IEmailTemplateService
{
    public Task<string> GetEmailTemplateAsync(string templateName, Dictionary<string, string> placeholders)
    {
        return Task.FromResult($"{templateName}:{string.Join(",", placeholders.Select(p => $"{p.Key}={p.Value}"))}");
    }
}

public sealed class TestPushNotificationService : IPushNotificationService
{
    public bool IsConfigured { get; set; } = true;
    public bool NextSendResult { get; set; } = true;
    public List<PushNotificationMessage> SentPushNotifications { get; } = new();

    public string? GetPublicKey()
    {
        return IsConfigured ? "test-public-key" : null;
    }

    public Task UpsertSubscriptionAsync(string userId, PushSubscriptionDto subscription)
    {
        return Task.CompletedTask;
    }

    public Task RemoveSubscriptionAsync(string userId, PushSubscriptionDeleteDto subscription)
    {
        return Task.CompletedTask;
    }

    public Task<bool> SendPushAsync(string userId, string title, string message, string route)
    {
        SentPushNotifications.Add(new PushNotificationMessage(userId, title, message, route));
        return Task.FromResult(NextSendResult);
    }
}

public sealed record PushNotificationMessage(string UserId, string Title, string Message, string Route);
