using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    public class Game
    {
        [Key]
        public int Id { get; set; }

        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public int HomeTeamId { get; set; }
        public Team HomeTeam { get; set; }

        public int AwayTeamId { get; set; }
        public Team AwayTeam { get; set; }

        public DateTime MatchStart { get; set; }

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }

        [MaxLength(20)]
        public string? Group { get; set; }

        public bool WhoQualifiesBetRequired { get; set; }

        // Betting Odds
        public decimal OddsHomeWin { get; set; }
        public decimal OddsDraw { get; set; }
        public decimal OddsAwayWin { get; set; }
        public decimal? OddsExactResult { get; set; }
        public decimal? OddsHomeQualifies {  get; set; }
        public decimal? OddsAwayQualifies { get; set; }

        public ICollection<Bet> Bets { get; set; } = new List<Bet>();
    }
}
