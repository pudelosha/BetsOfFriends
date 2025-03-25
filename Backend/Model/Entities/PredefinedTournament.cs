using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Backend.Model.Entities.CustomTournament;

namespace Backend.Model.Entities
{
    public class PredefinedTournament
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TournamentId { get; set; }

        [Required, MaxLength(100)]
        public string TournamentName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TournamentUpdate Update { get; set; } = TournamentUpdate.Manual;

        public ICollection<PredefinedTeam> PredefinedTeams { get; set; } = new List<PredefinedTeam>();
        public ICollection<PredefinedMatch> PredefinedMatches { get; set; } = new List<PredefinedMatch>();
        public ICollection<PredefinedMatchStage> PredefinedStages { get; set; } = new List<PredefinedMatchStage>();

    }
}
