using System.Net;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Services;

public class RegisterServiceTests
{
    private const int UrlTokenStressIterations = 100;

    [Fact]
    public async Task RegisterUserAsync_WithValidData_CreatesUserAssignsRoleAndSendsConfirmationEmail()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();

        var result = await service.RegisterUserAsync(
            "new-user@example.com",
            BackendTestHost.ValidPassword,
            "pl");

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("new-user@example.com");

        Assert.True(result.Success);
        Assert.Equal("Registration successful. Please check your email to confirm your account.", result.Message);
        Assert.NotNull(user);
        Assert.False(user!.EmailConfirmed);
        Assert.Equal(2, user.LanguageId);
        Assert.True(await userManager.IsInRoleAsync(user, "User"));
        Assert.Single(host.Emails.ConfirmationEmails);
        Assert.Equal(user.Id, host.Emails.ConfirmationEmails[0].Id);
    }

    [Fact]
    public async Task RegisterUserAsync_WithExistingEmail_ReturnsDuplicateEmailError()
    {
        using var host = new BackendTestHost();
        await host.CreateUserAsync("duplicate@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();

        var result = await service.RegisterUserAsync(
            "duplicate@example.com",
            BackendTestHost.ValidPassword,
            "en");

        Assert.False(result.Success);
        Assert.Equal("Email is already in use.", result.Message);
        Assert.Empty(host.Emails.ConfirmationEmails);
    }

    [Fact]
    public async Task RegisterUserAsync_WithUnknownLanguage_FallsBackToEnglish()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();

        var result = await service.RegisterUserAsync(
            "bad-language@example.com",
            BackendTestHost.ValidPassword,
            "xx");

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("bad-language@example.com");

        Assert.True(result.Success);
        Assert.Equal("Registration successful. Please check your email to confirm your account.", result.Message);
        Assert.NotNull(user);
        Assert.Equal(1, user!.LanguageId);
        Assert.Single(host.Emails.ConfirmationEmails);
    }

    [Fact]
    public async Task RegisterUserAsync_WhenConfirmationEmailFails_ReturnsSuccessWithResendMessage()
    {
        using var host = new BackendTestHost(services =>
        {
            services.AddSingleton<IEmailService, ThrowingConfirmationEmailService>();
        });

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();

        var result = await service.RegisterUserAsync(
            "email-failure@example.com",
            BackendTestHost.ValidPassword,
            "en");

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("email-failure@example.com");

        Assert.True(result.Success);
        Assert.Equal(
            "Registration successful, but the confirmation email could not be sent. Please use resend activation email from the login page.",
            result.Message);
        Assert.NotNull(user);
        Assert.False(user!.EmailConfirmed);
    }

    [Fact]
    public async Task RegisterInvitedUserAsync_WhenUserDoesNotExist_CreatesPasswordlessUserWithUserRole()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await service.RegisterInvitedUserAsync("invited@example.com");

        Assert.NotNull(user);
        Assert.Equal("invited@example.com", user!.Email);
        Assert.False(user.EmailConfirmed);
        Assert.False(await userManager.HasPasswordAsync(user));
        Assert.True(await userManager.IsInRoleAsync(user, "User"));
    }

    [Fact]
    public async Task RegisterInvitedUserAsync_WhenUserExists_ReturnsExistingUser()
    {
        using var host = new BackendTestHost();
        var existingUser = await host.CreateUserAsync("already-invited@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();

        var result = await service.RegisterInvitedUserAsync("already-invited@example.com");

        Assert.NotNull(result);
        Assert.Equal(existingUser.Id, result!.Id);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithValidEncodedToken_ConfirmsEmail()
    {
        using var host = new BackendTestHost();
        var createdUser = await host.CreateUserAsync("confirm@example.com", emailConfirmed: false);

        using var scope = host.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();
        var user = await userManager.FindByIdAsync(createdUser.Id);
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);

        var result = await service.ConfirmEmailAsync(user!.Id, Uri.EscapeDataString(token));
        var refreshedUser = await userManager.FindByIdAsync(user.Id);

        Assert.True(result.Success);
        Assert.Equal("Email confirmed successfully.", result.Message);
        Assert.True(refreshedUser!.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithTokenFromClickedActivationUrl_ConfirmsEmail()
    {
        using var host = new BackendTestHost();
        var (user, token) = await CreateUnconfirmedUserWithEmailConfirmationTokenAsync(host, "clicked-activation-url");

        using var scope = host.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();
        var activationUrl = BuildActivationUrl(user.Id, Uri.EscapeDataString(token));
        var htmlHref = WebUtility.HtmlEncode(activationUrl);
        var clickedUrl = WebUtility.HtmlDecode(htmlHref);
        var clickedUserId = GetQueryParameter(clickedUrl, "userId");
        var clickedToken = GetQueryParameter(clickedUrl, "token");

        var result = await service.ConfirmEmailAsync(clickedUserId, clickedToken);
        var refreshedUser = await userManager.FindByIdAsync(user.Id);

        Assert.True(result.Success);
        Assert.True(refreshedUser!.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenClickedActivationUrlContainsUnescapedPlusToken_ConfirmsEmail()
    {
        using var host = new BackendTestHost();
        var (user, token) = await CreateUnconfirmedUserWithEmailConfirmationTokenAsync(
            host,
            "clicked-activation-url-plus",
            requirePlusToken: true);

        using var scope = host.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();
        var encodedToken = Uri.EscapeDataString(token);
        var activationUrlWithUnescapedPlus = BuildActivationUrl(user.Id, encodedToken.Replace("%2B", "+"));
        var clickedToken = GetQueryParameter(activationUrlWithUnescapedPlus, "token");

        Assert.Contains(' ', clickedToken);

        var result = await service.ConfirmEmailAsync(user.Id, clickedToken);
        var refreshedUser = await userManager.FindByIdAsync(user.Id);

        Assert.True(result.Success);
        Assert.True(refreshedUser!.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmailAsync_With100ClickedActivationUrlsContainingUnescapedPlus_ConfirmsAllEmails()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();

        for (var index = 0; index < UrlTokenStressIterations; index++)
        {
            var (user, token) = await CreateUnconfirmedUserWithEmailConfirmationTokenAsync(
                host,
                $"activation-plus-stress-{index}",
                requirePlusToken: true);
            var encodedToken = Uri.EscapeDataString(token);
            var activationUrlWithUnescapedPlus = BuildActivationUrl(user.Id, encodedToken.Replace("%2B", "+"));
            var clickedToken = GetQueryParameter(activationUrlWithUnescapedPlus, "token");

            Assert.Contains(' ', clickedToken);

            var result = await service.ConfirmEmailAsync(user.Id, clickedToken);
            var refreshedUser = await userManager.FindByIdAsync(user.Id);

            Assert.True(result.Success, $"Iteration {index} failed.");
            Assert.True(refreshedUser!.EmailConfirmed, $"Iteration {index} did not confirm email.");
        }
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithMissingUser_ReturnsInvalidUserMessage()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();

        var result = await service.ConfirmEmailAsync("missing-user", "token");

        Assert.False(result.Success);
        Assert.Equal("Invalid user ID.", result.Message);
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_ForUnconfirmedUser_SendsConfirmationEmail()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("resend@example.com", emailConfirmed: false);

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();

        var result = await service.ResendConfirmationEmailAsync(user.Email!);

        Assert.True(result.Success);
        Assert.Equal("Confirmation email sent successfully.", result.Message);
        Assert.Single(host.Emails.ConfirmationEmails);
        Assert.Equal(user.Id, host.Emails.ConfirmationEmails[0].Id);
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_ForConfirmedUser_ReturnsAlreadyConfirmedMessage()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("confirmed@example.com", emailConfirmed: true);

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();

        var result = await service.ResendConfirmationEmailAsync(user.Email!);

        Assert.False(result.Success);
        Assert.Equal("Email already confirmed.", result.Message);
        Assert.Empty(host.Emails.ConfirmationEmails);
    }

    [Fact]
    public async Task SetupAccountAsync_WithValidResetToken_SetsPasswordLanguageAndConfirmsEmail()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var invitedUser = await service.RegisterInvitedUserAsync("setup@example.com");
        var token = await userManager.GeneratePasswordResetTokenAsync(invitedUser!);

        var result = await service.SetupAccountAsync(
            invitedUser!.Id,
            Uri.EscapeDataString(token),
            BackendTestHost.ValidPassword,
            "pl");
        var refreshedUser = await userManager.FindByIdAsync(invitedUser.Id);

        Assert.True(result.Success);
        Assert.Equal("Account setup completed successfully!", result.Message);
        Assert.True(refreshedUser!.EmailConfirmed);
        Assert.Equal(2, refreshedUser.LanguageId);
        Assert.True(await userManager.CheckPasswordAsync(refreshedUser, BackendTestHost.ValidPassword));
    }

    [Fact]
    public async Task SetupAccountAsync_WithClickedUrlTokenContainingUnescapedPlus_SetsPasswordLanguageAndConfirmsEmail()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var invitedUser = await service.RegisterInvitedUserAsync("setup-plus@example.com");
        var token = await GeneratePasswordResetTokenContainingPlusAsync(userManager, invitedUser!);
        var encodedToken = Uri.EscapeDataString(token);
        var setupUrlWithUnescapedPlus = BuildSetupUrl(invitedUser!.Id, encodedToken.Replace("%2B", "+"));
        var clickedToken = GetQueryParameter(setupUrlWithUnescapedPlus, "token");

        Assert.Contains(' ', clickedToken);

        var result = await service.SetupAccountAsync(
            invitedUser.Id,
            clickedToken,
            BackendTestHost.ValidPassword,
            "pl");
        var refreshedUser = await userManager.FindByIdAsync(invitedUser.Id);

        Assert.True(result.Success);
        Assert.True(refreshedUser!.EmailConfirmed);
        Assert.Equal(2, refreshedUser.LanguageId);
        Assert.True(await userManager.CheckPasswordAsync(refreshedUser, BackendTestHost.ValidPassword));
    }

    [Fact]
    public async Task SetupAccountAsync_With100ClickedUrlTokensContainingUnescapedPlus_SetsUpAllAccounts()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        for (var index = 0; index < UrlTokenStressIterations; index++)
        {
            var invitedUser = await service.RegisterInvitedUserAsync($"setup-plus-stress-{index}@example.com");
            var token = await GeneratePasswordResetTokenContainingPlusAsync(userManager, invitedUser!);
            var encodedToken = Uri.EscapeDataString(token);
            var setupUrlWithUnescapedPlus = BuildSetupUrl(invitedUser!.Id, encodedToken.Replace("%2B", "+"));
            var clickedToken = GetQueryParameter(setupUrlWithUnescapedPlus, "token");

            Assert.Contains(' ', clickedToken);

            var result = await service.SetupAccountAsync(
                invitedUser.Id,
                clickedToken,
                BackendTestHost.ValidPassword,
                "pl");
            var refreshedUser = await userManager.FindByIdAsync(invitedUser.Id);

            Assert.True(result.Success, $"Iteration {index} failed.");
            Assert.True(refreshedUser!.EmailConfirmed, $"Iteration {index} did not confirm email.");
            Assert.Equal(2, refreshedUser.LanguageId);
            Assert.True(
                await userManager.CheckPasswordAsync(refreshedUser, BackendTestHost.ValidPassword),
                $"Iteration {index} did not set the password.");
        }
    }

    [Fact]
    public async Task SetupAccountAsync_WithUnknownLanguage_FallsBackToEnglish()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegisterService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var invitedUser = await service.RegisterInvitedUserAsync("setup-language@example.com");
        var token = await userManager.GeneratePasswordResetTokenAsync(invitedUser!);

        var result = await service.SetupAccountAsync(
            invitedUser!.Id,
            Uri.EscapeDataString(token),
            BackendTestHost.ValidPassword,
            "xx");

        var refreshedUser = await userManager.FindByIdAsync(invitedUser.Id);

        Assert.True(result.Success);
        Assert.Equal("Account setup completed successfully!", result.Message);
        Assert.Equal(1, refreshedUser!.LanguageId);
    }

    private static async Task<(ApplicationUser User, string Token)> CreateUnconfirmedUserWithEmailConfirmationTokenAsync(
        BackendTestHost host,
        string emailPrefix,
        bool requirePlusToken = false)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var createdUser = await host.CreateUserAsync(
                $"{emailPrefix}-{attempt}@example.com",
                emailConfirmed: false);

            using var scope = host.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(createdUser.Id)
                ?? throw new InvalidOperationException("Created test user could not be loaded.");
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

            if (!requirePlusToken || token.Contains('+'))
            {
                return (user, token);
            }
        }

        throw new InvalidOperationException("Could not generate an email confirmation token containing '+'.");
    }

    private static async Task<string> GeneratePasswordResetTokenContainingPlusAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            if (token.Contains('+'))
            {
                return token;
            }
        }

        throw new InvalidOperationException("Could not generate a password reset token containing '+'.");
    }

    private static string BuildActivationUrl(string userId, string encodedToken)
    {
        return $"https://frontend.test/confirm-email?userId={Uri.EscapeDataString(userId)}&token={encodedToken}";
    }

    private static string BuildSetupUrl(string userId, string encodedToken)
    {
        return $"https://frontend.test/setup-account?userId={Uri.EscapeDataString(userId)}&token={encodedToken}";
    }

    private static string GetQueryParameter(string url, string parameterName)
    {
        var query = new Uri(url).Query.TrimStart('?');
        var queryParts = query.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var queryPart in queryParts)
        {
            var keyValue = queryPart.Split('=', 2);
            var key = WebUtility.UrlDecode(keyValue[0]);

            if (key == parameterName)
            {
                return keyValue.Length == 2 ? WebUtility.UrlDecode(keyValue[1]) : string.Empty;
            }
        }

        throw new InvalidOperationException($"Query parameter '{parameterName}' was not found.");
    }

    private sealed class ThrowingConfirmationEmailService : IEmailService
    {
        public Task SendConfirmationEmailAsync(ApplicationUser user)
            => throw new InvalidOperationException("SMTP failed.");

        public Task SendAccountSetupEmailAsync(ApplicationUser user, string tournamentName) => Task.CompletedTask;

        public Task SendTournamentInvitationEmailAsync(string email, string tournamentName, int tournamentId) => Task.CompletedTask;

        public Task SendPasswordResetEmailAsync(ApplicationUser user) => Task.CompletedTask;

        public Task SendNotificationEmailAsync(ApplicationUser user, string title, string message, string route, string language) => Task.CompletedTask;
    }
}
