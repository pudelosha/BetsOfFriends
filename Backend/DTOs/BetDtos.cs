using Backend.Model.Entities;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class BetUpdateDto
    {
        public decimal BaseAmount { get; set; }
        public decimal? BonusAmount { get; set; }
        public int? HomeGoals { get; set; }
        public int? AwayGoals { get; set; }
        public Bet.Team? QualifiedTeam { get; set; }
    }

    public class BetDto
    {
        public int BetId { get; set; }
        public int MatchId { get; set; }

        public string TeamHome { get; set; }
        public string TeamAway { get; set; }
        public DateTime StartTime { get; set; }

        public decimal BaseAmount { get; set; }
        public decimal? BonusAmount { get; set; }

        public int? PlayerHomeGoals { get; set; }
        public int? PlayerAwayGoals { get; set; }
        public int? ActualHomeGoals { get; set; }
        public int? ActualAwayGoals { get; set; }

        public decimal HomeOdds { get; set; }
        public decimal DrawOdds { get; set; }
        public decimal AwayOdds { get; set; }

        public decimal? QualifyHomeOdds { get; set; }
        public decimal? QualifyAwayOdds { get; set; }

        public string? QualifiedTeam { get; set; }
        public string Status { get; set; }
        public string Result { get; set; }
    }
}
