using System.Security.Claims;
using Backend.Controllers;
using Backend.DTOs;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Controllers;

public class UsersControllerTests
{
    [Fact]
    public async Task ForgotPassword_AlwaysReturnsGenericOkAndCallsService()
    {
        var service = new FakeUserService { PasswordResetEmailSent = false };
        var controller = CreateController(service);

        var result = await controller.ForgotPassword(new ForgotPasswordRequestDto
        {
            Email = "missing@example.com"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("If your email exists in our system, a reset link has been sent.", GetMessage(ok.Value));
        Assert.Equal("missing@example.com", service.PasswordResetEmailAddress);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidModelState_ReturnsBadRequestWithoutCallingService()
    {
        var service = new FakeUserService();
        var controller = CreateController(service);
        controller.ModelState.AddModelError("Token", "Required");

        var result = await controller.ResetPassword(new ResetPasswordRequestDto());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = Assert.IsType<ResetPasswordResultDto>(badRequest.Value);
        Assert.False(payload.Success);
        Assert.Equal("Invalid request data.", payload.Message);
        Assert.Equal(0, service.ResetPasswordCalls);
    }

    [Fact]
    public async Task ResetPassword_WhenServiceSucceeds_ReturnsOk()
    {
        var service = new FakeUserService
        {
            ResetPasswordResult = new ResetPasswordResultDto
            {
                Success = true,
                Message = "Password updated successfully."
            }
        };
        var controller = CreateController(service);

        var result = await controller.ResetPassword(new ResetPasswordRequestDto
        {
            UserId = "user-id",
            Token = "token",
            NewPassword = "NewValidPassword123!"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(service.ResetPasswordResult, ok.Value);
        Assert.Equal(1, service.ResetPasswordCalls);
    }

    [Fact]
    public async Task ResetPassword_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new FakeUserService
        {
            ResetPasswordResult = new ResetPasswordResultDto
            {
                Success = false,
                Message = "Invalid user ID."
            }
        };
        var controller = CreateController(service);

        var result = await controller.ResetPassword(new ResetPasswordRequestDto
        {
            UserId = "missing",
            Token = "token",
            NewPassword = "NewValidPassword123!"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Same(service.ResetPasswordResult, badRequest.Value);
    }

    [Fact]
    public async Task GetUserProfile_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakeUserService(), userId: null);

        var result = await controller.GetUserProfile();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User not authenticated", GetMessage(unauthorized.Value));
    }

    [Fact]
    public async Task GetUserProfile_WhenProfileExists_ReturnsOk()
    {
        var service = new FakeUserService
        {
            ProfileResult = new UserProfileDto
            {
                Email = "profile@example.com",
                Nickname = "Profile",
                Language = "pl"
            }
        };
        var controller = CreateController(service, "user-id");

        var result = await controller.GetUserProfile();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(service.ProfileResult, ok.Value);
        Assert.Equal("user-id", service.ProfileRequestedForUserId);
    }

    [Fact]
    public async Task GetUserProfile_WhenProfileIsMissing_ReturnsNotFound()
    {
        var service = new FakeUserService { ProfileResult = null };
        var controller = CreateController(service, "missing-user");

        var result = await controller.GetUserProfile();

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task UpdateUserProfile_WhenServiceSucceeds_ReturnsOk()
    {
        var service = new FakeUserService { UpdateProfileResult = true };
        var controller = CreateController(service, "user-id");
        var profile = new UserProfileDto { Nickname = "Updated", Language = "en" };

        var result = await controller.UpdateUserProfile(profile);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Profile updated successfully", GetMessage(ok.Value));
        Assert.Equal("user-id", service.ProfileUpdatedForUserId);
        Assert.Same(profile, service.LastProfileUpdate);
    }

    [Fact]
    public async Task UpdateUserProfile_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new FakeUserService { UpdateProfileResult = false };
        var controller = CreateController(service, "user-id");

        var result = await controller.UpdateUserProfile(new UserProfileDto { Language = "en" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to update profile", GetMessage(badRequest.Value));
    }

    [Fact]
    public async Task ChangeEmail_ReturnsOkOrBadRequestBasedOnServiceResult()
    {
        var successService = new FakeUserService { ChangeEmailResult = true };
        var successController = CreateController(successService, "user-id");
        var failService = new FakeUserService { ChangeEmailResult = false };
        var failController = CreateController(failService, "user-id");

        var okResult = await successController.ChangeEmail(new ChangeEmailRequestDto
        {
            NewEmail = "new@example.com",
            Password = "password"
        });
        var badResult = await failController.ChangeEmail(new ChangeEmailRequestDto
        {
            NewEmail = "new@example.com",
            Password = "bad"
        });

        Assert.IsType<OkObjectResult>(okResult);
        Assert.IsType<BadRequestObjectResult>(badResult);
        Assert.Equal(("user-id", "new@example.com", "password"), successService.LastChangeEmail);
    }

    [Fact]
    public async Task UpdatePassword_ReturnsOkOrBadRequestBasedOnServiceResult()
    {
        var successService = new FakeUserService { UpdatePasswordResult = true };
        var successController = CreateController(successService, "user-id");
        var failService = new FakeUserService { UpdatePasswordResult = false };
        var failController = CreateController(failService, "user-id");

        var okResult = await successController.UpdatePassword(new UpdatePasswordRequestDto
        {
            CurrentPassword = "old",
            NewPassword = "new"
        });
        var badResult = await failController.UpdatePassword(new UpdatePasswordRequestDto
        {
            CurrentPassword = "bad",
            NewPassword = "new"
        });

        Assert.IsType<OkObjectResult>(okResult);
        Assert.IsType<BadRequestObjectResult>(badResult);
        Assert.Equal(("user-id", "old", "new"), successService.LastUpdatePassword);
    }

    [Fact]
    public async Task DeleteAccount_ReturnsOkOrBadRequestBasedOnServiceResult()
    {
        var successService = new FakeUserService { DeleteAccountResult = true };
        var successController = CreateController(successService, "user-id");
        var failService = new FakeUserService { DeleteAccountResult = false };
        var failController = CreateController(failService, "user-id");

        var okResult = await successController.DeleteAccount(new DeleteAccountRequestDto { Password = "password" });
        var badResult = await failController.DeleteAccount(new DeleteAccountRequestDto { Password = "bad" });

        Assert.IsType<OkObjectResult>(okResult);
        Assert.IsType<BadRequestObjectResult>(badResult);
        Assert.Equal(("user-id", "password"), successService.LastDeleteAccount);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsUsersFromService()
    {
        var service = new FakeUserService
        {
            AllUsers = new List<ApplicationUserDto>
            {
                new()
                {
                    UserId = "user-id",
                    UserName = "user@example.com",
                    UserEmail = "user@example.com",
                    UserRole = "User",
                    UserStatus = "Active"
                }
            }
        };
        var controller = CreateController(service, "super-admin-id");

        var result = await controller.GetAllUsers();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(service.AllUsers, ok.Value);
    }

    [Fact]
    public async Task SuspendUnsuspendAndAdminDelete_ReturnOkWhenServiceSucceeds()
    {
        var service = new FakeUserService
        {
            SuspendResult = ActionResultDto.SuccessResult("Suspended"),
            UnsuspendResult = ActionResultDto.SuccessResult("Unsuspended"),
            DeleteUserResult = ActionResultDto.SuccessResult("Deleted")
        };
        var controller = CreateController(service, "super-admin-id");
        var request = new UserActionRequestDto { UserId = "target-user" };

        var suspend = await controller.SuspendUser(request);
        var unsuspend = await controller.UnsuspendUser(request);
        var delete = await controller.DeleteUser(request);

        Assert.IsType<OkObjectResult>(suspend);
        Assert.IsType<OkObjectResult>(unsuspend);
        Assert.IsType<OkObjectResult>(delete);
        Assert.Equal(("target-user", "super-admin-id"), service.LastSuspend);
        Assert.Equal(("target-user", "super-admin-id"), service.LastUnsuspend);
        Assert.Equal(("target-user", "super-admin-id"), service.LastDeleteUser);
    }

    [Fact]
    public async Task SuspendUnsuspendAndAdminDelete_ReturnBadRequestWhenServiceFails()
    {
        var service = new FakeUserService
        {
            SuspendResult = ActionResultDto.ErrorResult("Cannot suspend"),
            UnsuspendResult = ActionResultDto.ErrorResult("Cannot unsuspend"),
            DeleteUserResult = ActionResultDto.ErrorResult("Cannot delete")
        };
        var controller = CreateController(service, "super-admin-id");
        var request = new UserActionRequestDto { UserId = "target-user" };

        var suspend = await controller.SuspendUser(request);
        var unsuspend = await controller.UnsuspendUser(request);
        var delete = await controller.DeleteUser(request);

        Assert.IsType<BadRequestObjectResult>(suspend);
        Assert.IsType<BadRequestObjectResult>(unsuspend);
        Assert.IsType<BadRequestObjectResult>(delete);
    }

    [Fact]
    public async Task AdminActions_WithoutAuthenticatedUser_ReturnUnauthorized()
    {
        var controller = CreateController(new FakeUserService(), userId: null);
        var request = new UserActionRequestDto { UserId = "target-user" };

        var suspend = await controller.SuspendUser(request);
        var unsuspend = await controller.UnsuspendUser(request);
        var delete = await controller.DeleteUser(request);

        Assert.IsType<UnauthorizedObjectResult>(suspend);
        Assert.IsType<UnauthorizedObjectResult>(unsuspend);
        Assert.IsType<UnauthorizedObjectResult>(delete);
    }

    private static UsersController CreateController(FakeUserService service, string? userId = "user-id")
    {
        var principal = userId == null
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "TestAuth"));

        return new UsersController(service, NullLogger<UsersController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static string? GetMessage(object? value)
    {
        return value?.GetType().GetProperty("message")?.GetValue(value)?.ToString()
            ?? value?.GetType().GetProperty("Message")?.GetValue(value)?.ToString();
    }

    private sealed class FakeUserService : IUserService
    {
        public bool PasswordResetEmailSent { get; set; }
        public string? PasswordResetEmailAddress { get; private set; }
        public ResetPasswordResultDto ResetPasswordResult { get; set; } = new() { Success = true };
        public int ResetPasswordCalls { get; private set; }
        public UserProfileDto? ProfileResult { get; set; }
        public string? ProfileRequestedForUserId { get; private set; }
        public bool UpdateProfileResult { get; set; }
        public string? ProfileUpdatedForUserId { get; private set; }
        public UserProfileDto? LastProfileUpdate { get; private set; }
        public bool ChangeEmailResult { get; set; }
        public (string UserId, string NewEmail, string Password)? LastChangeEmail { get; private set; }
        public bool UpdatePasswordResult { get; set; }
        public (string UserId, string CurrentPassword, string NewPassword)? LastUpdatePassword { get; private set; }
        public bool DeleteAccountResult { get; set; }
        public (string UserId, string Password)? LastDeleteAccount { get; private set; }
        public List<ApplicationUserDto> AllUsers { get; set; } = new();
        public ActionResultDto SuspendResult { get; set; } = ActionResultDto.SuccessResult("Suspended");
        public ActionResultDto UnsuspendResult { get; set; } = ActionResultDto.SuccessResult("Unsuspended");
        public ActionResultDto DeleteUserResult { get; set; } = ActionResultDto.SuccessResult("Deleted");
        public (string TargetUserId, string AdminUserId)? LastSuspend { get; private set; }
        public (string TargetUserId, string AdminUserId)? LastUnsuspend { get; private set; }
        public (string TargetUserId, string AdminUserId)? LastDeleteUser { get; private set; }

        public string GetUserIdFromClaims(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        }

        public Task<ApplicationUser?> FindUserByEmailAsync(string email)
        {
            return Task.FromResult<ApplicationUser?>(null);
        }

        public Task<ApplicationUser?> FindUserByIdAsync(string userId)
        {
            return Task.FromResult<ApplicationUser?>(null);
        }

        public Task<UserProfileDto> GetUserProfileAsync(string userId)
        {
            ProfileRequestedForUserId = userId;
            return Task.FromResult(ProfileResult!);
        }

        public Task<bool> UpdateUserProfileAsync(string userId, UserProfileDto profile)
        {
            ProfileUpdatedForUserId = userId;
            LastProfileUpdate = profile;
            return Task.FromResult(UpdateProfileResult);
        }

        public Task<bool> SendPasswordResetEmailAsync(string email)
        {
            PasswordResetEmailAddress = email;
            return Task.FromResult(PasswordResetEmailSent);
        }

        public Task<ResetPasswordResultDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            ResetPasswordCalls++;
            return Task.FromResult(ResetPasswordResult);
        }

        public Task<bool> ChangeUserEmailAsync(string userId, string newEmail, string password)
        {
            LastChangeEmail = (userId, newEmail, password);
            return Task.FromResult(ChangeEmailResult);
        }

        public Task<bool> UpdateUserPasswordAsync(string userId, string currentPassword, string newPassword)
        {
            LastUpdatePassword = (userId, currentPassword, newPassword);
            return Task.FromResult(UpdatePasswordResult);
        }

        public Task<bool> DeleteUserAccountAsync(string userId, string password)
        {
            LastDeleteAccount = (userId, password);
            return Task.FromResult(DeleteAccountResult);
        }

        public Task<List<ApplicationUserDto>> GetAllUsersAsync()
        {
            return Task.FromResult(AllUsers);
        }

        public Task<ActionResultDto> SuspendUserAsync(string targetUserId, string performedByUserId)
        {
            LastSuspend = (targetUserId, performedByUserId);
            return Task.FromResult(SuspendResult);
        }

        public Task<ActionResultDto> UnsuspendUserAsync(string targetUserId, string adminUserId)
        {
            LastUnsuspend = (targetUserId, adminUserId);
            return Task.FromResult(UnsuspendResult);
        }

        public Task<ActionResultDto> DeleteUserAsync(string targetUserId, string performedByUserId)
        {
            LastDeleteUser = (targetUserId, performedByUserId);
            return Task.FromResult(DeleteUserResult);
        }
    }
}
