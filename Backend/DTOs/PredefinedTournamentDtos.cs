using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class PredefinedTournamentDto
    {
        public int? TournamentId { get; set; }          // Nullable for new tournaments
        public int? ExternalTournamentId { get; set; }  // From "competition.id"
        public int? Season { get; set; }                // From "filters.season"
        public int? SeasonId { get; set; }              // From "matches.season.id"
        public DateTime? TournamentStart { get; set; }  // From "matches.season.startDate"
        public DateTime? TournamentEnd { get; set; }    // From "matches.season.endDate"

        [Required, MaxLength(100)]
        public string TournamentName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? PublicTournamentName { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public bool IncludeHomeAdvantage { get; set; }

        // Frontend: 'Private' | 'Public'
        [Required, RegularExpression("^(Private|Public)$")]
        public string TournamentVisibility { get; set; } = "Private";

        // Frontend: 'Manual' | 'Semi' | 'Auto'
        [Required, RegularExpression("^(Manual|Semi|Auto)$")]
        public string UpdateMethod { get; set; } = "Manual";

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public List<PredefinedTeamDto> Teams { get; set; } = new();

        [Required]
        public List<PredefinedStageDto> Stages { get; set; } = new();

        [Required]
        public List<PredefinedMatchDto> Matches { get; set; } = new();
    }

    public class PredefinedTeamDto
    {
        public int? TeamId { get; set; }                // Nullable for new teams
        public int? ExternalTeamId { get; set; }        // API data

        // Frontend sends this (can be null)
        public int? PredefinedTeamId { get; set; }

        [Required, MaxLength(50)]
        public string TeamName { get; set; } = string.Empty;

        // Frontend sends eloRating (defaults 1000)
        [Range(0, 5000)]
        public int EloRating { get; set; } = 1000;

        // Frontend: 'New'|'Uploaded'|'Update'|'Delete' (and you sometimes use 'Finalised' in UI)
        [Required, RegularExpression("^(New|Uploaded|Update|Delete|Finalised)$")]
        public string RecordStatus { get; set; } = "Uploaded";
    }

    public class PredefinedStageDto
    {
        public int? StageId { get; set; }

        // Frontend model has it (submit currently doesn't send it, but safe to accept/return)
        public int? PredefinedStageId { get; set; }

        [Range(1, int.MaxValue)]
        public int Order { get; set; }

        [Required, MaxLength(50)]
        public string StageName { get; set; } = string.Empty;

        [Required, RegularExpression("^(New|Uploaded|Update|Delete)$")]
        public string RecordStatus { get; set; } = "Uploaded";
    }

    public class PredefinedMatchDto
    {
        public int? MatchId { get; set; }               // Nullable for new matches
        public int? ExternalMatchId { get; set; }       // API data

        // Frontend model has it (submit currently doesn't send it, but safe to accept/return)
        public int? PredefinedMatchId { get; set; }

        public int? StageId { get; set; }               // Nullable for new matches

        [Required]
        public string StageName { get; set; } = string.Empty;

        public int? HomeTeamId { get; set; }            // Nullable for new matches

        [Required]
        public string HomeTeam { get; set; } = string.Empty;

        public int? AwayTeamId { get; set; }            // Nullable for new matches

        [Required]
        public string AwayTeam { get; set; } = string.Empty;

        [Required]
        public string MatchType { get; set; } = "Regular90Min";

        // Frontend sends numbers; allow null until calculated
        public decimal HomeWinOdds { get; set; } = 1m;
        public decimal DrawOdds { get; set; } = 1m;
        public decimal AwayWinOdds { get; set; } = 1m;

        public decimal? HomeQualifies { get; set; }
        public decimal? AwayQualifies { get; set; }

        // Frontend: 'New'|'Uploaded'|'Update'|'Delete'|'Finalised'
        [Required, RegularExpression("^(New|Uploaded|Update|Delete|Finalised)$")]
        public string RecordStatus { get; set; } = "Uploaded";

        private DateTime _matchStart;

        [Required]
        public DateTime MatchStart
        {
            get => _matchStart;
            set => _matchStart = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public bool IsVisible { get; set; } = true;

        // Closed match props
        public string? MatchStatus { get; set; }        // e.g. 'Finished'
        public int? ScoreHome { get; set; }
        public int? ScoreAway { get; set; }
        public string? QualifiedTeam { get; set; }      // 'Home' | 'Away' | null
    }

    public class PredefinedTournamentListDto
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool HasLiveUpdates { get; set; }
    }

    public class TournamentStatusUpdateDto
    {
        [Required]
        public bool IsActive { get; set; }
    }
}