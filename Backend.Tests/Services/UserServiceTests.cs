using System.Security.Claims;
using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public void GetUserIdFromClaims_ReturnsNameIdentifierClaim()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-id-123")
        }));

        var userId = service.GetUserIdFromClaims(principal);

        Assert.Equal("user-id-123", userId);
    }

    [Fact]
    public async Task FindUserMethods_ReturnUsersByEmailAndId()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("find-user@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var byEmail = await service.FindUserByEmailAsync(user.Email!);
        var byId = await service.FindUserByIdAsync(user.Id);

        Assert.Equal(user.Id, byEmail?.Id);
        Assert.Equal(user.Email, byId?.Email);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithExistingUser_ReturnsProfileWithLanguageAndLocation()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("profile@example.com", languageId: 2);

        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var trackedUser = await dbContext.Users.FindAsync(user.Id);
        trackedUser!.Nickname = "Tipster";
        trackedUser.LocationId = 235;
        await dbContext.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var profile = await service.GetUserProfileAsync(user.Id);

        Assert.NotNull(profile);
        Assert.Equal("profile@example.com", profile!.Email);
        Assert.Equal("Tipster", profile.Nickname);
        Assert.Equal("pl", profile.Language);
        Assert.NotNull(profile.Location);
        Assert.Equal(235, profile.Location!.CountryId);
        Assert.Equal("United States", profile.Location.Name);
        Assert.False(profile.DarkMode);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithMissingUser_ReturnsNull()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var profile = await service.GetUserProfileAsync("missing-user");

        Assert.Null(profile);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WithKnownLanguage_UpdatesNicknameLocationAndLanguage()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("update-profile@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var updated = await service.UpdateUserProfileAsync(user.Id, new UserProfileDto
        {
            Nickname = "Updated Nick",
            Language = "pl",
            Location = new LocationDto { CountryId = 235, Name = "United States" }
        });

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedUser = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);

        Assert.True(updated);
        Assert.Equal("Updated Nick", savedUser.Nickname);
        Assert.Equal(235, savedUser.LocationId);
        Assert.Equal(2, savedUser.LanguageId);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WithUnknownLanguage_FallsBackToEnglish()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("fallback-language@example.com", languageId: 2);

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var updated = await service.UpdateUserProfileAsync(user.Id, new UserProfileDto
        {
            Nickname = "Fallback",
            Language = "xx"
        });

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedUser = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);

        Assert.True(updated);
        Assert.Equal(1, savedUser.LanguageId);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithExistingUser_SendsResetEmail()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("forgot-password@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var sent = await service.SendPasswordResetEmailAsync(user.Email!);

        Assert.True(sent);
        var resetEmail = Assert.Single(host.Emails.PasswordResetEmails);
        Assert.Equal(user.Id, resetEmail.Id);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithMissingUser_ReturnsFalseAndDoesNotSendEmail()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var sent = await service.SendPasswordResetEmailAsync("missing-reset@example.com");

        Assert.False(sent);
        Assert.Empty(host.Emails.PasswordResetEmails);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_UpdatesPassword()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("reset-password@example.com");

        using var scope = host.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();
        var trackedUser = await userManager.FindByIdAsync(user.Id);
        var token = await userManager.GeneratePasswordResetTokenAsync(trackedUser!);

        var result = await service.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            UserId = user.Id,
            Token = Uri.EscapeDataString(token),
            NewPassword = "NewValidPassword123!"
        });
        var refreshedUser = await userManager.FindByIdAsync(user.Id);

        Assert.True(result.Success);
        Assert.Equal("Password updated successfully.", result.Message);
        Assert.True(await userManager.CheckPasswordAsync(refreshedUser!, "NewValidPassword123!"));
        Assert.False(await userManager.CheckPasswordAsync(refreshedUser!, BackendTestHost.ValidPassword));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ReturnsFailure()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("bad-reset-token@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await service.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            UserId = user.Id,
            Token = "bad-token",
            NewPassword = "NewValidPassword123!"
        });

        Assert.False(result.Success);
        Assert.NotNull(result.Errors);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithMissingUser_ReturnsInvalidUserId()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await service.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            UserId = "missing-user",
            Token = "token",
            NewPassword = "NewValidPassword123!"
        });

        Assert.False(result.Success);
        Assert.Equal("Invalid user ID.", result.Message);
    }

    [Fact]
    public async Task ChangeUserEmailAsync_WithCorrectPassword_UpdatesEmailAndNormalizedEmail()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("old-email@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var updated = await service.ChangeUserEmailAsync(user.Id, "new-email@example.com", BackendTestHost.ValidPassword);

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedUser = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        Assert.True(updated);
        Assert.Equal("new-email@example.com", savedUser.Email);
        Assert.Equal("NEW-EMAIL@EXAMPLE.COM", savedUser.NormalizedEmail);
    }

    [Fact]
    public async Task ChangeUserEmailAsync_WithWrongPassword_ReturnsFalse()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("email-wrong-password@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var updated = await service.ChangeUserEmailAsync(user.Id, "should-not-change@example.com", "WrongPassword123!");

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedUser = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        Assert.False(updated);
        Assert.Equal("email-wrong-password@example.com", savedUser.Email);
    }

    [Fact]
    public async Task UpdateUserPasswordAsync_WithCorrectCurrentPassword_ChangesPassword()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("change-password@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var updated = await service.UpdateUserPasswordAsync(
            user.Id,
            BackendTestHost.ValidPassword,
            "ChangedPassword123!");
        var refreshedUser = await userManager.FindByIdAsync(user.Id);

        Assert.True(updated);
        Assert.True(await userManager.CheckPasswordAsync(refreshedUser!, "ChangedPassword123!"));
        Assert.False(await userManager.CheckPasswordAsync(refreshedUser!, BackendTestHost.ValidPassword));
    }

    [Fact]
    public async Task UpdateUserPasswordAsync_WithWrongCurrentPassword_ReturnsFalse()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("change-password-wrong@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var updated = await service.UpdateUserPasswordAsync(user.Id, "WrongPassword123!", "ChangedPassword123!");

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteUserAccountAsync_WithCorrectPassword_DeletesCurrentUser()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("delete-account@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var deleted = await service.DeleteUserAccountAsync(user.Id, BackendTestHost.ValidPassword);

        Assert.True(deleted);
        Assert.Null(await userManager.FindByIdAsync(user.Id));
    }

    [Fact]
    public async Task DeleteUserAccountAsync_WithWrongPassword_ReturnsFalse()
    {
        using var host = new BackendTestHost();
        var user = await host.CreateUserAsync("delete-account-wrong@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var deleted = await service.DeleteUserAccountAsync(user.Id, "WrongPassword123!");

        Assert.False(deleted);
        Assert.NotNull(await userManager.FindByIdAsync(user.Id));
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsRoleStatusAndTournamentCounts()
    {
        using var host = new BackendTestHost();
        var activeUser = await host.CreateUserAsync("active-list@example.com", roleName: "Admin");
        var inactiveUser = await host.CreateUserAsync("inactive-list@example.com", emailConfirmed: false);
        var suspendedUser = await host.CreateUserAsync("suspended-list@example.com");

        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var trackedActiveUser = await dbContext.Users.FindAsync(activeUser.Id);
        var trackedSuspendedUser = await dbContext.Users.FindAsync(suspendedUser.Id);
        trackedSuspendedUser!.LockoutEnabled = true;
        trackedSuspendedUser.LockoutEnd = DateTimeOffset.UtcNow.AddDays(1);

        var tournament = new CustomTournament
        {
            Name = "User List Cup",
            CreatedByUserId = activeUser.Id,
            CreatedByUser = trackedActiveUser!,
            IsActive = true
        };
        dbContext.CustomTournaments.Add(tournament);
        await dbContext.SaveChangesAsync();

        dbContext.CustomTournamentUserAssignments.Add(new CustomTournamentUserAssignment
        {
            UserId = activeUser.Id,
            User = trackedActiveUser!,
            TournamentId = tournament.TournamentId,
            Tournament = tournament,
            Role = UserTournamentRole.Admin,
            UserAdminName = "Active Admin",
            UserName = "Active Admin",
            Status = AssignmentStatus.Accepted
        });
        await dbContext.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var users = await service.GetAllUsersAsync();

        var activeDto = users.Single(u => u.UserEmail == activeUser.Email);
        var inactiveDto = users.Single(u => u.UserEmail == inactiveUser.Email);
        var suspendedDto = users.Single(u => u.UserEmail == suspendedUser.Email);
        Assert.Equal("Admin", activeDto.UserRole);
        Assert.Equal("Active", activeDto.UserStatus);
        Assert.Equal(1, activeDto.TournamentAdminCount);
        Assert.Equal(1, activeDto.TournamentParticipantCount);
        Assert.Equal("Inactive", inactiveDto.UserStatus);
        Assert.Equal("Suspended", suspendedDto.UserStatus);
    }

    [Fact]
    public async Task SuspendAndUnsuspendUserAsync_TogglesLockoutState()
    {
        using var host = new BackendTestHost();
        var admin = await host.CreateUserAsync("suspend-admin@example.com", roleName: "SuperAdmin");
        var target = await host.CreateUserAsync("suspend-target@example.com");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var suspendResult = await service.SuspendUserAsync(target.Id, admin.Id);
        var afterSuspend = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Users.AsNoTracking()
            .SingleAsync(u => u.Id == target.Id);
        var unsuspendResult = await service.UnsuspendUserAsync(target.Id, admin.Id);
        var afterUnsuspend = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Users.AsNoTracking()
            .SingleAsync(u => u.Id == target.Id);

        Assert.True(suspendResult.Success);
        Assert.Equal("User suspended successfully.", suspendResult.Message);
        Assert.True(afterSuspend.LockoutEnabled);
        Assert.True(afterSuspend.LockoutEnd > DateTimeOffset.UtcNow);
        Assert.True(unsuspendResult.Success);
        Assert.Equal("User unsuspended successfully.", unsuspendResult.Message);
        Assert.False(afterUnsuspend.LockoutEnabled);
        Assert.Null(afterUnsuspend.LockoutEnd);
    }

    [Fact]
    public async Task AdminDeleteUserAsync_PreventsDeletingSuperAdmin()
    {
        using var host = new BackendTestHost();
        var admin = await host.CreateUserAsync("delete-admin@example.com", roleName: "SuperAdmin");
        var targetSuperAdmin = await host.CreateUserAsync("delete-super-admin@example.com", roleName: "SuperAdmin");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await service.DeleteUserAsync(targetSuperAdmin.Id, admin.Id);

        Assert.False(result.Success);
        Assert.Equal("You cannot delete a Super Admin.", result.Message);
    }

    [Fact]
    public async Task AdminDeleteUserAsync_DeletesUserAndRelatedRows()
    {
        using var host = new BackendTestHost();
        var admin = await host.CreateUserAsync("admin-delete-user@example.com", roleName: "SuperAdmin");
        var target = await host.CreateUserAsync("target-delete-user@example.com");

        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedUserRelatedRowsAsync(dbContext, admin.Id, target.Id);
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await service.DeleteUserAsync(target.Id, admin.Id);

        Assert.True(result.Success);
        Assert.Equal("User deleted successfully.", result.Message);
        Assert.False(await dbContext.Users.AnyAsync(u => u.Id == target.Id));
        Assert.False(await dbContext.Bets.AnyAsync(b => b.UserId == target.Id));
        Assert.False(await dbContext.CustomTournamentUserAssignments.AnyAsync(a => a.UserId == target.Id));
        Assert.False(await dbContext.NotificationRecipients.AnyAsync(n => n.UserId == target.Id));
    }

    [Fact]
    public async Task AdminActions_WithMissingTarget_ReturnUserNotFound()
    {
        using var host = new BackendTestHost();
        var admin = await host.CreateUserAsync("missing-target-admin@example.com", roleName: "SuperAdmin");

        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();

        var suspend = await service.SuspendUserAsync("missing-user", admin.Id);
        var unsuspend = await service.UnsuspendUserAsync("missing-user", admin.Id);
        var delete = await service.DeleteUserAsync("missing-user", admin.Id);

        Assert.False(suspend.Success);
        Assert.False(unsuspend.Success);
        Assert.False(delete.Success);
        Assert.Equal("User not found.", suspend.Message);
        Assert.Equal("User not found.", unsuspend.Message);
        Assert.Equal("User not found.", delete.Message);
    }

    private static async Task SeedUserRelatedRowsAsync(AppDbContext dbContext, string adminUserId, string targetUserId)
    {
        var admin = await dbContext.Users.FindAsync(adminUserId)
            ?? throw new InvalidOperationException("Admin user missing in test seed.");
        var target = await dbContext.Users.FindAsync(targetUserId)
            ?? throw new InvalidOperationException("Target user missing in test seed.");

        var tournament = new CustomTournament
        {
            Name = "Delete User Cup",
            CreatedByUserId = admin.Id,
            CreatedByUser = admin,
            IsActive = true
        };
        dbContext.CustomTournaments.Add(tournament);
        await dbContext.SaveChangesAsync();

        var assignment = new CustomTournamentUserAssignment
        {
            UserId = target.Id,
            User = target,
            TournamentId = tournament.TournamentId,
            Tournament = tournament,
            Role = UserTournamentRole.Player,
            UserAdminName = "Delete Target",
            UserName = "Delete Target",
            Status = AssignmentStatus.Accepted
        };
        var stage = new CustomMatchStage
        {
            TournamentId = tournament.TournamentId,
            Tournament = tournament,
            StageName = "Final",
            Order = 1
        };
        var homeTeam = new CustomTeam
        {
            TournamentId = tournament.TournamentId,
            Tournament = tournament,
            TeamName = "Home",
            EloRating = 1500
        };
        var awayTeam = new CustomTeam
        {
            TournamentId = tournament.TournamentId,
            Tournament = tournament,
            TeamName = "Away",
            EloRating = 1500
        };

        dbContext.CustomTournamentUserAssignments.Add(assignment);
        dbContext.CustomMatchStages.Add(stage);
        dbContext.CustomTeams.AddRange(homeTeam, awayTeam);
        await dbContext.SaveChangesAsync();

        var match = new CustomMatch
        {
            TournamentId = tournament.TournamentId,
            Tournament = tournament,
            StageId = stage.StageId,
            Stage = stage,
            HomeTeamId = homeTeam.TeamId,
            HomeTeam = homeTeam,
            AwayTeamId = awayTeam.TeamId,
            AwayTeam = awayTeam,
            MatchStart = DateTime.UtcNow.AddDays(1),
            HomeWinOdds = 2,
            DrawOdds = 3,
            AwayWinOdds = 4
        };
        var notification = new Notification
        {
            Title = "Delete cleanup",
            Message = "Cleanup",
            Route = "/cleanup",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.CustomMatches.Add(match);
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        dbContext.Bets.Add(new Bet
        {
            MatchId = match.MatchId,
            Match = match,
            UserId = target.Id,
            User = target,
            Status = Bet.BetStatus.ToPlace,
            Result = Bet.BetResult.Pending
        });
        dbContext.NotificationRecipients.Add(new NotificationRecipient
        {
            UserId = target.Id,
            User = target,
            NotificationId = notification.Id,
            Notification = notification
        });
        await dbContext.SaveChangesAsync();
    }
}
