using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model.Entities
{
    public class CustomTournamentExtraPrediction : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PredictionId { get; set; }

        public int TournamentId { get; set; }
        public CustomTournament Tournament { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int? WinnerTeamId { get; set; }
        public CustomTeam? WinnerTeam { get; set; }

        public int? SecondPlaceTeamId { get; set; }
        public CustomTeam? SecondPlaceTeam { get; set; }

        public int? ThirdPlaceTeamId { get; set; }
        public CustomTeam? ThirdPlaceTeam { get; set; }

        public int? TopScorerTeamId { get; set; }
        public CustomTeam? TopScorerTeam { get; set; }

        [MaxLength(80)]
        public string? TopScorerName { get; set; }
    }
}
