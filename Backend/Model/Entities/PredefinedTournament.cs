using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Backend.Model.Entities.CustomTournament;

namespace Backend.Model.Entities
{
    public class PredefinedTournament : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TournamentId { get; set; }
        public int? ExternalTournamentId { get; set; }
        public int? ExternalSeasonId { get; set; }
        public int? Season { get; set; }
        public DateTime? EndDate { get; set; }

        [Required, MaxLength(100)]
        public string TournamentName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public TournamentUpdate Update { get; set; } = TournamentUpdate.Manual;
        public bool CalculateBetsWithHomeAdvantage { get; set; } = false;

        public ICollection<PredefinedTeam> PredefinedTeams { get; set; } = new List<PredefinedTeam>();
        public ICollection<PredefinedMatch> PredefinedMatches { get; set; } = new List<PredefinedMatch>();
        public ICollection<PredefinedMatchStage> PredefinedStages { get; set; } = new List<PredefinedMatchStage>();

    }
}
