using Backend.Controllers;
using Backend.DTOs;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Controllers;

public class RegisterControllerTests
{
    [Fact]
    public async Task Register_WhenConsentIsMissing_ReturnsBadRequestWithoutCallingService()
    {
        var registerService = new FakeRegisterService();
        var controller = new RegisterController(registerService, NullLogger<RegisterController>.Instance);

        var result = await controller.Register(new RegisterRequestDto
        {
            Email = "user@example.com",
            Password = "ValidPassword123!",
            Consent = false,
            Language = "en"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = Assert.IsType<RegisterResultDto>(badRequest.Value);
        Assert.False(payload.Success);
        Assert.Equal("You must accept terms and conditions.", payload.Message);
        Assert.Equal(0, registerService.RegisterCalls);
    }

    [Fact]
    public async Task Register_WhenServiceReturnsFailure_ReturnsBadRequestWithServiceResult()
    {
        var serviceResult = new RegisterResultDto
        {
            Success = false,
            Message = "Email is already in use."
        };
        var registerService = new FakeRegisterService { RegisterResult = serviceResult };
        var controller = new RegisterController(registerService, NullLogger<RegisterController>.Instance);

        var result = await controller.Register(new RegisterRequestDto
        {
            Email = "user@example.com",
            Password = "ValidPassword123!",
            Consent = true,
            Language = "en"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Same(serviceResult, badRequest.Value);
        Assert.Equal(1, registerService.RegisterCalls);
    }

    [Fact]
    public async Task Register_WhenServiceSucceeds_ReturnsOkWithSuccessMessage()
    {
        var registerService = new FakeRegisterService
        {
            RegisterResult = new RegisterResultDto
            {
                Success = true,
                Message = "Registration successful. Please check your email to confirm your account."
            }
        };
        var controller = new RegisterController(registerService, NullLogger<RegisterController>.Instance);

        var result = await controller.Register(new RegisterRequestDto
        {
            Email = "user@example.com",
            Password = "ValidPassword123!",
            Consent = true,
            Language = "en"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<RegisterResultDto>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("Registration successful! Please check your email to confirm your account.", payload.Message);
        Assert.Equal(1, registerService.RegisterCalls);
    }

    [Fact]
    public async Task SetupAccount_WhenRequestIsInvalid_ReturnsBadRequestWithoutCallingService()
    {
        var registerService = new FakeRegisterService();
        var controller = new RegisterController(registerService, NullLogger<RegisterController>.Instance);

        var result = await controller.SetupAccount(new SetupAccountRequestDto
        {
            UserId = "",
            Token = "token",
            Password = "ValidPassword123!",
            Language = "en"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = Assert.IsType<RegisterResultDto>(badRequest.Value);
        Assert.False(payload.Success);
        Assert.Equal("Invalid request data.", payload.Message);
        Assert.Equal(0, registerService.SetupAccountCalls);
    }

    private sealed class FakeRegisterService : IRegisterService
    {
        public int RegisterCalls { get; private set; }
        public int SetupAccountCalls { get; private set; }
        public RegisterResultDto RegisterResult { get; set; } = new() { Success = true };
        public RegisterResultDto SetupAccountResult { get; set; } = new() { Success = true };

        public Task<RegisterResultDto> RegisterUserAsync(string email, string password, string language)
        {
            RegisterCalls++;
            return Task.FromResult(RegisterResult);
        }

        public Task<ApplicationUser?> RegisterInvitedUserAsync(string email)
        {
            return Task.FromResult<ApplicationUser?>(new ApplicationUser { Email = email, UserName = email });
        }

        public Task<RegisterResultDto> ConfirmEmailAsync(string userId, string token)
        {
            return Task.FromResult(new RegisterResultDto { Success = true });
        }

        public Task<RegisterResultDto> ResendConfirmationEmailAsync(string email)
        {
            return Task.FromResult(new RegisterResultDto { Success = true });
        }

        public Task<RegisterResultDto> SetupAccountAsync(string userId, string token, string password, string language)
        {
            SetupAccountCalls++;
            return Task.FromResult(SetupAccountResult);
        }
    }
}
