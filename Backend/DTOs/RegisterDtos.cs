using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class RegisterRequestDto
    {
        [MaxLength(50)]
        [Required]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(8)]
        public string Password { get; set; }
        public bool Consent { get; set; }
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
}
