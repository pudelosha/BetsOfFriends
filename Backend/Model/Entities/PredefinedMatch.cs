using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    public class PredefinedMatch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MatchId { get; set; }

        public int StageId { get; set; }
        [ForeignKey("StageId")]
        public PredefinedMatchStage PredefinedStage { get; set; }

        public int TournamentId { get; set; }
        [ForeignKey("TournamentId")]
        public PredefinedTournament PredefinedTournament { get; set; }

        [Required]
        public int HomeTeamId { get; set; }
        [ForeignKey("HomeTeamId")]
        public PredefinedTeam HomeTeam { get; set; }

        [Required]
        public int AwayTeamId { get; set; }
        [ForeignKey("AwayTeamId")]
        public PredefinedTeam AwayTeam { get; set; }

        [Required]
        public DateTime MatchStart { get; set; }

        [Required]
        public CustomMatch.MatchType Type { get; set; } = CustomMatch.MatchType.Regular90Min;

        public bool IsVisible { get; set; } = true;

        public decimal HomeWinOdds { get; set; }
        public decimal DrawOdds { get; set; }
        public decimal AwayWinOdds { get; set; }
        public decimal? HomeQualifies { get; set; }
        public decimal? AwayQualifies { get; set; }
    }
}
