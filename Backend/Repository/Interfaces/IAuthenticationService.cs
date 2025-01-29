using Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface IAuthenticationService
    {
        Task<LoginResponseDto> AuthenticateUserAsync(LoginRequestDto request);
    }
}
