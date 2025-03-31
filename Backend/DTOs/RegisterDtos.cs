using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(8)]
        public string Password { get; set; }
        public bool Consent { get; set; }
        public string Language { get; set; }
    }

    public class RegisterResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public IEnumerable<IdentityError>? Errors { get; set; }
    }

    public class ResendConfirmationRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class SetupAccountRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Language { get; set; }
    }
}
