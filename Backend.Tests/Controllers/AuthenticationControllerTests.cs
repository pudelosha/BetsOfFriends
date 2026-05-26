using Backend.Controllers;
using Backend.DTOs;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Controllers;

public class AuthenticationControllerTests
{
    [Fact]
    public async Task Login_WhenAuthenticationSucceeds_ReturnsOkWithLoginResponse()
    {
        var response = new LoginResponseDto
        {
            Success = true,
            Token = "jwt-token",
            Message = "Login successful."
        };
        var controller = new AuthenticationController(
            new FakeAuthenticationService(response),
            NullLogger<AuthenticationController>.Instance);

        var result = await controller.Login(new LoginRequestDto
        {
            Email = "user@example.com",
            Password = "ValidPassword123!"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task Login_WhenAuthenticationFails_ReturnsUnauthorizedWithLoginResponse()
    {
        var response = new LoginResponseDto
        {
            Success = false,
            Message = "Invalid credentials."
        };
        var controller = new AuthenticationController(
            new FakeAuthenticationService(response),
            NullLogger<AuthenticationController>.Instance);

        var result = await controller.Login(new LoginRequestDto
        {
            Email = "user@example.com",
            Password = "bad-password"
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Same(response, unauthorized.Value);
    }

    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        private readonly LoginResponseDto _response;

        public FakeAuthenticationService(LoginResponseDto response)
        {
            _response = response;
        }

        public Task<LoginResponseDto> AuthenticateUserAsync(LoginRequestDto request)
        {
            return Task.FromResult(_response);
        }
    }
}
