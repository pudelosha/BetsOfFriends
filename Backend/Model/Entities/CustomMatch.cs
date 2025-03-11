using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model.Entities
{
    public class CustomMatch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MatchId { get; set; }

        public string? Stage { get; set; } = string.Empty;

        public int TournamentId { get; set; }
        public CustomTournament Tournament { get; set; }

        public int HomeTeamId { get; set; }
        public CustomTeam HomeTeam { get; set; }

        public int AwayTeamId { get; set; }
        public CustomTeam AwayTeam { get; set; }

        public DateTime MatchStart { get; set; }

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public TeamQualified? Qualified { get; set; }


        // Betting Odds
        public decimal HomeWinOdds { get; set; }
        public decimal DrawOdds { get; set; }
        public decimal AwayWinOdds { get; set; }
        public decimal? HomeQualifies { get; set; }
        public decimal? AwayQualifies { get; set; }

        public MatchStatus Status { get; set; } = MatchStatus.Upcoming;
        public MatchType Type { get; set; } = MatchType.Regular90Min;

        public ICollection<Bet> Bets { get; set; } = new List<Bet>();

        public enum MatchStatus
        {
            Upcoming,
            InProgress,
            Finalised
        }

        public enum MatchType
        {
            Regular90Min,
            ExtendedWithQualification
        }

        public enum TeamQualified
        {
            Home,
            Away
        }
    }
}
