using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model.Entities
{
    public class UserPushSubscription : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }

        [Required]
        [MaxLength(2048)]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string EndpointHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string P256dh { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Auth { get; set; } = string.Empty;

        public long? ExpirationTime { get; set; }

        [MaxLength(512)]
        public string? UserAgent { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? LastSeenAt { get; set; }
        public DateTime? LastSentAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
