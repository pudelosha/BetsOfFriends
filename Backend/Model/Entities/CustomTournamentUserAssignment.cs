using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model.Entities
{
    /// <summary>
    /// Many-to-Many Relationship Table Between Users and Tournaments.
    /// Stores user participation and role in tournaments.
    /// </summary>
    public class CustomTournamentUserAssignment : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AssignmentId { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }

        [Required]
        public int TournamentId { get; set; }

        [ForeignKey(nameof(TournamentId))]
        public CustomTournament Tournament { get; set; }

        [Required]
        public UserTournamentRole Role { get; set; } = UserTournamentRole.Player;
        [Required]
        public string UserAdminName { get; set; }
        public string? UserName { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; } = false;

        [Required]
        public AssignmentStatus Status { get; set; } = AssignmentStatus.New;
    }

    public enum UserTournamentRole
    {
        Player,
        Admin
    }

    public enum AssignmentStatus
    {
        New,
        Invited,
        Accepted,
        Requested
    }
}
