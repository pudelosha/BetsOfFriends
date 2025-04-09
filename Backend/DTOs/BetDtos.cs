using Backend.Model.Entities;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;

namespace Backend.DTOs
{
    public class BetUpdateDto
    {
        public decimal BaseAmount { get; set; }
        public decimal? BonusAmount { get; set; }
        public int? HomeGoals { get; set; }
        public int? AwayGoals { get; set; }
        public string? QualifiedTeam { get; set; }
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
        public string? PlayerQualifiedTeam { get; set; }
        public int? ActualHomeGoals { get; set; }
        public int? ActualAwayGoals { get; set; }
        public string? ActualQualifiedTeam { get; set; }

        public decimal HomeOdds { get; set; }
        public decimal DrawOdds { get; set; }
        public decimal AwayOdds { get; set; }

        public decimal? QualifyHomeOdds { get; set; }
        public decimal? QualifyAwayOdds { get; set; }


        public string MatchStatus { get; set; }
        public string Status { get; set; }
        public string Result { get; set; }
        public string Type { get; set; }

        public bool ShowWhoQualifies{ get; set; } = false;
    }

    public class BetStatsDto
    {
        public bool ShowExactResult { get; set; } = false;
        public bool ShowQualified { get; set; } = false;
        public string MatchStatus { get; set; } = string.Empty;

        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }

        public int? HomeScoreUser { get; set; }
        public int? AwayScoreUser { get; set; }
        public int? HomeScoreActual { get; set; }
        public int? AwayScoreActual { get; set; }

        public string? QualifiedTeam { get; set; }

        public decimal Percent1 { get; set; }
        public decimal PercentX { get; set; }
        public decimal Percent2 { get; set; }
        public decimal? Percent1Q { get; set; }
        public decimal? Percent2Q { get; set; }

        public string? Result { get; set; }
        public string? ResultQualified { get; set; }

        public List<UserBetDto>? UserBets { get; set; }
    }

    public class UserBetDto
    {
        public string Username { get; set; } // Player's name
        public string BetScore { get; set; } // Example: "2-1" or "0-0"

        // Success flags (1 = correct, 0 = incorrect, null = not applicable)
        public int? HomeWinSuccess { get; set; }
        public int? DrawSuccess { get; set; }
        public int? AwayWinSuccess { get; set; }
        public int? HomeQualifiesSuccess { get; set; }
        public int? AwayQualifiesSuccess { get; set; }
        public int? ResultSuccess { get; set; }
    }

    public class UpcomingBetDto
    {
        public int MatchId { get; set; }
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public DateTime MatchTime { get; set; }
    }

    public class UserBettingStatsDto
    {
        public bool ShowExactResult { get; set; } = false;
        public bool ShowQualified { get; set; } = false;

        public int MatchId { get; set; }
        public string MatchStatus { get; set; } = string.Empty;
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;

        public string? BetPlaced { get; set; }
        public string? WhoQualifiedBet { get; set; }

        public string? MatchResult { get; set; }
        public string? WhoQualifiedResult { get; set; }

        public string? OutcomeRegular { get; set; }
        public string? OutcomeQualification { get; set; }
        public string? OutcomeExactResult { get; set; }

        public decimal? PayoutRegular { get; set; }
        public decimal? PayoutQualification { get; set; }
        public decimal? PayoutExactResult { get; set; }

        public decimal? TotalPayout { get; set; }
    }
}
