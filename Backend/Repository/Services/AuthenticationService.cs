using Backend.DTOs;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Repository.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger<AuthenticationService> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponseDto> AuthenticateUserAsync(LoginRequestDto request)
        {
            _logger.LogInformation($"Login attempt for email: {request.Email}");

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning($"Login failed: User not found for email {request.Email}");
                return new LoginResponseDto { Success = false, Message = "Invalid credentials." };
            }

            // Check if account is locked
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning($"Login failed: Account locked for user {request.Email}");
                return new LoginResponseDto { Success = false, Message = "Account is locked due to multiple failed login attempts." };
            }

            // Check if email is confirmed
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                _logger.LogWarning($"Login failed: Email not confirmed for user {request.Email}");
                return new LoginResponseDto { Success = false, Message = "Email is not confirmed. Please check your inbox." };
            }

            // Validate password
            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning($"Invalid password attempt for user {request.Email}");
                await _userManager.AccessFailedAsync(user); // Increase failed login attempt counter
                return new LoginResponseDto { Success = false, Message = "Invalid credentials." };
            }

            // Reset failed login attempts on success
            await _userManager.ResetAccessFailedCountAsync(user);

            // Generate JWT token
            var token = GenerateJwtToken(user);

            _logger.LogInformation($"Login successful for user {request.Email}");

            return new LoginResponseDto { Success = true, Token = token, Message = "Login successful." };
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var userRoles = _userManager.GetRolesAsync(user).Result;
            claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddHours(24), // Token expiration time
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
