using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    public class PredefinedTeam : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TeamId { get; set; }

        public int? ExternalTeamId { get; set; }

        [Required, MaxLength(50)]
        public string TeamName { get; set; } = string.Empty;

        public int PredefinedTournamentId { get; set; }
        [ForeignKey("PredefinedTournamentId")]
        public PredefinedTournament PredefinedTournament { get; set; }
    }
}
