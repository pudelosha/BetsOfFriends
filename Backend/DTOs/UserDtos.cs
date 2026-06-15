using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordRequestDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; }
    }

    public class ResetPasswordResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public IEnumerable<IdentityError>? Errors { get; set; }
    }

    public class UserProfileDto
    {
        public string? Email { get; set; }
        public string? Nickname { get; set; }
        public LocationDto? Location { get; set; }
        public DateTime? MemberSince { get; set; }
        public string Language { get; set; } = "en";
        public bool DarkMode { get; set; }
    }

    public class ChangeEmailRequestDto
    {
        public string NewEmail { get; set; }
        public string Password { get; set; }
    }

    public class UpdatePasswordRequestDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class DeleteAccountRequestDto
    {
        public string Password { get; set; }
    }

    public class ApplicationUserDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserRole { get; set; }
        public string UserStatus { get; set; }

        public int TournamentAdminCount { get; set; }
        public int TournamentParticipantCount { get; set; }

        public DateTime MemberSince { get; set; }
    }

    public class PagedApplicationUsersDto
    {
        public List<ApplicationUserDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class UserActionRequestDto
    {
        public string UserId { get; set; }
    }
}
