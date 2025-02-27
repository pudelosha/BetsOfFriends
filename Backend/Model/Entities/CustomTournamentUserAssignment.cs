using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model.Entities
{
    /// <summary>
    /// Many-to-Many Relationship Table Between Users and Tournaments.
    /// Stores user participation and role in tournaments.
    /// </summary>
    public class CustomTournamentUserAssignment
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
        public UserTournamentRole Role { get; set; } = UserTournamentRole.Guest;

        [Required]
        public bool IsConfirmed { get; set; } = false;
    }

    /// <summary>
    /// Defines user roles within a tournament.
    /// </summary>
    public enum UserTournamentRole
    {
        Guest,
        Admin
    }
}
