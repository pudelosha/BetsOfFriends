using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model.Entities
{
    /// <summary>
    /// Represents a football tournament where users can bet on matches.
    /// </summary>
    public class CustomTournament
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TournamentId { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        [Required]
        public bool IsActive { get; set; } = true;
        public TournamentType Type { get; set; }

        [Required]
        public string CreatedByUserId { get; set; }
        public ApplicationUser CreatedByUser { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Tournament Settings
        public bool AllowExactResultBonus { get; set; } = false;

        [Required]
        public ExactResultBonusCalculationType ExactResultBonusCalculation { get; set; } = ExactResultBonusCalculationType.Fixed;

        [Range(1, int.MaxValue)]
        public int? ExactResultBonus { get; set; }

        public bool AllowWhoQualifiesBets { get; set; } = false;

        public bool AllowBetsWithBooster { get; set; } = false;

        [Range(1, int.MaxValue)]
        public int MaxBetBooster { get; set; } = 1;

        [Range(1, int.MaxValue)]
        public int? TotalBoosterPool { get; set; }

        public bool AllowNonSubmittedBetsPenalty { get; set; } = false;

        [Range(0, int.MaxValue)]
        public int? NonSubmittedBetPenalty { get; set; }

        public ICollection<CustomTournamentUserAssignment> Participants { get; set; } = new List<CustomTournamentUserAssignment>();
        public ICollection<CustomTeam> Teams { get; set; } = new List<CustomTeam>();
        public ICollection<CustomMatch> Matches { get; set; } = new List<CustomMatch>();

        public enum TournamentType
        {
            GroupAndKnockout,
            League,
            Cup
        }

        public enum ExactResultBonusCalculationType
        {
            Fixed,
            Multiplied
        }
    }
}
