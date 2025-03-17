using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    public class PredefinedMatchStage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StageId { get; set; }

        [Required]
        public int Order { get; set; }

        [Required]
        [MaxLength(50)]
        public string StageName { get; set; }

        public int TournamentId { get; set; }
        [ForeignKey("TournamentId")]
        public PredefinedTournament PredefinedTournament { get; set; }
    }
}
