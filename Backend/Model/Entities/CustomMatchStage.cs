using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    public class CustomMatchStage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StageId { get; set; }

        [Required]
        public int Order { get; set; }  // Defines the order of the stages

        [Required]
        [MaxLength(50)]
        public string StageName { get; set; }  // Stage name (e.g., Group A, Group B)

        public int TournamentId { get; set; }
        [ForeignKey("TournamentId")]
        public CustomTournament Tournament { get; set; }
    }
}
