using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model.Entities
{
    public class Match
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? Stage { get; set; } = string.Empty;

        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public int HomeTeamId { get; set; }
        public Team HomeTeam { get; set; }

        public int AwayTeamId { get; set; }
        public Team AwayTeam { get; set; }

        public DateTime MatchStart { get; set; }

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }

        [Required]
        public string BetType { get; set; } = string.Empty;

        // Betting Odds
        public decimal HomeWinOdds { get; set; }
        public decimal DrawOdds { get; set; }
        public decimal AwayWinOdds { get; set; }
        public decimal HomeQualifies { get; set; }
        public decimal AwayQualifies { get; set; }

        public ICollection<Bet> Bets { get; set; } = new List<Bet>();
    }
}
