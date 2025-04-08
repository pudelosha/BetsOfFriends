using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model.Entities
{
    public class CustomMatch : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MatchId { get; set; }

        public int StageId { get; set; }
        [ForeignKey("StageId")]

        public int? PredefinedMatchId { get; set; }
        [ForeignKey("PredefinedMatchId")]
        public PredefinedMatch? PredefinedSource { get; set; }

        public CustomMatchStage Stage { get; set; }

        public int TournamentId { get; set; }
        [ForeignKey("TournamentId")]
        public CustomTournament Tournament { get; set; }

        public int HomeTeamId { get; set; }
        [ForeignKey("HomeTeamId")]
        public CustomTeam HomeTeam { get; set; }

        public int AwayTeamId { get; set; }
        [ForeignKey("AwayTeamId")]
        public CustomTeam AwayTeam { get; set; }

        public DateTime MatchStart { get; set; }

        // Live result
        public int? HomeScoreLive { get; set; }
        public int? AwayScoreLive { get; set; }
        public string? LiveStatus { get; set; }
        public int? ExternalMatchId { get; set; }

        // Final match result
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public TeamQualified? Qualified { get; set; }

        // Betting Odds
        public decimal HomeWinOdds { get; set; }
        public decimal DrawOdds { get; set; }
        public decimal AwayWinOdds { get; set; }
        public decimal? HomeQualifies { get; set; }
        public decimal? AwayQualifies { get; set; }

        public MatchStatus Status { get; set; } = MatchStatus.Timed;
        public MatchType Type { get; set; } = MatchType.Regular90Min;

        public bool IsVisible { get; set; } = true;

        public bool Notifications1Sent { get; set; } = false;
        public bool Notifications24Sent { get; set; } = false;


        public ICollection<Bet> Bets { get; set; } = new List<Bet>();

        public enum MatchStatus
        {
            Scheduled,
            Timed,
            In_Play,
            Paused,
            Finished,
            Postponed,
            Suspended,
            Canceled
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
