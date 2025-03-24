using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model.Entities
{
    public class CustomTeam
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TeamId { get; set; }

        public int? PredefinedTeamId { get; set; }
        [ForeignKey("PredefinedTeamId")]
        public PredefinedTeam? PredefinedSource { get; set; }

        [Required]
        [MaxLength(50)]
        public string TeamName { get; set; }

        public int TournamentId { get; set; }
        [ForeignKey("TournamentId")]
        public CustomTournament Tournament { get; set; }
    }
}
