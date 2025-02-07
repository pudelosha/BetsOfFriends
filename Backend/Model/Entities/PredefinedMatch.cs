using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    public class PredefinedMatch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MatchId { get; set; }

        public int PredefinedTournamentId { get; set; }
        [ForeignKey("PredefinedTournamentId")]
        public PredefinedTournament PredefinedTournament { get; set; }

        public string Stage { get; set; } = string.Empty;

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
        public string BetType { get; set; } = string.Empty;

        public decimal HomeWinOdds { get; set; }
        public decimal DrawOdds { get; set; }
        public decimal AwayWinOdds { get; set; }
        public decimal? HomeQualifies { get; set; }
        public decimal? AwayQualifies { get; set; }
    }
}
