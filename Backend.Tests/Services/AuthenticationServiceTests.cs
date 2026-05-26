using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Services;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task AuthenticateUserAsync_WithConfirmedUserAndValidPassword_ReturnsJwtWithUserClaims()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync(
            "login-success@example.com",
            emailConfirmed: true,
            roleName: "Admin",
            languageId: 2);

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        var result = await service.AuthenticateUserAsync(new LoginRequestDto
        {
            Email = user.Email!,
            Password = BackendTestHost.ValidPassword
        });

        Assert.True(result.Success);
        Assert.Equal("Login successful.", result.Message);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(user.Id, GetClaimValue(token, ClaimTypes.NameIdentifier, "nameid", "sub"));
        Assert.Equal(user.Email, GetClaimValue(token, ClaimTypes.Email, JwtRegisteredClaimNames.Email, "email"));
        Assert.Equal("pl", GetClaimValue(token, "preferred_language"));
        Assert.Contains(token.Claims, claim => IsRoleClaim(claim) && claim.Value == "Admin");
    }

    [Fact]
    public async Task AuthenticateUserAsync_WithUnknownEmail_ReturnsInvalidCredentials()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        var result = await service.AuthenticateUserAsync(new LoginRequestDto
        {
            Email = "missing@example.com",
            Password = BackendTestHost.ValidPassword
        });

        Assert.False(result.Success);
        Assert.Equal("Invalid credentials.", result.Message);
        Assert.True(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task AuthenticateUserAsync_WithUnconfirmedEmail_ReturnsEmailConfirmationMessage()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("unconfirmed@example.com", emailConfirmed: false);

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        var result = await service.AuthenticateUserAsync(new LoginRequestDto
        {
            Email = user.Email!,
            Password = BackendTestHost.ValidPassword
        });

        Assert.False(result.Success);
        Assert.Equal("Email is not confirmed. Please check your inbox.", result.Message);
    }

    [Fact]
    public async Task AuthenticateUserAsync_WithWrongPassword_IncrementsFailedAccessCount()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("wrong-password@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        var result = await service.AuthenticateUserAsync(new LoginRequestDto
        {
            Email = user.Email!,
            Password = "WrongPassword123!"
        });

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var refreshedUser = await userManager.FindByIdAsync(user.Id);

        Assert.False(result.Success);
        Assert.Equal("Invalid credentials.", result.Message);
        Assert.Equal(1, refreshedUser?.AccessFailedCount);
    }

    [Fact]
    public async Task AuthenticateUserAsync_WithLockedOutUser_ReturnsLockedMessage()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("locked@example.com");

        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lockedUser = await dbContext.Users.FindAsync(user.Id);
        lockedUser!.LockoutEnabled = true;
        lockedUser.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);
        await dbContext.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        var result = await service.AuthenticateUserAsync(new LoginRequestDto
        {
            Email = user.Email!,
            Password = BackendTestHost.ValidPassword
        });

        Assert.False(result.Success);
        Assert.Equal("Account is locked due to multiple failed login attempts.", result.Message);
    }

    private static string? GetClaimValue(JwtSecurityToken token, params string[] claimTypes)
    {
        return token.Claims.FirstOrDefault(claim => claimTypes.Contains(claim.Type))?.Value;
    }

    private static bool IsRoleClaim(Claim claim)
    {
        return claim.Type == ClaimTypes.Role || claim.Type == "role";
    }
}
